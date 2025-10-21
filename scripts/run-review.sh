#!/usr/bin/env bash
set -euo pipefail

# Pull request SHAs from GitHub event JSON
BASE_SHA=$(jq -r '.pull_request.base.sha // empty' "$GITHUB_EVENT_PATH")
HEAD_SHA=$(jq -r '.pull_request.head.sha // empty' "$GITHUB_EVENT_PATH")

PATCH_FILE=patch.diff

echo "Collecting diff ${BASE_SHA:-<none>}..${HEAD_SHA:-HEAD}"
if [[ -n "${BASE_SHA}" ]]; then
  git diff ${BASE_SHA}...${HEAD_SHA:-HEAD} --unified=3 > "$PATCH_FILE"
else
  # Fallback for manual runs
  git diff --unified=3 > "$PATCH_FILE"
fi

# Build paths
SERVER=./src/ReviewMcpServer/bin/Release/net8.0/ReviewMcpServer
CLIENT=./src/ReviewClient/bin/Release/net8.0/ReviewClient

# Start server (stdio) in background
$SERVER &
SERVER_PID=$!
trap 'kill ${SERVER_PID} 2>/dev/null || true' EXIT

# Invoke client (will spawn a one-shot call as well)
$CLIENT           --patch-file "$PATCH_FILE"           --owner "${GITHUB_REPOSITORY%/*}"           --repo  "${GITHUB_REPOSITORY#*/}"           --pr    "$(jq -r '.pull_request.number // 0' "$GITHUB_EVENT_PATH")" || true
