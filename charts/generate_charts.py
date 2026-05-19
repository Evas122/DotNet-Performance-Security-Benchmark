"""
generate_charts.py — generuje wszystkie wykresy dla magisterki.

Źródła danych:
  • k6/results/crud-load_{minimal,controllers}.json
  • k6/results/auth-flow_{minimal,controllers}.json
  • k6/results/read-heavy_{minimal,controllers}.json
  • k6/results/resources.csv            (resource-monitor)
  • security/zap-reports/*.xml          (OWASP ZAP)
  • BenchmarkDotNet.Artifacts/results/  (BenchmarkDotNet CSV)

Wykresy (charts/output/):
  01  Czas odpowiedzi p50/p95/p99 — crud-load
  02  Przepustowość req/s w czasie — crud-load
  03  Wskaźnik błędów — crud-load
  04  Histogram rozkładu latencji — crud-load
  05  Czas odpowiedzi p50/p95/p99 — read-heavy
  06  Przepustowość req/s w czasie — read-heavy
  07  Kroki auth flow — register/login/secured/refresh/revoke
  08  Wyniki BenchmarkDotNet (Mean ± Error)
  09  CPU% w czasie — podczas testów k6
  10  RAM MB w czasie — podczas testów k6
  11  Alerty bezpieczeństwa ZAP (unauthenticated vs authenticated)

Użycie:
  pip install -r charts/requirements.txt
  python charts/generate_charts.py
"""

import argparse
import csv
import glob
import json
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.ticker as mticker
import numpy as np

# ── Paths ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR  = Path(__file__).parent
REPO_ROOT   = SCRIPT_DIR.parent
OUTPUT_DIR  = SCRIPT_DIR / "output"
K6_RESULTS  = REPO_ROOT / "k6" / "results"
ZAP_REPORTS = REPO_ROOT / "security" / "zap-reports"
BDN_DIR     = REPO_ROOT / "BenchmarkDotNet.Artifacts" / "results"

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# ── Style ─────────────────────────────────────────────────────────────────────
C_MIN  = "#1565C0"   # ciemnoniebieski — Minimal API
C_CTRL = "#B71C1C"   # ciemnoczerwony  — Controller-based API
C_P50  = "#388E3C"
C_P95  = "#F57C00"
C_P99  = "#C62828"

DPI  = 150      # niższe DPI = mniejszy plik, ale nadal czytelny
FS   = 10
FST  = 12

plt.rcParams.update({
    "figure.facecolor": "white", "axes.facecolor": "white",
    "axes.edgecolor": "#cccccc", "axes.grid": True,
    "grid.color": "#eeeeee", "grid.linewidth": 0.8,
    "font.size": FS, "axes.titlesize": FST,
    "axes.labelsize": FS, "xtick.labelsize": FS,
    "ytick.labelsize": FS, "legend.fontsize": FS - 1,
})


def _add_footnote(fig, text: str):
    """Dodaje szarą ramkę z objaśnieniem na dole wykresu."""
    fig.text(
        0.5, -0.04, text,
        ha="center", va="top", fontsize=FS - 2,
        color="#444444",
        bbox=dict(boxstyle="round,pad=0.5", facecolor="#F5F5F5",
                  edgecolor="#BDBDBD", alpha=0.95),
        wrap=True,
    )

# ── k6 JSON parser ─────────────────────────────────────────────────────────────

def parse_k6(path: Path) -> dict:
    metrics     = defaultdict(list)
    time_series = defaultdict(list)
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError:
                continue
            if obj.get("type") != "Point":
                continue
            metric = obj.get("metric", "")
            data   = obj.get("data", {})
            value  = data.get("value")
            ts     = data.get("time", "")
            if value is None:
                continue
            metrics[metric].append(float(value))
            time_series[metric].append((ts, float(value)))
    return {"metrics": dict(metrics), "time_series": dict(time_series)}


def pct(values, p):
    return float(np.percentile(values, p)) if values else 0.0


def to_rps(time_series_data, window=5.0):
    if not time_series_data:
        return [], []
    from datetime import datetime
    def parse_ts(s):
        try:
            return datetime.fromisoformat(s.replace("Z", "+00:00")).timestamp()
        except Exception:
            return 0.0
    points = sorted((parse_ts(t), v) for t, v in time_series_data)
    t0     = points[0][0]
    max_t  = points[-1][0] - t0
    bins   = np.arange(0, max_t + window, window)
    counts, _ = np.histogram([p[0] - t0 for p in points], bins=bins)
    return list((bins[:-1] + bins[1:]) / 2), list(counts / window)

