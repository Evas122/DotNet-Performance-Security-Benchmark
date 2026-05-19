// CRUD load test — 3 scenarios in sequence against one API.
//
// Run against Minimal API (default):
//   k6 run k6/scenarios/crud-load.js
//
// Run against Controller-based API:
//   k6 run --env API=controllers k6/scenarios/crud-load.js
//
// Save JSON results:
//   k6 run --out json=k6/results/crud-minimal.json k6/scenarios/crud-load.js
//
// Scenarios
// ─────────────────────────────────────────────────────────────────────────────
//  ramp_up   : 0 → 100 VU over 60 s  (measures throughput while load grows)
//  sustained : 100 VU for 120 s       (steady-state performance)
//  spike     : 100 → 500 VU over 30 s (resilience under sudden traffic surge)

import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate } from "k6/metrics";
import { resolveBaseUrl, commonThresholds } from "../config.js";
import { register, authHeaders } from "../helpers/auth.js";

// ── Custom metrics ────────────────────────────────────────────────────────────
const getProductsDuration    = new Trend("get_products_duration",    true);
const getProductByIdDuration = new Trend("get_product_by_id_duration", true);
const createProductDuration  = new Trend("create_product_duration",  true);
const getOrdersDuration      = new Trend("get_orders_duration",      true);
const getOrderByIdDuration   = new Trend("get_order_by_id_duration", true);
const errorRate              = new Rate("custom_error_rate");

// ── Options ───────────────────────────────────────────────────────────────────
// Oś czasu (wszystkie scenariusze sekwencyjne — jeden po drugim):
//
//   t=  0s  warmup     5 VU / 30s   — rozgrzanie JIT, brak pomiarów w wynikach
//   t= 35s  ramp_up    0→100 VU / 60s + 10s cool-down
//   t=110s  sustained  100 VU / 120s
//   t=235s  spike      100→500→100 VU / 70s
//
// Przerwy (startTime z marginesem 5s) gwarantują że poprzedni scenariusz
// w pełni zakończył pracę zanim następny ruszy.

export const options = {
    scenarios: {
        // ── Warmup — nie wlicza się do thresholdów ────────────────────────────
        warmup: {
            executor:  "constant-vus",
            vus:       20,
            duration:  "60s",
            startTime: "0s",
            exec:      "warmupFn",              // osobna funkcja, nie wpada do Trends
            gracefulStop: "5s",
        },

        // ── Ramp-up: 0 → 100 VU przez 60 s ───────────────────────────────────
        ramp_up: {
            executor:  "ramping-vus",
            startVUs:  0,
            stages: [
                { duration: "60s", target: 100 },
                { duration: "10s", target: 0   },   // cool-down
            ],
            startTime: "65s",                       // po warmup + 5s margines
            gracefulRampDown: "10s",
        },

        // ── Sustained: stały ruch 100 VU przez 120 s ──────────────────────────
        sustained: {
            executor:  "constant-vus",
            vus:       100,
            duration:  "120s",
            startTime: "140s",                      // po ramp_up
        },

        // ── Spike: 100 → 300 VU (nagły skok) ─────────────────────────────────
        spike: {
            executor:  "ramping-vus",
            startVUs:  100,
            stages: [
                { duration: "30s", target: 300 },
                { duration: "30s", target: 100 },
                { duration: "10s", target: 0   },
            ],
            startTime: "265s",                      // po sustained
            gracefulRampDown: "10s",
        },
    },
    thresholds: {
        ...commonThresholds,
        "get_products_duration":      ["p(95)<1500"],
        "get_product_by_id_duration": ["p(95)<1500"],
        "create_product_duration":    ["p(95)<2000"],
        "get_orders_duration":        ["p(95)<2000"],
        "get_order_by_id_duration":   ["p(95)<2000"],
    },
};

const BASE_URL = resolveBaseUrl();

export function setup() {
    const email = `loadtest_${Date.now()}_${Math.random().toString(36).slice(2)}@test.com`;
    const tokens = register(BASE_URL, email);

    // Fetch a real categoryId from seeded products so POST /api/products doesn't fail FK validation
    const res = http.get(`${BASE_URL}/api/products`, authHeaders(tokens.accessToken));
    let categoryId = null;
    try {
        const products = res.json();
        if (Array.isArray(products) && products.length > 0) {
            categoryId = products[0].categoryId;
        }
    } catch (_) {}

    return { ...tokens, categoryId };
}

// ── Warmup iteration — tylko proste GETs, nie dodaje do custom Trends ─────────
export function warmupFn() {
    http.get(`${BASE_URL}/health`);
    http.get(`${BASE_URL}/api/products`);
    sleep(1);
}

// ── Main iteration ────────────────────────────────────────────────────────────
export default function (tokens) {
    const headers = authHeaders(tokens.accessToken);

    // 1. GET /api/products — list all products
    const listRes = http.get(`${BASE_URL}/api/products`, headers);
    getProductsDuration.add(listRes.timings.duration);
    const listOk = check(listRes, {
        "GET /api/products → 200": (r) => r.status === 200,
    });
    errorRate.add(!listOk);

    sleep(0.5);

    // 2. GET /api/products/:id — fetch first product from list (if any)
    let productId = null;
    try {
        const products = listRes.json();
        if (Array.isArray(products) && products.length > 0) {
            productId = products[0].id;
        }
    } catch (_) {}

    if (productId) {
        const getRes = http.get(`${BASE_URL}/api/products/${productId}`, headers);
        getProductByIdDuration.add(getRes.timings.duration);
        const getOk = check(getRes, {
            "GET /api/products/:id → 200": (r) => r.status === 200,
        });
        errorRate.add(!getOk);
        sleep(0.5);
    }

    // 3. POST /api/products — create a new product
    const payload = JSON.stringify({
        name:        `LoadProduct_${__VU}_${__ITER}`,
        description: "Created by k6 load test",
        price:       9.99,
        stock:       100,
        categoryId:  tokens.categoryId,
    });

    const createRes = http.post(`${BASE_URL}/api/products`, payload, headers);
    createProductDuration.add(createRes.timings.duration);
    const createOk = check(createRes, {
        "POST /api/products → 200 or 201": (r) => r.status === 200 || r.status === 201,
    });
    errorRate.add(!createOk);

    sleep(0.5);

    // 4. GET /api/orders — list orders (paginated, uses seeded 100k orders)
    const ordersRes = http.get(`${BASE_URL}/api/orders`, headers);
    getOrdersDuration.add(ordersRes.timings.duration);
    const ordersOk = check(ordersRes, {
        "GET /api/orders → 200": (r) => r.status === 200,
    });
    errorRate.add(!ordersOk);

    sleep(0.5);

    // 5. GET /api/orders/:id — fetch a specific order if list returned results
    let orderId = null;
    try {
        const orders = ordersRes.json();
        if (Array.isArray(orders) && orders.length > 0) {
            orderId = orders[0].id;
        }
    } catch (_) {}

    if (orderId) {
        const orderByIdRes = http.get(`${BASE_URL}/api/orders/${orderId}`, headers);
        getOrderByIdDuration.add(orderByIdRes.timings.duration);
        const orderByIdOk = check(orderByIdRes, {
            "GET /api/orders/:id → 200": (r) => r.status === 200,
        });
        errorRate.add(!orderByIdOk);
    }

    sleep(1);
}
