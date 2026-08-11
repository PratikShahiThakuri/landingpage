#!/usr/bin/env bash
# run.sh: Build and deploy the landingmvc application using Docker.
set -euo pipefail
trap 'echo "❌ Failed at line $LINENO (exit $?)"; exit 1' ERR

TARGET_ENV="${TARGET_ENV:-dev}"

# ==== GIT SYNC ====
RAW_BRANCH="${GIT_BRANCH:-${BRANCH_NAME:-main}}"
case "$RAW_BRANCH" in
  refs/heads/*) BRANCH="${RAW_BRANCH#refs/heads/}" ;;
  origin/*)     BRANCH="${RAW_BRANCH#origin/}"     ;;
  */*)          BRANCH="${RAW_BRANCH##*/}"         ;;
  *)            BRANCH="$RAW_BRANCH"               ;;
esac
COMMIT="$(git rev-parse --short=7 HEAD || echo manualrun)"
echo "Using image tag: ${COMMIT} (branch=${BRANCH}, env=${TARGET_ENV})"

# ==== PORTS ====
if [ "$TARGET_ENV" = "prod" ]; then
  PORT_WEB=5000
else
  PORT_WEB=5200
fi

# ==== VARS ====
IMAGE_NAME="landingmvc"
CONTAINER_NAME="${IMAGE_NAME}-${TARGET_ENV}"
WORKSPACE="${WORKSPACE:-$PWD}"
NETWORK="landingmvc-net"

echo "== MATRIX AXIS =="
echo "SERVICE     : ${IMAGE_NAME}"
echo "TARGET_ENV  : ${TARGET_ENV}"
echo "BRANCH      : ${BRANCH}"
echo "COMMIT      : ${COMMIT}"
echo "WORKSPACE   : ${WORKSPACE}"
echo "HTTP PORT   : ${PORT_WEB}"

# ===== Docker prerequisites =====
docker network inspect "${NETWORK}" >/dev/null 2>&1 || docker network create "${NETWORK}" >/dev/null

# ===== Build Image =====
echo "== Building Docker Image =="
docker build -t ${IMAGE_NAME}:latest -t ${IMAGE_NAME}:${COMMIT} "$WORKSPACE"

# ===== Stop old container =====
echo "== Stopping existing container =="
docker rm -f "$CONTAINER_NAME" >/dev/null 2>&1 || true

# ===== Start new container =====
echo "== Starting ${CONTAINER_NAME} on port ${PORT_WEB} =="
docker run -d --restart unless-stopped --name "$CONTAINER_NAME" --network "$NETWORK" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:80" \
  -v "$WORKSPACE/appsettings.json:/app/appsettings.json:ro" \
  -p ${PORT_WEB}:80 \
  ${IMAGE_NAME}:${COMMIT}

echo "✅ Deployment successful. Container $CONTAINER_NAME is running on port ${PORT_WEB}."
