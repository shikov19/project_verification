"""
Fetches closed bug issues from GitHub using the Search API
split into monthly chunks to avoid the 1000-result cap.

Usage:
    python fetch_github_bugs.py

Output:
    github_bugs.csv  —  two columns: period (month index), cumulative_bugs
"""

import urllib.request
import urllib.parse
import json
import time
from datetime import datetime
from collections import defaultdict
import calendar

# ── Configuration ────────────────────────────────────────────────
REPO        = "microsoft/vscode"
YEAR        = 2024           # Which year to analyze
OUTPUT_FILE = "github_bugs.csv"
TOKEN = ""  # Set your GitHub personal access token here, or use env var GITHUB_TOKEN
# TOKEN = os.environ.get("GITHUB_TOKEN", "")
# ─────────────────────────────────────────────────────────────────

def fetch_month(repo, year, month):
    """Fetch all bug issues for a single month."""
    # First and last day of the month
    last_day = calendar.monthrange(year, month)[1]
    start = f"{year}-{month:02d}-01"
    end   = f"{year}-{month:02d}-{last_day}"

    query = f'repo:{repo} label:bug is:issue is:closed created:{start}..{end}'

    all_items = []
    page = 1
    while True:
        params = urllib.parse.urlencode({
            "q":        query,
            "per_page": 100,
            "page":     page,
            "sort":     "created",
            "order":    "asc"
        })
        url = f"https://api.github.com/search/issues?{params}"
        req = urllib.request.Request(
            url,
            headers={
                "User-Agent": "SRGM-DataFetcher/1.0",
                "Accept":     "application/vnd.github.v3+json",
		"Authorization": f"Bearer {TOKEN}"
            }
        )
        try:
            with urllib.request.urlopen(req) as resp:
                data = json.loads(resp.read())
        except Exception as e:
            print(f"    Error: {e}")
            break

        batch = data.get("items", [])
        total = data.get("total_count", 0)
        all_items.extend(batch)

        if len(all_items) >= total or len(batch) < 100 or page >= 10:
            break

        page += 1
        time.sleep(1.0)

    return len(all_items)


def build_dataset(repo, year):
    print(f"Fetching monthly bug counts for {repo} — year {year}\n")
    rows = []
    cumulative = 0

    for month in range(1, 13):
        month_name = datetime(year, month, 1).strftime("%b %Y")
        count = fetch_month(repo, year, month)
        cumulative += count
        rows.append((month, cumulative))
        print(f"  Period {month:2d}  ({month_name}):  +{count:4d}  →  cumulative: {cumulative}")
        time.sleep(1.5)  # Respect rate limit between months

    return rows


def save_csv(rows, filename):
    with open(filename, "w") as f:
        for period, cum_bugs in rows:
            f.write(f"{period},{cum_bugs}\n")
    print(f"\nSaved {len(rows)} rows to '{filename}'")


if __name__ == "__main__":
    rows = build_dataset(REPO, YEAR)

    if rows:
        save_csv(rows, OUTPUT_FILE)
        print(f"Done! Load '{OUTPUT_FILE}' into your SRGM app.")
    else:
        print("No data to save.")
