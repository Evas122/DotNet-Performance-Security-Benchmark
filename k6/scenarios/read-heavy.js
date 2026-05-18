// Read-heavy scenario — 80% GET /api/products, 20% GET /api/products/:id
// Symuluje ruch produkcyjny gdzie zdecydowana większość requestów to odczyty.
// Brak zapisów — mierzy czysty throughput warstwy odczytu + cache DB.
//
// k6 run k6/scenarios/read-heavy.js
// k6 run --env API=controllers k6/scenarios/read-heavy.js

import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate, Counter } from "k6/metrics";
import { resolveBaseUrl, commonThresholds } from "../config.js";
import { register, authHeaders } from "../helpers/auth.js";

const listDuration   = new Trend("read_list_duration",   true);
const singleDuration = new Trend("read_single_duration", true);
const errorRate      = new Rate("read_error_rate");
const listRequests   = new Counter("read_list_count");
const singleRequests = new Counter("read_single_count");

// Oś czasu:
//   t=  0s  warmup    5 VU / 30s
//   t= 35s  ramp_up   0→150 VU / 60s  (wyższy peak niż crud-load — tylko odczyty)
//   t=100s  sustained 150 VU / 180s   (3 minuty stabilnego odczytu)
//   t=285s  spike     150→600 VU / 60s (maksymalny stress na read path)

export const options = {
    scenarios: {
        warmup: {
            executor:  "constant-vus",
            vus:       5,
            duration:  "30s",
            startTime: "0s",
            exec:      "warmupFn",
            gracefulStop: "5s",
        },
        ramp_up: {
            executor:  "ramping-vus",
            startVUs:  0,
            stages: [
                { duration: "60s", target: 150 },
                { duration: "5s",  target: 0   },
            ],
            startTime: "35s",
            gracefulRampDown: "10s",
        },
        sustained: {
            executor:  "constant-vus",
            vus:       150,
            duration:  "180s",
            startTime: "105s",
        },
        spike: {
            executor:  "ramping-vus",
            startVUs:  150,
            stages: [
                { duration: "30s", target: 600 },
                { duration: "30s", target: 0   },
            ],
            startTime: "290s",
            gracefulRampDown: "10s",
        },
    },
    thresholds: {
        ...commonThresholds,
        "read_list_duration":   ["p(95)<800" ],
        "read_single_duration": ["p(95)<500" ],
        "read_error_rate":      ["rate<0.005"],
    },
};

const BASE_URL = resolveBaseUrl();

export function setup() {
    const email = `read_${Date.now()}@test.com`;
    return register(BASE_URL, email);
}

export function warmupFn() {
    http.get(`${BASE_URL}/health`);
    http.get(`${BASE_URL}/api/products`);
    sleep(1);
}

export default function (tokens) {
    const headers = authHeaders(tokens.accessToken);
    const roll = Math.random();

    if (roll < 0.80) {
        // 80% — lista produktów
        const res = http.get(`${BASE_URL}/api/products`, headers);
        listDuration.add(res.timings.duration);
        listRequests.add(1);
        const ok = check(res, { "GET /api/products → 200": (r) => r.status === 200 });
        errorRate.add(!ok);

        // Pobierz id pierwszego produktu do użycia w 20% przypadków
        if (ok) {
            try {
                const products = res.json();
                if (Array.isArray(products) && products.length > 0) {
                    // Zapisz id w VU-scope dla kolejnych iteracji (uproszczenie)
                    __ENV._LAST_ID = products[Math.floor(Math.random() * Math.min(products.length, 10))].id;
                }
            } catch (_) {}
        }
    } else {
        // 20% — pojedynczy produkt
        const id = __ENV._LAST_ID || "00000000-0000-0000-0000-000000000001";
        const res = http.get(`${BASE_URL}/api/products/${id}`, headers);
        singleDuration.add(res.timings.duration);
        singleRequests.add(1);
        const ok = check(res, { "GET /api/products/:id → 200 or 404": (r) => r.status === 200 || r.status === 404 });
        errorRate.add(!ok);
    }

    sleep(0.1);  // krótki sleep — read-heavy może obsłużyć więcej req/s
}