# ── ZAP XML parser ─────────────────────────────────────────────────────────────

RISK_LABELS = {"0": "Informacyjne", "1": "Niskie", "2": "Średnie", "3": "Wysokie"}
RISK_COLORS = {"0": "#BBDEFB", "1": "#C8E6C9", "2": "#FFF9C4", "3": "#FFCDD2"}

def parse_zap(path: Path) -> dict[str, int]:
    counts = {"0": 0, "1": 0, "2": 0, "3": 0}
    try:
        for alert in ET.parse(path).getroot().iter("alertitem"):
            risk = alert.findtext("riskcode", "0").strip()
            counts[risk] = counts.get(risk, 0) + 1
    except Exception as e:
        print(f"  [WARN] ZAP parse error {path.name}: {e}")
    return counts

# ── BenchmarkDotNet CSV parser ─────────────────────────────────────────────────

def parse_bdn_csv(path: Path) -> list[dict]:
    rows = []
    try:
        with open(path, newline="", encoding="utf-8") as fh:
            reader = csv.DictReader(fh)
            for row in reader:
                rows.append(row)
    except Exception as e:
        print(f"  [WARN] BDN parse error {path.name}: {e}")
    return rows

# ── Resource CSV parser ────────────────────────────────────────────────────────

def parse_resources(path: Path) -> dict[str, dict]:
    """Zwraca {container_name: {timestamps: [], cpu: [], mem_mb: []}}"""
    data = defaultdict(lambda: {"ts": [], "cpu": [], "mem": []})
    try:
        with open(path, newline="", encoding="utf-8") as fh:
            reader = csv.DictReader(fh)
            t0 = None
            for row in reader:
                ts = float(row.get("timestamp_s", 0))
                if t0 is None:
                    t0 = ts
                name = row.get("name", "").replace("secperf-", "").replace("-1", "")
                data[name]["ts"].append(ts - t0)
                data[name]["cpu"].append(float(row.get("cpu_pct", 0)))
                data[name]["mem"].append(float(row.get("mem_mb", 0)))
    except Exception as e:
        print(f"  [WARN] Resources parse error: {e}")
    return dict(data)

# ═══════════════════════════════════════════════════════════════════════════════
# CHART FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════════

