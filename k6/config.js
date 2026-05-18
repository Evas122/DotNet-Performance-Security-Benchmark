// k6 shared configuration
// All scenarios import BASE_URL_MINIMAL / BASE_URL_CONTROLLERS from here.
// Override at runtime via k6 --env flag:
//   k6 run --env API=minimal scenarios/crud-load.js

export const BASE_URL_MINIMAL     = __ENV.BASE_URL_MINIMAL     || "http://localhost:5001";
export const BASE_URL_CONTROLLERS = __ENV.BASE_URL_CONTROLLERS || "http://localhost:5002";

// Which API to target when a scenario runs against one API at a time
// Set --env API=minimal  or  --env API=controllers  (default: minimal)
export function resolveBaseUrl() {
    return (__ENV.API === "controllers")
        ? BASE_URL_CONTROLLERS
        : BASE_URL_MINIMAL;
}

// Shared thresholds — applied to every scenario unless overridden
export const commonThresholds = {
    http_req_failed:   [{ threshold: "rate<0.01",  abortOnFail: false }], // <1 % error rate
    http_req_duration: [{ threshold: "p(95)<2000", abortOnFail: false }], // p95 < 2 s
};
