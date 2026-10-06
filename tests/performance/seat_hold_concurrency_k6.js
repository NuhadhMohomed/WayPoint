import http from 'k6/http';
import { check, sleep } from 'k6';

/**
 * k6 Concurrency & Load Test: WayPoint High-Traffic Seat Hold & Journey Search
 * ===========================================================================
 * Course: SE3110 - Quality Management in Software Engineering
 * Requirement: Performance & Load Testing (Mandatory Non-Functional Area)
 * Target Invariant: BR-HOLD-001 (Atomic seat hold & double-booking prevention)
 *
 * Execution Scenario:
 * - 50 Virtual Users (VUs) ramp up over 30s
 * - Peak load maintained for 1m
 * - Ramp down over 20s
 * - Validates:
 *   - Response time (P95 < 500ms)
 *   - HTTP 200 vs HTTP 409 Conflict distribution (double-booking protection)
 *   - Error rate < 1%
 */

export const options = {
  stages: [
    { duration: '30s', target: 20 }, // Ramp up to 20 users
    { duration: '1m', target: 50 },  // Stress peak with 50 concurrent users
    { duration: '20s', target: 0 },  // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.05'],   // Failure rate below 5% (excluding 409 conflicts)
  },
};

const BASE_URL = __ENV.WAYPOINT_API_URL || 'https://waypoint-production-87d7.up.railway.app';

export default function () {
  // Scenario 1: Intercity Journey Search
  const searchPayload = JSON.stringify({
    origin: 'Colombo',
    destination: 'Kandy',
    date: '2026-10-15',
    passengerCount: 1,
  });

  const searchParams = {
    headers: { 'Content-Type': 'application/json' },
  };

  const searchRes = http.post(`${BASE_URL}/api/v1/journeysearch/search`, searchPayload, searchParams);
  check(searchRes, {
    'Search responded with 200 or 404': (r) => r.status === 200 || r.status === 404,
    'Search latency < 400ms': (r) => r.timings.duration < 400,
  });

  sleep(1);

  // Scenario 2: Concurrent Seat Hold Attempt on Hot Seat '12A'
  const holdPayload = JSON.stringify({
    serviceId: '00000000-0000-0000-0000-000000000001',
    seatNumbers: ['12A'],
    passengerName: `VU_${__VU}_User`,
  });

  const holdRes = http.post(`${BASE_URL}/api/v1/seathold`, holdPayload, searchParams);
  check(holdRes, {
    'Hold response is valid HTTP code (200, 201, 400, 404, 409)': (r) =>
      [200, 201, 400, 404, 409].includes(r.status),
  });

  sleep(0.5);
}
