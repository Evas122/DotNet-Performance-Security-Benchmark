// Read-heavy scenario — 60% GET /api/products, 20% GET /api/products/:id, 20% GET /api/orders
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

const listDuration    = new Trend("read_list_duration",    true);
const singleDuration  = new Trend("read_single_duration",  true);
const ordersListDur   = new Trend("read_orders_duration",  true);
const errorRate       = new Rate("read_error_rate");
const listRequests    = new Counter("read_list_count");
const singleRequests  = new Counter("read_single_count");
const ordersRequests  = new Counter("read_orders_count");

// Oś czasu:
//   t=  0s  warmup    5 VU / 30s
//   t= 35s  ramp_up   0→150 VU / 60s  (wyższy peak niż crud-load — tylko odczyty)
//   t=100s  sustained 150 VU / 180s   (3 minuty stabilnego odczytu)
//   t=285s  spike     150→600 VU / 60s (maksymalny stress na read path)

export const options = {
    scenarios: {
        warmup: {
            executor:  "constant-vus",
            vus:       20,
            duration:  "60s",
            startTime: "0s",
            exec:      "warmupFn",
            gracefulStop: "5s",
        },
        ramp_up: {
            executor:  "ramping-vus",
            startVUs:  0,
            stages: [
                { duration: "60s", target: 50 },
                { duration: "5s",  target: 0  },
            ],
            startTime: "65s",
            gracefulRampDown: "10s",
        },
        sustained: {
            executor:  "constant-vus",
            vus:       50,
            duration:  "180s",
            startTime: "135s",
        },
        spike: {
            executor:  "ramping-vus",
            startVUs:  50,
            stages: [
                { duration: "30s", target: 150 },
                { duration: "30s", target: 0   },
            ],
            startTime: "320s",
            gracefulRampDown: "10s",
        },
    },
    thresholds: {
        ...commonThresholds,
        "read_list_duration":   ["p(95)<800" ],
        "read_single_duration": ["p(95)<500" ],
        "read_orders_duration": ["p(95)<2000"],
        "read_error_rate":      ["rate<0.005"],
    },
};

const BASE_URL = resolveBaseUrl();

// VU-scoped cache — przechowuje id ostatnio pobranych zasobów
let lastProductId = null;
let lastOrderId   = null;

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

    if (roll < 0.60) {
        // 60% — lista produktów
        const res = http.get(`${BASE_URL}/api/products`, headers);
        listDuration.add(res.timings.duration);
        listRequests.add(1);
        const ok = check(res, { "GET /api/products → 200": (r) => r.status === 200 });
        errorRate.add(!ok);

        if (ok) {
            try {
                const products = res.json();
                if (Array.isArray(products) && products.length > 0) {
                    lastProductId = products[Math.floor(Math.random() * Math.min(products.length, 10))].id;
                }
            } catch (_) {}
        }
    } else if (roll < 0.80) {
        // 20% — pojedynczy produkt
        const id = lastProductId || "00000000-0000-0000-0000-000000000001";
        const res = http.get(`${BASE_URL}/api/products/${id}`, headers);
        singleDuration.add(res.timings.duration);
        singleRequests.add(1);
        const ok = check(res, { "GET /api/products/:id → 200 or 404": (r) => r.status === 200 || r.status === 404 });
        errorRate.add(!ok);
    } else {
        // 20% — lista zamówień (heaviest read — join Products+OrderItems na 100k rekordów)
        const res = http.get(`${BASE_URL}/api/orders`, headers);
        ordersListDur.add(res.timings.duration);
        ordersRequests.add(1);
        const ok = check(res, { "GET /api/orders → 200": (r) => r.status === 200 });
        errorRate.add(!ok);

        if (ok) {
            try {
                const orders = res.json();
                if (Array.isArray(orders) && orders.length > 0) {
                    lastOrderId = orders[Math.floor(Math.random() * Math.min(orders.length, 10))].id;
                }
            } catch (_) {}
        }
    }

    sleep(0.1);  // krótki sleep — read-heavy może obsłużyć więcej req/s
}
