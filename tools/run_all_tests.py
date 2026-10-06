"""
WayPoint Monorepo Master Test Execution & Quality Gate Runner
=============================================================
Module: SE3110 - Quality Management in Software Engineering
Unified Test Execution Harness across all subsystems.
"""

import os
import sys
import time
import subprocess
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

SUITES = [
    {
        "name": "Backend (.NET 8 xUnit)",
        "cmd": ["dotnet", "test", "backend/WayPoint.sln", "--configuration", "Release"],
        "cwd": REPO_ROOT,
        "category": "Backend / API Testing"
    },
    {
        "name": "Web Frontend (React / Vitest)",
        "cmd": ["npm", "test"],
        "cwd": REPO_ROOT / "web",
        "category": "React Web Application Testing"
    },
    {
        "name": "Mobile Client (Flutter Test)",
        "cmd": ["flutter", "test"],
        "cwd": REPO_ROOT / "mobile",
        "category": "Flutter Mobile Application Testing"
    },
    {
        "name": "Agentic AI Subsystem (Pytest)",
        "cmd": ["pytest", "tests/", "-v"],
        "cwd": REPO_ROOT / "ai",
        "category": "Agentic AI Testing & Evaluation"
    },
    {
        "name": "Closed-Loop E2E Workflow",
        "cmd": ["pytest", "tests/e2e/test_closed_loop_workflow.py", "-v"],
        "cwd": REPO_ROOT,
        "category": "Integration / End-to-End Testing"
    },
    {
        "name": "Cross-Service API Integration",
        "cmd": ["pytest", "tests/integration/test_cross_service_api.py", "-v"],
        "cwd": REPO_ROOT,
        "category": "Database & Integration Testing"
    },
    {
        "name": "Security Audit & Pen-Test",
        "cmd": ["pytest", "tests/security/test_security_audit.py", "-v"],
        "cwd": REPO_ROOT,
        "category": "Non-Functional Security Testing"
    },
    {
        "name": "Concurrency Performance Benchmarks",
        "cmd": [sys.executable, "tests/performance/run_concurrency_benchmarks.py"],
        "cwd": REPO_ROOT,
        "category": "Non-Functional Performance Testing"
    }
]

def main():
    print("=" * 80)
    print("      WAYPOINT MONOREPO UNIFIED TEST SUITE & QUALITY EVALUATION")
    print("=" * 80)
    print(f"Base Directory: {REPO_ROOT}\n")

    results = []
    total_start = time.time()

    for idx, suite in enumerate(SUITES, 1):
        print(f"[{idx}/{len(SUITES)}] Running {suite['name']} ({suite['category']})...")
        start = time.time()
        try:
            res = subprocess.run(
                suite["cmd"],
                cwd=str(suite["cwd"]),
                capture_output=True,
                text=True,
                shell=(sys.platform == "win32" and suite["cmd"][0] in ["npm"])
            )
            elapsed = time.time() - start
            success = (res.returncode == 0)
            status_text = "PASS" if success else "FAIL"
            print(f"    -> Result: {status_text} in {elapsed:.2f}s")
            results.append({
                "name": suite["name"],
                "category": suite["category"],
                "status": status_text,
                "duration": elapsed,
                "error": res.stderr if not success else ""
            })
        except Exception as e:
            elapsed = time.time() - start
            print(f"    -> Result: ERROR ({e}) in {elapsed:.2f}s")
            results.append({
                "name": suite["name"],
                "category": suite["category"],
                "status": "ERROR",
                "duration": elapsed,
                "error": str(e)
            })

    total_time = time.time() - total_start

    print("\n" + "=" * 80)
    print("                         SUMMARY SCORECARD")
    print("=" * 80)
    print(f"{'Test Suite':<35} | {'Category':<32} | {'Status':<6} | {'Time':<6}")
    print("-" * 80)
    all_passed = True
    for r in results:
        status_symbol = "✓ PASS" if r["status"] == "PASS" else "✗ FAIL"
        if r["status"] != "PASS":
            all_passed = False
        print(f"{r['name']:<35} | {r['category']:<32} | {status_symbol:<6} | {r['duration']:>5.1f}s")
    print("-" * 80)
    print(f"Total Execution Time: {total_time:.1f}s")
    if all_passed:
        print(">>> OVERALL STATUS: ALL TEST SUITES PASSED (100% SUCCESS) <<<")
    else:
        print(">>> OVERALL STATUS: SOME TESTS FAILED <<<")
    print("=" * 80)

if __name__ == "__main__":
    main()
