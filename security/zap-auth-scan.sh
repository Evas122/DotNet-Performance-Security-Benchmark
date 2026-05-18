#!/bin/sh
# ZAP authenticated baseline scan — uruchamiany wewnątrz kontenera zaproxy/zap-stable.
# Rejestruje użytkownika, pobiera JWT, przekazuje go jako nagłówek Authorization
# aby ZAP mógł przeskanować chronione endpointy (/api/products POST, /api/auth/revoke itd.)
#
# Użycie w docker-compose (entrypoint override):
#   entrypoint: ["/bin/sh", "/scripts/zap-auth-scan.sh"]
#   environment:
#     TARGET: http://api-minimal:8080
#     REPORT_BASE: minimal-api-auth

set -e

TARGET="${TARGET:-http://api-minimal:8080}"
REPORT_BASE="${REPORT_BASE:-api-auth}"
REPORTS_DIR="/zap/wrk"

echo "[ZAP-auth] Rejestracja użytkownika skanującego..."

RESPONSE=$(curl -sf -X POST "${TARGET}/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"zap_${REPORT_BASE}@scan.local\",\"firstName\":\"ZAP\",\"lastName\":\"Scanner\",\"password\":\"ZapScan123!\"}" \
  || echo "{}")

# Wyciągnij token — ZAP image ma Pythona
TOKEN=$(echo "${RESPONSE}" | python3 -c \
  "import sys,json; d=json.load(sys.stdin); print(d.get('accessToken',''))" 2>/dev/null || echo "")

if [ -z "${TOKEN}" ]; then
  echo "[ZAP-auth] Nie udało się pobrać tokenu — skan bez autoryzacji."
else
  echo "[ZAP-auth] Token pobrany. Uruchamiam skan autoryzowany..."
fi

AUTH_FLAG=""
if [ -n "${TOKEN}" ]; then
  AUTH_FLAG="-config replacer.full_list(0).matchtype=REQ_HEADER \
             -config replacer.full_list(0).matchstr=Authorization \
             -config replacer.full_list(0).replacement=Bearer\ ${TOKEN} \
             -config replacer.full_list(0).initiators="
fi

zap-baseline.py \
  -t "${TARGET}" \
  -r "${REPORTS_DIR}/${REPORT_BASE}-report.html" \
  -x "${REPORTS_DIR}/${REPORT_BASE}-report.xml" \
  -I \
  ${AUTH_FLAG} \
  || true

echo "[ZAP-auth] Skan zakończony. Raport: ${REPORT_BASE}-report.html"
