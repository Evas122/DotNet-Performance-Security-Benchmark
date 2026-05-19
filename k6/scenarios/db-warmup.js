// db-warmup.js — rozgrzewa SQL Server cache dla obu API przed testami pomiarowymi.
// Uruchamiany raz przed każdym zestawem testów, nie generuje wyników pomiarowych.
// Wykonuje te same zapytania co testy właściwe żeby cache trafień SQL był identyczny.

import http from "k6/http";
import { sleep } from "k6";
import { resolveBaseUrl } from "../config.js";
import { register, authHeaders } from "../helpers/auth.js";

export const options = {
    vus:      20,
    duration: "90s",
    discardResponseBodies: true,
    thresholds: {},    // brak thresholdów — to tylko warmup
};

const BASE_URL = resolveBaseUrl();

export function setup() {
    const email = `warmup_${Date.now()}@warmup.local`;
    return register(BASE_URL, email);
}

export default function (tokens) {
    const headers = authHeaders(tokens.accessToken);

    http.get(`${BASE_URL}/api/products`, headers);
    sleep(0.15);
    http.get(`${BASE_URL}/api/products?page=2`, headers);
    sleep(0.15);
    http.get(`${BASE_URL}/api/products?page=3`, headers);
    sleep(0.15);
    http.get(`${BASE_URL}/api/orders`, headers);
    sleep(0.15);
    http.get(`${BASE_URL}/api/orders?page=2`, headers);
    sleep(0.15);
    http.get(`${BASE_URL}/api/orders?page=3`, headers);
    sleep(0.15);
}
