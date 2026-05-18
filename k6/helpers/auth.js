// Auth helper — login and token refresh utilities.
// Used by every scenario that hits a secured endpoint.

import http from "k6/http";
import { check } from "k6";

const DEFAULT_PASSWORD = "Password123!";

/**
 * Register a new user and return { accessToken, refreshToken }.
 * Uses a unique e-mail so parallel VUs don't collide.
 */
export function register(baseUrl, email, password = DEFAULT_PASSWORD) {
    const payload = JSON.stringify({
        email:     email,
        firstName: "Load",
        lastName:  "Tester",
        password:  password,
    });

    const res = http.post(`${baseUrl}/api/auth/register`, payload, {
        headers: { "Content-Type": "application/json" },
    });

    check(res, { "register 200": (r) => r.status === 200 });

    const body = res.json();
    return {
        accessToken:  body.accessToken,
        refreshToken: body.refreshToken,
    };
}

/**
 * Login with an existing account and return { accessToken, refreshToken }.
 */
export function login(baseUrl, email, password = DEFAULT_PASSWORD) {
    const payload = JSON.stringify({ email, password });

    const res = http.post(`${baseUrl}/api/auth/login`, payload, {
        headers: { "Content-Type": "application/json" },
    });

    check(res, { "login 200": (r) => r.status === 200 });

    const body = res.json();
    return {
        accessToken:  body.accessToken,
        refreshToken: body.refreshToken,
    };
}

/**
 * Refresh an access token and return the new { accessToken, refreshToken }.
 */
export function refreshToken(baseUrl, refreshToken) {
    const payload = JSON.stringify({ refreshToken });

    const res = http.post(`${baseUrl}/api/auth/refresh`, payload, {
        headers: { "Content-Type": "application/json" },
    });

    check(res, { "refresh 200": (r) => r.status === 200 });

    const body = res.json();
    return {
        accessToken:  body.accessToken,
        refreshToken: body.refreshToken,
    };
}

/**
 * Return Authorization header object ready for use in http.get / http.post.
 */
export function authHeaders(accessToken) {
    return {
        headers: {
            "Content-Type":  "application/json",
            "Authorization": `Bearer ${accessToken}`,
        },
    };
}
