#!/bin/sh
# Uruchamiany wewnątrz kontenera grafana/k6.
# Skrypty montowane w /scripts, wyniki zapisywane w /results.
# Zawsze kończy kodem 0 — wyniki zachowywane nawet przy przekroczeniu thresholdów.

MINIMAL="http://api-minimal:8080"
CONTROLLERS="http://api-controllers:8080"

# Czas w sekundach na ochłodzenie API między testami.
# .NET GC, pula wątków i pula połączeń DB potrzebują czasu żeby wrócić do baseline.
COOLDOWN=90

run() {
  scenario="$1"   # crud-load | auth-flow | read-heavy
  api="$2"        # minimal | controllers

  echo ""
  echo "══════════════════════════════════════════════"
  echo " k6: ${scenario} → ${api}"
  echo "══════════════════════════════════════════════"

  k6 run \
    --env API="${api}" \
    --env BASE_URL_MINIMAL="${MINIMAL}" \
    --env BASE_URL_CONTROLLERS="${CONTROLLERS}" \
    --out "json=/results/${scenario}_${api}.json" \
    "/scripts/scenarios/${scenario}.js" || true
}

warmup_api() {
  api="$1"
  echo ""
  echo "── Warmup bazy danych: ${api} (90s) ──"
  k6 run \
    --env API="${api}" \
    --env BASE_URL_MINIMAL="${MINIMAL}" \
    --env BASE_URL_CONTROLLERS="${CONTROLLERS}" \
    "/scripts/scenarios/db-warmup.js" || true
}

cooldown() {
  echo ""
  echo "── Przerwa ${COOLDOWN}s — API wraca do baseline (GC, pula połączeń) ──"
  sleep "${COOLDOWN}"
}

# ── Globalny warmup SQL Server — oba API rozgrzane jednakowo ────────────────
echo ""
echo "══════════════════════════════════════════════"
echo " Globalny warmup SQL Server (2 × 90s)"
echo "══════════════════════════════════════════════"
warmup_api minimal
warmup_api controllers

echo ""
echo "── Pauza 30s po warmupie ──"
sleep 30

# ── Testy właściwe — kolejność: minimal przed controllers, jeden scenariusz na raz
run crud-load minimal
cooldown

run crud-load controllers
cooldown

run auth-flow minimal
cooldown

run auth-flow controllers
cooldown

run read-heavy minimal
cooldown

run read-heavy controllers

echo ""
echo "══════════════════════════════════════════════"
echo " k6: wszystkie scenariusze zakończone."
echo " Wyniki: k6/results/"
echo "══════════════════════════════════════════════"
