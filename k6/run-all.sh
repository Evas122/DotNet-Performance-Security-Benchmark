#!/usr/bin/env bash
# Runs every k6 scenario against both APIs and saves JSON results to k6/results/.
#
# Prerequisites:
#   - k6 installed (https://k6.io/docs/getting-started/installation/)
#   - Both APIs running (docker compose up  OR  dotnet run)
#
# Usage:
#   chmod +x k6/run-all.sh
#   ./k6/run-all.sh
#
# Override API URLs:
#   BASE_URL_MINIMAL=http://localhost:5001 \
#   BASE_URL_CONTROLLERS=http://localhost:5002 \
#   ./k6/run-all.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RESULTS_DIR="${SCRIPT_DIR}/results"
mkdir -p "${RESULTS_DIR}"

BASE_URL_MINIMAL="${BASE_URL_MINIMAL:-http://localhost:5001}"
BASE_URL_CONTROLLERS="${BASE_URL_CONTROLLERS:-http://localhost:5002}"

TIMESTAMP="$(date +%Y%m%d_%H%M%S)"

run_scenario() {
    local scenario="$1"   # e.g. crud-load
    local api="$2"        # minimal | controllers
    local base_url="$3"

    local out_file="${RESULTS_DIR}/${scenario}_${api}_${TIMESTAMP}.json"
    local script="${SCRIPT_DIR}/scenarios/${scenario}.js"

    echo ""
    echo "════════════════════════════════════════════════════════"
    echo " Scenario : ${scenario}"
    echo " API      : ${api}  (${base_url})"
    echo " Output   : ${out_file}"
    echo "════════════════════════════════════════════════════════"

    k6 run \
        --env API="${api}" \
        --env BASE_URL_MINIMAL="${BASE_URL_MINIMAL}" \
        --env BASE_URL_CONTROLLERS="${BASE_URL_CONTROLLERS}" \
        --out "json=${out_file}" \
        "${script}" \
        || echo "[WARN] ${scenario}/${api} finished with non-zero exit (check thresholds)"
}

echo "╔══════════════════════════════════════════════════════════╗"
echo "║         SecPerf k6 Load Test Suite                      ║"
echo "║  Timestamp : ${TIMESTAMP}                    ║"
echo "╚══════════════════════════════════════════════════════════╝"

# ── CRUD load: ramp-up → sustained → spike ────────────────────────────────────
run_scenario "crud-load" "minimal"     "${BASE_URL_MINIMAL}"
run_scenario "crud-load" "controllers" "${BASE_URL_CONTROLLERS}"

# ── Auth flow: register → login → secured → refresh → revoke ─────────────────
run_scenario "auth-flow" "minimal"     "${BASE_URL_MINIMAL}"
run_scenario "auth-flow" "controllers" "${BASE_URL_CONTROLLERS}"

echo ""
echo "════════════════════════════════════════════════════════"
echo " All scenarios complete."
echo " Results saved to: k6/results/"
echo "════════════════════════════════════════════════════════"
