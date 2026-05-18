// Auth flow test — measures each step of the JWT lifecycle independently.
//
// Steps measured separately:
//   1. register   → POST /api/auth/register
//   2. login      → POST /api/auth/login
//   3. secured    → GET  /api/products  (with Bearer token)
//   4. refresh    → POST /api/auth/refresh
//   5. revoke     → POST /api/auth/revoke
//
// Run:
//   k6 run k6/scenarios/auth-flow.js
//   k6 run --env API=controllers k6/scenarios/auth-flow.js
//   k6 run --out json=k6/results/auth-minimal.json k6/scenarios/auth-flow.js

import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate } from "k6/metrics";
import { resolveBaseUrl, commonThresholds } from "../config.js";

// ── Custom per-step metrics ───────────────────────────────────────────────────
const registerDuration = new Trend("auth_register_duration", true);
const loginDuration    = new Trend("auth_login_duration",    true);
const securedDuration  = new Trend("auth_secured_duration",  true);
const refreshDuration  = new Trend("auth_refresh_duration",  true);
const revokeDuration   = new Trend("auth_revoke_duration",   true);
const authErrorRate    = new Rate("auth_error_rate");

// ── Options ───────────────────────────────────────────────────────────────────
// Oś czasu:
//   t= 0s  warmup    3 VU / 30s  — rozgrzanie JIT + puli połączeń DB
//   t=35s  auth_flow 0→50→50→0 VU / ~105s

export const options = {
    scenarios: {
        // ── Warmup ────────────────────────────────────────────────────────────
        warmup: {
            executor:  "constant-vus",
            vus:       3,
            duration:  "30s",
            startTime: "0s",
            exec:      "warmupFn",
            gracefulStop: "5s",
        },

        // ── Auth flow ─────────────────────────────────────────────────────────
        auth_flow: {
            executor: "ramping-vus",
            startVUs: 0,
            stages: [
                { duration: "30s", target: 50  },  // ramp up
                { duration: "60s", target: 50  },  // sustained
                { duration: "15s", target: 0   },  // cool down
            ],
            startTime: "35s",                      // po warmup + 5s margines
            gracefulRampDown: "10s",
        },
    },
    thresholds: {
        ...commonThresholds,
        "auth_register_duration": ["p(95)<1000"],
        "auth_login_duration":    ["p(95)<500" ],
        "auth_secured_duration":  ["p(95)<500" ],
        "auth_refresh_duration":  ["p(95)<500" ],
        "auth_revoke_duration":   ["p(95)<500" ],
    },
};

const BASE_URL = resolveBaseUrl();
const JSON_HEADERS = { headers: { "Content-Type": "application/json" } };

// ── Warmup iteration ──────────────────────────────────────────────────────────
export function warmupFn() {
    http.get(`${BASE_URL}/health`);
    http.post(`${BASE_URL}/api/auth/login`,
        JSON.stringify({ email: "warmup@warmup.com", password: "warmup" }),
        JSON_HEADERS);
    sleep(1);
}

// ── Main iteration — each VU runs the full auth lifecycle ─────────────────────
export default function () {
    const email    = `vu${__VU}_iter${__ITER}_${Date.now()}@loadtest.com`;
    const password = "Password123!";

    // ── Step 1: Register ──────────────────────────────────────────────────────
    const regRes = http.post(
        `${BASE_URL}/api/auth/register`,
        JSON.stringify({ email, firstName: "Load", lastName: "Tester", password }),
        JSON_HEADERS,
    );
    registerDuration.add(regRes.timings.duration);
    const regOk = check(regRes, { "register → 200": (r) => r.status === 200 });
    authErrorRate.add(!regOk);

    if (!regOk) { sleep(1); return; }

    const regBody      = regRes.json();
    let accessToken    = regBody.accessToken;
    let currentRefresh = regBody.refreshToken;

    sleep(0.3);

    // ── Step 2: Login (separate measurement) ─────────────────────────────────
    const loginRes = http.post(
        `${BASE_URL}/api/auth/login`,
        JSON.stringify({ email, password }),
        JSON_HEADERS,
    );
    loginDuration.add(loginRes.timings.duration);
    const loginOk = check(loginRes, { "login → 200": (r) => r.status === 200 });
    authErrorRate.add(!loginOk);

    if (loginOk) {
        const loginBody = loginRes.json();
        accessToken    = loginBody.accessToken;
        currentRefresh = loginBody.refreshToken;
    }

    sleep(0.3);

    // ── Step 3: Secured request ───────────────────────────────────────────────
    const securedRes = http.get(
        `${BASE_URL}/api/products`,
        { headers: { "Authorization": `Bearer ${accessToken}` } },
    );
    securedDuration.add(securedRes.timings.duration);
    const securedOk = check(securedRes, { "secured GET → 200": (r) => r.status === 200 });
    authErrorRate.add(!securedOk);

    sleep(0.3);

    // ── Step 4: Refresh token ─────────────────────────────────────────────────
    const refreshRes = http.post(
        `${BASE_URL}/api/auth/refresh`,
        JSON.stringify({ refreshToken: currentRefresh }),
        JSON_HEADERS,
    );
    refreshDuration.add(refreshRes.timings.duration);
    const refreshOk = check(refreshRes, { "refresh → 200": (r) => r.status === 200 });
    authErrorRate.add(!refreshOk);

    if (refreshOk) {
        const refreshBody = refreshRes.json();
        accessToken    = refreshBody.accessToken;
        currentRefresh = refreshBody.refreshToken;
    }

    sleep(0.3);

    // ── Step 5: Revoke token ──────────────────────────────────────────────────
    const revokeRes = http.post(
        `${BASE_URL}/api/auth/revoke`,
        JSON.stringify({ refreshToken: currentRefresh }),
        {
            headers: {
                "Content-Type":  "application/json",
                "Authorization": `Bearer ${accessToken}`,
            },
        },
    );
    revokeDuration.add(revokeRes.timings.duration);
    const revokeOk = check(revokeRes, { "revoke → 200": (r) => r.status === 200 });
    authErrorRate.add(!revokeOk);

    sleep(1);
}
