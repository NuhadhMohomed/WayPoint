"""
WayPoint High-Concurrency Load & Performance Benchmark Runner
=============================================================
Module: SE3110 - Quality Management in Software Engineering
Testing Area: Non-Functional Performance & Concurrency Testing

Features:
- Simulates 50-100 concurrent virtual requests
- Evaluates endpoint response times and latency distributions (P50, P90, P95, P99)
- Measures concurrency conflicts and transactional isolation (BR-HOLD-001)
- Generates JSON summary evidence for SE3110 submission
"""

import os
import sys
import time
import json
import statistics
from concurrent.futures import ThreadPoolExecutor, as_completed
import requests

BASE_URL = os.environ.get("WAYPOINT_API_URL", "https://waypoint-production-87d7.up.railway.app").rstrip("/")

def benchmark_endpoint(name, url, method="GET", payload=None, headers=None, concurrency=25, total_requests=100):
    print(f"\n=======================================================")
    print(f"Benchmarking: {name}")
    print(f"Target: {url}")
    print(f"Concurrency: {concurrency} workers | Total: {total_requests} requests")
    print(f"=======================================================")

    latencies = []
    status_counts = {}
    start_time = time.time()

    def do_request(req_id):
        req_start = time.time()
        try:
            if method.upper() == "POST":
                resp = requests.post(url, json=payload, headers=headers, timeout=15)
            else:
                resp = requests.get(url, headers=headers, timeout=15)
            duration_ms = (time.time() - req_start) * 1000.0
            return resp.status_code, duration_ms
        except Exception as e:
            duration_ms = (time.time() - req_start) * 1000.0
            return f"ERR:{type(e).__name__}", duration_ms

    with ThreadPoolExecutor(max_workers=concurrency) as executor:
        futures = [executor.submit(do_request, i) for i in range(total_requests)]
        for f in as_completed(futures):
            code, dur = f.result()
            latencies.append(dur)
            status_counts[str(code)] = status_counts.get(str(code), 0) + 1

    total_time = time.time() - start_time
    rps = total_requests / total_time if total_time > 0 else 0

    latencies.sort()
    p50 = statistics.median(latencies)
    p90 = latencies[int(len(latencies) * 0.90)]
    p95 = latencies[int(len(latencies) * 0.95)]
    p99 = latencies[int(len(latencies) * 0.99)]
    avg_lat = statistics.mean(latencies)

    results = {
        "benchmark_name": name,
        "endpoint": url,
        "concurrency": concurrency,
        "total_requests": total_requests,
        "total_time_seconds": round(total_time, 2),
        "requests_per_second": round(rps, 2),
        "status_distribution": status_counts,
        "latency_stats_ms": {
            "min": round(min(latencies), 2),
            "avg": round(avg_lat, 2),
            "p50_median": round(p50, 2),
            "p90": round(p90, 2),
            "p95": round(p95, 2),
            "p99": round(p99, 2),
            "max": round(max(latencies), 2)
        }
    }

    print(f"Completed in {round(total_time, 2)}s | Throughput: {round(rps, 2)} req/sec")
    print(f"Status codes: {status_counts}")
    print(f"Latency: Avg={round(avg_lat, 2)}ms | P50={round(p50, 2)}ms | P95={round(p95, 2)}ms | P99={round(p99, 2)}ms")
    return results

def run_all_benchmarks():
    headers = {"Content-Type": "application/json"}
    
    # Obtain auth token
    login_resp = requests.post(
        f"{BASE_URL}/api/v1/auth/login",
        json={"email": "passenger@waypoint.lk", "password": "Password123!"},
        headers=headers,
        timeout=10
    )
    token = login_resp.json().get("token") if login_resp.status_code == 200 else ""
    auth_headers = {"Content-Type": "application/json", "Authorization": f"Bearer {token}"}

    all_results = []

    # 1. System Health Check Benchmark
    res_health = benchmark_endpoint(
        name="System Health & Readiness Endpoint",
        url=f"{BASE_URL}/health",
        method="GET",
        headers=headers,
        concurrency=20,
        total_requests=50
    )
    all_results.append(res_health)

    # 2. Journey Search Query Benchmark
    res_search = benchmark_endpoint(
        name="Intercity Journey Search (High Volume)",
        url=f"{BASE_URL}/api/v1/journeysearch/search",
        method="POST",
        payload={"origin": "Colombo", "destination": "Kandy", "date": "2026-10-15", "passengerCount": 1},
        headers=auth_headers,
        concurrency=20,
        total_requests=50
    )
    all_results.append(res_search)

    # 3. High-Concurrency Seat Hold Contention Benchmark (BR-HOLD-001)
    res_hold = benchmark_endpoint(
        name="Seat Hold Concurrency Contention (Double-Booking Barrier)",
        url=f"{BASE_URL}/api/v1/seathold",
        method="POST",
        payload={
            "serviceId": "00000000-0000-0000-0000-000000000001",
            "seatNumbers": ["12A"],
            "passengerName": "Concurrent_Benchmark_User"
        },
        headers=auth_headers,
        concurrency=25,
        total_requests=50
    )
    all_results.append(res_hold)

    output_path = os.path.join(os.path.dirname(__file__), "performance_benchmark_results.json")
    with open(output_path, "w") as f:
        json.dump(all_results, f, indent=2)
    print(f"\n[EVIDENCE GENERATED] Benchmark results exported to: {output_path}")

if __name__ == "__main__":
    run_all_benchmarks()
