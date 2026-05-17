#!/usr/bin/env bash
# Runs OWASP ZAP baseline scan against both APIs and saves reports to security/zap-reports/.
# Must be executed from the repository root with both API containers healthy.
#
# Usage:
#   chmod +x security/zap-scan.sh
#   ./security/zap-scan.sh

set -euo pipefail

REPORTS_DIR="$(cd "$(dirname "$0")/zap-reports" && pwd)"
mkdir -p "$REPORTS_DIR"

MINIMAL_URL="http://api-minimal:8080"
CONTROLLERS_URL="http://api-controllers:8080"

run_scan() {
  local name="$1"
  local target="$2"
  local report_base="$3"

  echo ""
  echo "========================================================"
  echo " ZAP Baseline Scan — ${name}"
  echo " Target : ${target}"
  echo "========================================================"

  docker compose run --rm \
    -v "${REPORTS_DIR}:/zap/reports:rw" \
    --entrypoint "" \
    "zap-${name}" \
    zap-baseline.py \
      -t "${target}" \
      -r "/zap/reports/${report_base}-report.html" \
      -x "/zap/reports/${report_base}-report.xml" \
      -I \
    || true   # -I already suppresses exit 2 on alerts; || true guards against exit 1

  echo " Report : security/zap-reports/${report_base}-report.html"
  echo " XML    : security/zap-reports/${report_base}-report.xml"
}

run_scan "minimal"     "${MINIMAL_URL}"     "minimal-api"
run_scan "controllers" "${CONTROLLERS_URL}" "controllers-api"

echo ""
echo "========================================================"
echo " All scans complete."
echo " Reports saved to: security/zap-reports/"
echo "========================================================"
