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

DPI  = 300
FS   = 9
FST  = 11

plt.rcParams.update({
    "figure.facecolor": "white", "axes.facecolor": "white",
    "axes.edgecolor": "#cccccc", "axes.grid": True,
    "grid.color": "#eeeeee", "grid.linewidth": 0.8,
    "font.size": FS, "axes.titlesize": FST,
    "axes.labelsize": FS, "xtick.labelsize": FS - 1,
    "ytick.labelsize": FS - 1, "legend.fontsize": FS - 1,
})

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


def chart_response_time(m_data, c_data, label, num):
    vals_m = [pct(m_data["metrics"].get("http_req_duration", []), p) for p in [50, 95, 99]]
    vals_c = [pct(c_data["metrics"].get("http_req_duration", []), p) for p in [50, 95, 99]]
    x, w   = np.arange(3), 0.35
    fig, ax = plt.subplots(figsize=(7, 4))
    b1 = ax.bar(x - w/2, vals_m, w, label="Minimal API",        color=C_MIN,  zorder=3)
    b2 = ax.bar(x + w/2, vals_c, w, label="Controller-based API", color=C_CTRL, zorder=3)
    for bars in (b1, b2):
        for b in bars:
            ax.text(b.get_x() + b.get_width()/2, b.get_height() + 1,
                    f"{b.get_height():.1f}", ha="center", va="bottom", fontsize=FS-1)
    ax.set_title(f"Czas odpowiedzi HTTP [{label}]", pad=8)
    ax.set_ylabel("Czas [ms]")
    ax.set_xticks(x); ax.set_xticklabels(["p50 (mediana)", "p95", "p99"])
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_czas_odpowiedzi_{label}.png")


def chart_throughput(m_data, c_data, label, num):
    tm, rm = to_rps(m_data["time_series"].get("http_reqs", []))
    tc, rc = to_rps(c_data["time_series"].get("http_reqs", []))
    fig, ax = plt.subplots(figsize=(8, 4))
    if tm: ax.plot(tm, rm, color=C_MIN,  label="Minimal API",        lw=1.5, zorder=3)
    if tc: ax.plot(tc, rc, color=C_CTRL, label="Controller-based API", lw=1.5, zorder=3)
    ax.set_title(f"Przepustowość (req/s) w czasie [{label}]", pad=8)
    ax.set_xlabel("Czas [s]"); ax.set_ylabel("Żądania / sekundę")
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_przepustowosc_{label}.png")


def chart_error_rate(m_data, c_data, label, num):
    def epct(d):
        v = d["metrics"].get("http_req_failed", [])
        return float(np.mean(v)) * 100 if v else 0.0
    vals = [epct(m_data), epct(c_data)]
    fig, ax = plt.subplots(figsize=(5, 4))
    bars = ax.bar(["Minimal API", "Controller-based API"], vals,
                  color=[C_MIN, C_CTRL], width=0.4, zorder=3)
    for b, v in zip(bars, vals):
        ax.text(b.get_x() + b.get_width()/2, b.get_height() + 0.01,
                f"{v:.3f}%", ha="center", va="bottom")
    ax.set_title(f"Wskaźnik błędów HTTP [{label}]", pad=8)
    ax.set_ylabel("Błędy [%]"); ax.set_ylim(0, max(max(vals)*1.4, 1))
    ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_bledy_{label}.png")


def chart_latency_histogram(m_data, c_data, label, num):
    dm = m_data["metrics"].get("http_req_duration", [])
    dc = c_data["metrics"].get("http_req_duration", [])
    if not dm and not dc:
        print(f"  [SKIP] Brak danych dla histogramu [{label}]")
        return
    cap   = np.percentile(dm + dc, 99) if dm or dc else 2000
    bins  = np.linspace(0, cap, 50)
    fig, ax = plt.subplots(figsize=(8, 4))
    if dm: ax.hist(dm, bins=bins, alpha=0.6, color=C_MIN,  label="Minimal API",        density=True, zorder=3)
    if dc: ax.hist(dc, bins=bins, alpha=0.6, color=C_CTRL, label="Controller-based API", density=True, zorder=3)
    ax.set_title(f"Rozkład latencji HTTP (do p99) [{label}]", pad=8)
    ax.set_xlabel("Czas odpowiedzi [ms]"); ax.set_ylabel("Gęstość")
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_histogram_{label}.png")