def _save(fig, name):
    out = OUTPUT_DIR / name
    fig.savefig(out, dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {name}")


def _bar_label(ax, bar, text, pad=1, fontsize=None, color="black"):
    """Etykieta nad słupkiem, ze spacją od góry."""
    fs = fontsize or (FS - 1)
    ax.text(
        bar.get_x() + bar.get_width() / 2,
        bar.get_height() + pad,
        text, ha="center", va="bottom",
        fontsize=fs, fontweight="bold", color=color,
    )


def chart_response_time(m_data, c_data, label, num):
    """Trzy osobne subploty — każdy z własną skalą:
       1) p50  — typowy request (mediana), skala dopasowana
       2) p90  — 90% requestów poniżej tej wartości
       3) p95/p99 — ogon (faza spike), z linią timeout k6
    """
    K6_TIMEOUT_MS = 30_000
    dur_m = m_data["metrics"].get("http_req_duration", [])
    dur_c = c_data["metrics"].get("http_req_duration", [])
    p50m, p90m, p95m, p99m = [pct(dur_m, p) for p in (50, 90, 95, 99)]
    p50c, p90c, p95c, p99c = [pct(dur_c, p) for p in (50, 90, 95, 99)]

    w = 0.32
    fig, axes = plt.subplots(1, 3, figsize=(13, 5),
                             gridspec_kw={"wspace": 0.42})
    fig.suptitle(f"Czas odpowiedzi HTTP [{label}]", fontsize=FST + 1, y=1.02)

    # ── subplot 1: p50 (mediana) ──────────────────────────────────────────────
    ax = axes[0]
    bm = ax.bar(-w/2, p50m, w, color=C_MIN,  zorder=3, label="Minimal API")
    bc = ax.bar( w/2, p50c, w, color=C_CTRL, zorder=3, label="Controller-based API")
    _bar_label(ax, bm[0], f"{p50m:.0f} ms", pad=0.5)
    _bar_label(ax, bc[0], f"{p50c:.0f} ms", pad=0.5)
    ax.set_title("p50 — mediana\n(typowy request)", fontsize=FS)
    ax.set_ylabel("Czas [ms]")
    ax.set_xticks([])
    ax.set_ylim(0, max(p50m, p50c) * 2.2)
    ax.legend(loc="upper center", fontsize=FS - 2, ncol=1)
    ax.set_axisbelow(True)

    # ── subplot 2: p90 ────────────────────────────────────────────────────────
    ax = axes[1]
    bm2 = ax.bar(-w/2, p90m, w, color=C_MIN,  zorder=3, label="Minimal API")
    bc2 = ax.bar( w/2, p90c, w, color=C_CTRL, zorder=3, label="Controller-based API")
    for b, v in [(bm2[0], p90m), (bc2[0], p90c)]:
        lbl = f"{v/1000:.1f} s" if v >= 1000 else f"{v:.0f} ms"
        _bar_label(ax, b, lbl, pad=max(v * 0.01, 50))
    ax.set_title("p90\n(90% requestów szybszych)", fontsize=FS)
    ax.set_ylabel("Czas [ms]")
    ax.set_xticks([])
    ax.set_ylim(0, max(p90m, p90c) * 1.3)
    if max(p90m, p90c) >= 1000:
        ax.yaxis.set_major_formatter(
            mticker.FuncFormatter(lambda v, _: f"{v/1000:.0f} s"))
    ax.legend(loc="upper center", fontsize=FS - 2, ncol=1)
    ax.set_axisbelow(True)

    # ── subplot 3: p95 i p99 z linią timeout ─────────────────────────────────
    ax = axes[2]
    x = np.array([0, 1])
    b1 = ax.bar(x - w/2, [p95m, p99m], w, color=C_MIN,  zorder=3, label="Minimal API")
    b2 = ax.bar(x + w/2, [p95c, p99c], w, color=C_CTRL, zorder=3, label="Controller-based API")
    for b in list(b1) + list(b2):
        lbl = f"{b.get_height()/1000:.1f} s"
        _bar_label(ax, b, lbl, pad=max(b.get_height() * 0.01, 200))
    # linia timeout — wewnątrz obszaru wykresu
    ymax = max(p95m, p95c, p99m, p99c) * 1.25
    ax.set_ylim(0, ymax)
    ax.axhline(K6_TIMEOUT_MS, color="#E53935", ls="--", lw=1.3, zorder=4,
               label="Limit timeout k6 (30 s)")
    ax.set_title("p95 / p99\n(faza spike — głównie timeouty)", fontsize=FS)
    ax.set_ylabel("Czas [s]")
    ax.set_xticks(x); ax.set_xticklabels(["p95", "p99"])
    ax.yaxis.set_major_formatter(
        mticker.FuncFormatter(lambda v, _: f"{v/1000:.0f} s"))
    ax.legend(loc="upper left", fontsize=FS - 2, ncol=1)
    ax.set_axisbelow(True)

    _add_footnote(fig,
        "p50 (mediana) = połowa requestów odpowiedziała szybciej niż ta wartość — najważniejsza miara typowej wydajności.\n"
        "p90 = 90% requestów zakończyło się poniżej tej wartości. "
        "p95/p99 = ogon rozkładu — podczas fazy spike (300 wirtualnych użytkowników) część requestów "
        "trafia w domyślny timeout k6 (30 s), co zawyża te percentyle.")
    fig.savefig(OUTPUT_DIR / f"{num:02d}_czas_odpowiedzi_{label}.png",
                dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {num:02d}_czas_odpowiedzi_{label}.png")


def _rolling_avg(values, window=5):
    """Prosta średnia krocząca."""
    if len(values) < window:
        return values
    result = []
    for i in range(len(values)):
        start = max(0, i - window // 2)
        end   = min(len(values), i + window // 2 + 1)
        result.append(float(np.mean(values[start:end])))
    return result


def chart_throughput(m_data, c_data, label, num):
    tm, rm = to_rps(m_data["time_series"].get("http_reqs", []), window=10.0)
    tc, rc = to_rps(c_data["time_series"].get("http_reqs", []), window=10.0)
    fig, ax = plt.subplots(figsize=(9, 4))
    # surowe dane — półprzezroczyste
    if tm: ax.plot(tm, rm, color=C_MIN,  alpha=0.25, lw=0.8, zorder=2)
    if tc: ax.plot(tc, rc, color=C_CTRL, alpha=0.25, lw=0.8, zorder=2)
    # rolling average — wyraźna linia
    ROLL = 5
    if tm:
        ax.plot(tm, _rolling_avg(rm, ROLL), color=C_MIN,  lw=2.0, label="Minimal API",          zorder=3)
    if tc:
        ax.plot(tc, _rolling_avg(rc, ROLL), color=C_CTRL, lw=2.0, label="Controller-based API", zorder=3)
    ax.set_title(f"Przepustowość (req/s) w czasie [{label}]", pad=10, fontsize=FST)
    ax.set_xlabel("Czas od startu testu [s]")
    ax.set_ylabel("Żądania / sekundę")
    ax.legend(loc="upper right")
    ax.set_axisbelow(True)
    _add_footnote(fig,
        "Przepustowość = liczba odpowiedzi HTTP na sekundę. "
        "Linia jasna (w tle) = surowe próbki co 10 s. Linia ciągła = wygładzona średnia krocząca. "
        "Spadek przepustowości przy dużym obciążeniu (faza spike) wynika z kolejkowania requestów "
        "i ograniczonego przydziału 1 rdzenia CPU na kontener.")
    fig.savefig(OUTPUT_DIR / f"{num:02d}_przepustowosc_{label}.png",
                dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {num:02d}_przepustowosc_{label}.png")


def chart_error_rate(m_data, c_data, label, num):
    def epct(d):
        v = d["metrics"].get("http_req_failed", [])
        return float(np.mean(v)) * 100 if v else 0.0
    vals = [epct(m_data), epct(c_data)]
    fig, ax = plt.subplots(figsize=(6, 4.5))
    bars = ax.bar(["Minimal API", "Controller-based API"], vals,
                  color=[C_MIN, C_CTRL], width=0.4, zorder=3)
    for b, v in zip(bars, vals):
        ax.text(b.get_x() + b.get_width()/2, b.get_height() + 0.1,
                f"{v:.2f}%", ha="center", va="bottom", fontsize=FS, fontweight="bold")
    ax.set_title(f"Wskaźnik błędów HTTP [{label}]", pad=8)
    ax.set_ylabel("Błędy [%]"); ax.set_ylim(0, max(max(vals) * 1.5, 2))
    ax.set_axisbelow(True)
    # adnotacja wyjaśniająca źródło błędów
    ax.text(0.5, 0.92,
            "Błędy wynikają głównie z timeoutów podczas fazy spike (300 VU)\n"
            "— normalny ruch (sustained) generuje <1% błędów",
            transform=ax.transAxes, ha="center", va="top",
            fontsize=FS - 2, color="#555555",
            bbox=dict(boxstyle="round,pad=0.3", facecolor="#FFF8E1",
                      edgecolor="#FFD54F", alpha=0.9))
    fig.tight_layout()
    _save(fig, f"{num:02d}_bledy_{label}.png")


def chart_latency_histogram(m_data, c_data, label, num):
    dm = m_data["metrics"].get("http_req_duration", [])
    dc = c_data["metrics"].get("http_req_duration", [])
    if not dm and not dc:
        print(f"  [SKIP] Brak danych dla histogramu [{label}]")
        return
    all_vals = dm + dc
    # Obetnij przy p90 żeby oś X była czytelna — ogon timeoutów i tak widać
    cap  = np.percentile(all_vals, 90) if all_vals else 2000
    bins = np.linspace(0, cap, 60)
    fig, ax = plt.subplots(figsize=(9, 4.5))
    if dm: ax.hist(dm, bins=bins, alpha=0.55, color=C_MIN,  label="Minimal API",          density=True, zorder=3)
    if dc: ax.hist(dc, bins=bins, alpha=0.55, color=C_CTRL, label="Controller-based API", density=True, zorder=3)
    # linia median
    for vals, color, name in [(dm, C_MIN, "Minimal"), (dc, C_CTRL, "Controllers")]:
        if vals:
            med = np.percentile(vals, 50)
            if med <= cap:
                ax.axvline(med, color=color, ls="--", lw=1.4,
                           label=f"p50 {name} = {med:.0f} ms")
    ax.set_title(f"Rozkład latencji HTTP (do p90) [{label}]\n"
                 "Ogon powyżej p90 pominięty (timeouty spike — 30 s)", pad=8)
    ax.set_xlabel("Czas odpowiedzi [ms]"); ax.set_ylabel("Gęstość")
    if cap > 2000:
        ax.xaxis.set_major_formatter(
            mticker.FuncFormatter(lambda v, _: f"{v/1000:.1f} s" if v >= 1000 else f"{v:.0f} ms"))
    ax.legend(fontsize=FS - 1); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_histogram_{label}.png")


def chart_auth_flow(m_data, c_data, num):
    steps = [
        ("auth_register_duration", "Rejestracja"),
        ("auth_login_duration",    "Logowanie"),
        ("auth_secured_duration",  "Secured GET"),
        ("auth_refresh_duration",  "Refresh token"),
        ("auth_revoke_duration",   "Revoke token"),
    ]
    labels = [s[1] for s in steps]
    vals_m = [pct(m_data["metrics"].get(s[0], []), 50) for s in steps]
    vals_c = [pct(c_data["metrics"].get(s[0], []), 50) for s in steps]
    p90_m  = [pct(m_data["metrics"].get(s[0], []), 90) for s in steps]
    p90_c  = [pct(c_data["metrics"].get(s[0], []), 90) for s in steps]

    x, w = np.arange(len(steps)), 0.33
    fig, ax = plt.subplots(figsize=(11, 5))

    b1 = ax.bar(x - w/2, vals_m, w, label="Minimal API (mediana p50)",         color=C_MIN,  zorder=3)
    b2 = ax.bar(x + w/2, vals_c, w, label="Controller-based API (mediana p50)", color=C_CTRL, zorder=3)

    # etykiety wartości na słupkach
    for b, v in list(zip(b1, vals_m)) + list(zip(b2, vals_c)):
        if v > 0:
            lbl = f"{v/1000:.1f}s" if v >= 1000 else f"{v:.0f}ms"
            ax.text(b.get_x() + b.get_width()/2, b.get_height() + 15,
                    lbl, ha="center", va="bottom", fontsize=FS - 2, fontweight="bold")

    # p90 jako słupki błędów — tylko odchylenie w górę, zablokowane do ymax
    err_m = [max(0, p - v) for p, v in zip(p90_m, vals_m)]
    err_c = [max(0, p - v) for p, v in zip(p90_c, vals_c)]
    ax.errorbar(x - w/2, vals_m, yerr=[np.zeros(len(steps)), err_m],
                fmt="none", color="#1A237E", capsize=4, lw=1.5,
                label="p90 Minimal API")
    ax.errorbar(x + w/2, vals_c, yerr=[np.zeros(len(steps)), err_c],
                fmt="none", color="#7B1FA2", capsize=4, lw=1.5,
                label="p90 Controller-based API")

    # oś Y — ogranicz do rozsądnej wartości żeby słupki błędów nie uciekały
    bar_max = max(vals_m + vals_c + p90_m + p90_c)
    ax.set_ylim(0, min(bar_max * 1.35, bar_max + 800))

    ax.set_title("Czas odpowiedzi poszczególnych kroków auth flow\n"
                 "(słupki = mediana p50, linie = p90)", pad=10, fontsize=FST)
    ax.set_ylabel("Czas [ms]")
    ax.set_xticks(x); ax.set_xticklabels(labels, fontsize=FS)
    ax.legend(loc="upper right", fontsize=FS - 1, ncol=2)
    ax.set_axisbelow(True)

    _add_footnote(fig,
        "Rejestracja i Logowanie są wolne (~2 s) z powodu BCrypt — celowo powolny algorytm hashowania haseł "
        "chroniący przed atakami brute-force. Secured GET to zwykły request z JWT — stąd bardzo niska latencja (~100 ms). "
        "Refresh/Revoke token = operacje na bazie danych bez hashowania.")
    fig.savefig(OUTPUT_DIR / f"{num:02d}_auth_flow_kroki.png",
                dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {num:02d}_auth_flow_kroki.png")


def chart_benchmarkdotnet(bdn_rows, num):
    if not bdn_rows:
        print("  [SKIP] Brak danych BenchmarkDotNet")
        return

    # Grupuj wiersze po Method
    min_rows  = [r for r in bdn_rows if "Minimal"     in r.get("Method", "")]
    ctrl_rows = [r for r in bdn_rows if "Controllers" in r.get("Method", "") or "Controller" in r.get("Method", "")]

    if not min_rows and not ctrl_rows:
        print("  [SKIP] BDN: nie znaleziono wierszy Minimal/Controllers")
        return

    def mean_ns(rows):
        out = []
        for r in rows:
            try:
                out.append(float(r.get("Mean", r.get("Mean (ns)", 0))))
            except ValueError:
                out.append(0.0)
        return out

    # Spróbuj dopasować parami (zakładamy że kolejność jest taka sama)
    methods   = [r.get("Method", f"#{i}").replace("MinimalApi_", "").replace("ControllersApi_", "")
                 for i, r in enumerate(min_rows)] or \
                [r.get("Method", f"#{i}").replace("ControllersApi_", "")
                 for i, r in enumerate(ctrl_rows)]

    vals_m_ns = mean_ns(min_rows)  if min_rows  else [0] * len(methods)
    vals_c_ns = mean_ns(ctrl_rows) if ctrl_rows else [0] * len(methods)

    # Przelicz na µs
    vals_m = [v / 1000 for v in vals_m_ns]
    vals_c = [v / 1000 for v in vals_c_ns]

    x, w = np.arange(len(methods)), 0.35
    fig, ax = plt.subplots(figsize=(max(6, len(methods) * 2), 4))
    if vals_m: ax.bar(x - w/2, vals_m, w, label="Minimal API",        color=C_MIN,  zorder=3)
    if vals_c: ax.bar(x + w/2, vals_c, w, label="Controller-based API", color=C_CTRL, zorder=3)
    ax.set_title("BenchmarkDotNet — czas wykonania (Mean)", pad=8)
    ax.set_ylabel("Czas [µs]")
    ax.set_xticks(x); ax.set_xticklabels(methods, rotation=20, ha="right")
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_benchmarkdotnet.png")


def _build_phases():
    """Przybliżone czasy faz testowych (sekundy od startu resource-monitora).
    Warmup globalny: 2×90s + 30s = ~210s
    crud-load: warmup 60s + ramp 70s + sustained 140s + spike 70s = ~340s
    read-heavy: warmup 60s + ramp 70s + sustained 185s + spike 65s = ~380s
    auth-flow: warmup 60s + auth 200s = ~260s
    cooldown między scenariuszami: 90s
    """
    CRUD = 340; READ = 380; AUTH = 260; COOL = 90; WARMUP = 210
    phases, t = [], WARMUP
    for name, dur, color in [
        ("CRUD\nMinimal",      CRUD, "#BBDEFB"),
        ("CRUD\nControllers",  CRUD, "#FFCDD2"),
        ("Read\nMinimal",      READ, "#BBDEFB"),
        ("Read\nControllers",  READ, "#FFCDD2"),
        ("Auth\nMinimal",      AUTH, "#BBDEFB"),
        ("Auth\nControllers",  AUTH, "#FFCDD2"),
    ]:
        phases.append((t, t + dur, name, color))
        t += dur + COOL
    return phases


def _add_phase_bands(ax, max_t, label_ypos_fraction=0.96):
    """Kolorowe pasy i etykiety faz — etykiety na stałej wysokości, nie nachodzą na dane."""
    phases = _build_phases()
    ymin, ymax = ax.get_ylim()
    label_y = ymin + (ymax - ymin) * label_ypos_fraction
    for t0, t1, name, color in phases:
        if t0 >= max_t:
            break
        t1c = min(t1, max_t)
        ax.axvspan(t0, t1c, alpha=0.18, color=color, zorder=0, linewidth=0)
        mid = (t0 + t1c) / 2
        if mid < max_t:
            ax.text(mid, label_y, name,
                    ha="center", va="top", fontsize=FS - 3,
                    color="#555555", linespacing=1.3,
                    bbox=dict(facecolor="white", alpha=0.6, edgecolor="none", pad=1))


def chart_cpu(resources, num):
    if not resources:
        print("  [SKIP] Brak danych resource monitor (CPU)")
        return
    fig, ax = plt.subplots(figsize=(12, 5))
    max_t = 0
    for name, d in resources.items():
        color = C_MIN if "minimal" in name else C_CTRL
        lbl   = "Minimal API" if "minimal" in name else "Controller-based API"
        if d["ts"]:
            smoothed = _rolling_avg(d["cpu"], window=7)
            ax.plot(d["ts"], d["cpu"],  color=color, alpha=0.15, lw=0.6, zorder=2)
            ax.plot(d["ts"], smoothed,  color=color, label=lbl,  lw=2.0, zorder=3)
            max_t = max(max_t, max(d["ts"]))
    ax.set_ylim(0, 115)
    ax.set_xlim(left=0)
    _add_phase_bands(ax, max_t, label_ypos_fraction=0.98)
    ax.set_title("Zużycie CPU podczas testów k6", pad=10, fontsize=FST)
    ax.set_xlabel("Czas od startu testów [s]")
    ax.set_ylabel("CPU [%]")
    ax.yaxis.set_major_formatter(mticker.PercentFormatter())
    ax.legend(loc="upper right", framealpha=0.9)
    ax.set_axisbelow(True)
    _add_footnote(fig,
        "Każde API testowane osobno po kolei (pasy na wykresie = fazy testów). "
        "Niebieski (Minimal API) i czerwony (Controller-based API) nie działają jednocześnie pod obciążeniem — "
        "kiedy jeden jest testowany, drugi jest bezczynny. "
        "Skoki do 100% oznaczają pełne wysycenie 1 rdzenia CPU przydzielonego kontenerowi.")
    fig.savefig(OUTPUT_DIR / f"{num:02d}_cpu_procent.png",
                dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {num:02d}_cpu_procent.png")


def chart_memory(resources, num):
    if not resources:
        print("  [SKIP] Brak danych resource monitor (RAM)")
        return
    fig, ax = plt.subplots(figsize=(12, 5))
    max_t = 0
    for name, d in resources.items():
        color = C_MIN if "minimal" in name else C_CTRL
        lbl   = "Minimal API" if "minimal" in name else "Controller-based API"
        if d["ts"]:
            ax.plot(d["ts"], d["mem"], color=color, label=lbl, lw=2.0, zorder=3)
            max_t = max(max_t, max(d["ts"]))
    ax.set_xlim(left=0)
    ymin_data = min(min(d["mem"]) for d in resources.values() if d["mem"])
    ax.set_ylim(max(0, ymin_data - 20), None)
    _add_phase_bands(ax, max_t, label_ypos_fraction=0.98)
    ax.set_title("Zużycie pamięci RAM podczas testów k6", pad=10, fontsize=FST)
    ax.set_xlabel("Czas od startu testów [s]")
    ax.set_ylabel("Pamięć [MB]")
    ax.legend(loc="upper right", framealpha=0.9)
    ax.set_axisbelow(True)
    _add_footnote(fig,
        "RAM mierzony co 2 sekundy per kontener. "
        "Każde API testowane osobno po kolei — wzrosty zużycia pamięci odpowiadają fazom obciążeniowym. "
        ".NET GC (Garbage Collector) okresowo zwalnia pamięć, stąd widoczne 'skoki w dół' na wykresie.")
    fig.savefig(OUTPUT_DIR / f"{num:02d}_ram_mb.png",
                dpi=DPI, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    print(f"  [OK] {num:02d}_ram_mb.png")


def chart_zap(unauth_m, unauth_c, auth_m, auth_c, num):
    risks      = ["3", "2", "1", "0"]
    risk_names = [RISK_LABELS[r] for r in risks]
    cols       = ["Min. (bez auth)", "Ctrl. (bez auth)", "Min. (z auth)", "Ctrl. (z auth)"]
    data_sets  = [unauth_m, unauth_c, auth_m, auth_c]

    table_data = [[ds.get(r, 0) for ds in data_sets] for r in risks]

    fig, ax = plt.subplots(figsize=(8, 3.5))
    ax.axis("off")
    tbl = ax.table(cellText=table_data, rowLabels=risk_names, colLabels=cols,
                   loc="center", cellLoc="center")
    tbl.auto_set_font_size(False); tbl.set_fontsize(FS); tbl.scale(1.3, 2.0)

    for ri, risk in enumerate(risks):
        for ci in range(len(cols)):
            tbl[ri + 1, ci].set_facecolor(RISK_COLORS[risk])
        tbl[ri + 1, -1].set_facecolor(RISK_COLORS[risk])

    ax.set_title("Alerty OWASP ZAP — bez autoryzacji vs z autoryzacją", pad=12)
    fig.tight_layout()
    _save(fig, f"{num:02d}_zap_alerty.png")

# ═══════════════════════════════════════════════════════════════════════════════
# FILE DISCOVERY
# ═══════════════════════════════════════════════════════════════════════════════

def latest(pattern):
    files = sorted(glob.glob(str(K6_RESULTS / pattern)))
    return Path(files[-1]) if files else None


def load_k6_pair(scenario):
    m = latest(f"{scenario}_minimal*.json") or latest(f"*{scenario}*minimal*.json")
    c = latest(f"{scenario}_controllers*.json") or latest(f"*{scenario}*controllers*.json")
    return m, c

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--crud-minimal",       type=Path)
    ap.add_argument("--crud-controllers",   type=Path)
    ap.add_argument("--auth-minimal",       type=Path)
    ap.add_argument("--auth-controllers",   type=Path)
    ap.add_argument("--read-minimal",       type=Path)
    ap.add_argument("--read-controllers",   type=Path)
    ap.add_argument("--resources",          type=Path)
    ap.add_argument("--zap-minimal",        type=Path)
    ap.add_argument("--zap-controllers",    type=Path)
    ap.add_argument("--zap-minimal-auth",   type=Path)
    ap.add_argument("--zap-controllers-auth", type=Path)
    args = ap.parse_args()

    print("\n╔══════════════════════════════════════════╗")
    print("║  SecPerf — generowanie wykresów          ║")
    print("╚══════════════════════════════════════════╝\n")

    # ── CRUD-load ─────────────────────────────────────────────────────────────
    cm_path = args.crud_minimal     or (lambda p: p if p else None)(latest("crud-load_minimal*.json"))
    cc_path = args.crud_controllers or (lambda p: p if p else None)(latest("crud-load_controllers*.json"))
    if cm_path and cm_path.exists() and cc_path and cc_path.exists():
        print("► crud-load")
        cm = parse_k6(cm_path); cc = parse_k6(cc_path)
        chart_response_time(cm, cc, "crud-load", 1)
        chart_throughput(cm, cc, "crud-load", 2)
        chart_error_rate(cm, cc, "crud-load", 3)
        chart_latency_histogram(cm, cc, "crud-load", 4)
    else:
        print("  [SKIP] Brak danych crud-load")

    # ── Read-heavy ────────────────────────────────────────────────────────────
    rm_path = args.read_minimal     or (lambda p: p if p else None)(latest("read-heavy_minimal*.json"))
    rc_path = args.read_controllers or (lambda p: p if p else None)(latest("read-heavy_controllers*.json"))
    if rm_path and rm_path.exists() and rc_path and rc_path.exists():
        print("► read-heavy")
        rm = parse_k6(rm_path); rc = parse_k6(rc_path)
        chart_response_time(rm, rc, "read-heavy", 5)
        chart_throughput(rm, rc, "read-heavy", 6)
    else:
        print("  [SKIP] Brak danych read-heavy")

    # ── Auth flow ─────────────────────────────────────────────────────────────
    am_path = args.auth_minimal     or (lambda p: p if p else None)(latest("auth-flow_minimal*.json"))
    ac_path = args.auth_controllers or (lambda p: p if p else None)(latest("auth-flow_controllers*.json"))
    if am_path and am_path.exists() and ac_path and ac_path.exists():
        print("► auth-flow")
        am = parse_k6(am_path); ac = parse_k6(ac_path)
        chart_auth_flow(am, ac, 7)
    else:
        print("  [SKIP] Brak danych auth-flow")

    # ── BenchmarkDotNet ───────────────────────────────────────────────────────
    bdn_csvs = sorted(glob.glob(str(BDN_DIR / "*.csv")))
    if bdn_csvs:
        print("► BenchmarkDotNet")
        all_rows = []
        for f in bdn_csvs:
            all_rows.extend(parse_bdn_csv(Path(f)))
        chart_benchmarkdotnet(all_rows, 8)
    else:
        print("  [SKIP] Brak CSV BenchmarkDotNet (uruchom: dotnet run -c Release --project src/SecPerf.Benchmarks)")

    # ── Resource monitor ──────────────────────────────────────────────────────
    res_path = args.resources or K6_RESULTS / "resources.csv"
    if res_path.exists():
        print("► resource monitor")
        resources = parse_resources(res_path)
        chart_cpu(resources, 9)
        chart_memory(resources, 10)
    else:
        print("  [SKIP] Brak resources.csv (uruchom docker compose z resource-monitor)")

    # ── ZAP ───────────────────────────────────────────────────────────────────
    zap_um = args.zap_minimal           or ZAP_REPORTS / "minimal-api-report.xml"
    zap_uc = args.zap_controllers       or ZAP_REPORTS / "controllers-api-report.xml"
    zap_am = args.zap_minimal_auth      or ZAP_REPORTS / "minimal-api-auth-report.xml"
    zap_ac = args.zap_controllers_auth  or ZAP_REPORTS / "controllers-api-auth-report.xml"

    if any(p.exists() for p in [zap_um, zap_uc, zap_am, zap_ac]):
        print("► ZAP")
        chart_zap(
            parse_zap(zap_um) if zap_um.exists() else {},
            parse_zap(zap_uc) if zap_uc.exists() else {},
            parse_zap(zap_am) if zap_am.exists() else {},
            parse_zap(zap_ac) if zap_ac.exists() else {},
            11,
        )
    else:
        print("  [SKIP] Brak raportów ZAP")

    print(f"\nGotowe → {OUTPUT_DIR}/")


if __name__ == "__main__":
    main()
