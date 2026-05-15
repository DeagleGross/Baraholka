#!/usr/bin/env bash
# Generates a self-signed test certificate for the TlsSession prototype.
# Run from the TlsSessionProto root: bash certs/make-cert.sh
set -euo pipefail

cd "$(dirname "$0")"

if [[ -f server.pem && -f server.key ]]; then
    echo "certs already present (server.pem, server.key) — delete them to regenerate."
    exit 0
fi

openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
    -keyout server.key \
    -out server.pem \
    -subj "/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"

echo "wrote $(pwd)/server.pem and $(pwd)/server.key"
