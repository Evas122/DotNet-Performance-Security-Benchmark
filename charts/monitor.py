"""
monitor.py — próbkuje CPU% i RAM obu kontenerów API co 2 sekundy.
Uruchamiany jako osobny kontener podczas testów k6.

Zapis: /results/resources.csv
Kolumny: timestamp_s, name, cpu_pct, mem_mb, mem_limit_mb, mem_pct
"""

import csv
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

try:
    import docker
except ImportError:
    print("[ERROR] Zainstaluj: pip install docker")
    sys.exit(1)

# Discover containers by Compose service label — works regardless of project folder name
SERVICES   = ["api-minimal", "api-controllers"]
OUTPUT     = Path("/results/resources.csv")
INTERVAL   = 2      # sekundy między próbkami
DURATION   = int(sys.argv[1]) if len(sys.argv) > 1 else 1800  # domyślnie 30 min


def cpu_percent(stats: dict) -> float:
    try:
        cpu_delta    = (stats["cpu_stats"]["cpu_usage"]["total_usage"]
                        - stats["precpu_stats"]["cpu_usage"]["total_usage"])
        system_delta = (stats["cpu_stats"]["system_cpu_usage"]
                        - stats["precpu_stats"]["system_cpu_usage"])
        num_cpus     = stats["cpu_stats"].get("online_cpus", 1)
        if system_delta <= 0:
            return 0.0
        return round((cpu_delta / system_delta) * num_cpus * 100, 2)
    except (KeyError, ZeroDivisionError):
        return 0.0


def mem_stats(stats: dict) -> tuple[float, float, float]:
    try:
        usage   = stats["memory_stats"]["usage"]
        limit   = stats["memory_stats"]["limit"]
        usage_mb  = round(usage  / 1024 / 1024, 1)
        limit_mb  = round(limit  / 1024 / 1024, 1)
        pct       = round((usage / limit) * 100, 2) if limit > 0 else 0.0
        return usage_mb, limit_mb, pct
    except (KeyError, ZeroDivisionError):
        return 0.0, 0.0, 0.0


def main():
    client   = docker.from_env()
    end_time = time.time() + DURATION

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    print(f"[monitor] Start — próbkowanie co {INTERVAL}s przez {DURATION}s → {OUTPUT}")

    with open(OUTPUT, "w", newline="") as fh:
        writer = csv.writer(fh)
        writer.writerow(["timestamp_s", "name", "cpu_pct", "mem_mb", "mem_limit_mb", "mem_pct"])

        while time.time() < end_time:
            ts = int(time.time())
            for service in SERVICES:
                try:
                    matches = client.containers.list(
                        filters={"label": f"com.docker.compose.service={service}", "status": "running"}
                    )
                    if not matches:
                        continue
                    container = matches[0]
                    stats     = container.stats(stream=False)
                    cpu       = cpu_percent(stats)
                    mem, lim, mem_pct = mem_stats(stats)
                    writer.writerow([ts, service, cpu, mem, lim, mem_pct])
                except Exception as e:
                    print(f"[monitor] {service}: {e}", flush=True)

            fh.flush()
            time.sleep(INTERVAL)

    print("[monitor] Zakończono.")


if __name__ == "__main__":
    main()