def chart_auth_flow(m_data, c_data, num):
    steps = [
        ("auth_register_duration", "Rejestracja"),
        ("auth_login_duration",    "Logowanie"),
        ("auth_secured_duration",  "Secured GET"),
        ("auth_refresh_duration",  "Refresh token"),
        ("auth_revoke_duration",   "Revoke token"),
    ]
    labels  = [s[1] for s in steps]
    vals_m  = [pct(m_data["metrics"].get(s[0], []), 50) for s in steps]
    vals_c  = [pct(c_data["metrics"].get(s[0], []), 50) for s in steps]
    p95_m   = [pct(m_data["metrics"].get(s[0], []), 95) for s in steps]
    p95_c   = [pct(c_data["metrics"].get(s[0], []), 95) for s in steps]

    x, w = np.arange(len(steps)), 0.35
    fig, ax = plt.subplots(figsize=(9, 4))
    b1 = ax.bar(x - w/2, vals_m, w, label="Minimal API (p50)",        color=C_MIN,  zorder=3)
    b2 = ax.bar(x + w/2, vals_c, w, label="Controller-based API (p50)", color=C_CTRL, zorder=3)
    # p95 error bars
    ax.errorbar(x - w/2, vals_m, yerr=[np.zeros(len(steps)), [p-m for p, m in zip(p95_m, vals_m)]],
                fmt="none", color="navy", capsize=3, lw=1.2, label="p95")
    ax.errorbar(x + w/2, vals_c, yerr=[np.zeros(len(steps)), [p-m for p, m in zip(p95_c, vals_c)]],
                fmt="none", color="darkred", capsize=3, lw=1.2)
    ax.set_title("Czas każdego kroku auth flow (p50, słupki błędu = p95)", pad=8)
    ax.set_ylabel("Czas [ms]")
    ax.set_xticks(x); ax.set_xticklabels(labels)
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_auth_flow_kroki.png")


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


def chart_cpu(resources, num):
    if not resources:
        print("  [SKIP] Brak danych resource monitor (CPU)")
        return
    fig, ax = plt.subplots(figsize=(9, 4))
    for name, d in resources.items():
        color = C_MIN if "minimal" in name else C_CTRL
        label = "Minimal API" if "minimal" in name else "Controller-based API"
        if d["ts"]:
            ax.plot(d["ts"], d["cpu"], color=color, label=label, lw=1.2, zorder=3)
    ax.set_title("Zużycie CPU podczas testów k6", pad=8)
    ax.set_xlabel("Czas [s]"); ax.set_ylabel("CPU [%]")
    ax.yaxis.set_major_formatter(mticker.PercentFormatter())
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_cpu_procent.png")


def chart_memory(resources, num):
    if not resources:
        print("  [SKIP] Brak danych resource monitor (RAM)")
        return
    fig, ax = plt.subplots(figsize=(9, 4))
    for name, d in resources.items():
        color = C_MIN if "minimal" in name else C_CTRL
        label = "Minimal API" if "minimal" in name else "Controller-based API"
        if d["ts"]:
            ax.plot(d["ts"], d["mem"], color=color, label=label, lw=1.2, zorder=3)
    ax.set_title("Zużycie pamięci RAM podczas testów k6", pad=8)
    ax.set_xlabel("Czas [s]"); ax.set_ylabel("Pamięć [MB]")
    ax.legend(); ax.set_axisbelow(True); fig.tight_layout()
    _save(fig, f"{num:02d}_ram_mb.png")


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
