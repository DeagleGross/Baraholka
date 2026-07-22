#!/usr/bin/env bash
# Send the same 4 CSRF probe requests probe.ps1 does, but from bash/curl.
# Usage:
#   ./probe.sh                                 # defaults to http://localhost:5265
#   ./probe.sh http://127.0.0.1:5266
set -u

BASE_URL="${1:-http://localhost:5265}"
POST_URL="${BASE_URL}/posthiddenfield"
BODY="field=hello"

# ANSI colors (skipped when stdout is not a TTY)
if [[ -t 1 ]]; then
    C_TITLE=$'\e[36m'; C_EXP=$'\e[90m'; C_OK=$'\e[32m'; C_BAD=$'\e[33m'; C_ERR=$'\e[31m'; C_OFF=$'\e[0m'
else
    C_TITLE=""; C_EXP=""; C_OK=""; C_BAD=""; C_ERR=""; C_OFF=""
fi

echo "Probing ${BASE_URL}"
echo

# ---- sanity: GET /html ----
html_status="$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/html" || echo "ERR")"
if [[ "$html_status" == "200" ]]; then
    printf "GET /html  =>  ${C_OK}200${C_OFF}\n\n"
else
    printf "${C_ERR}GET /html failed (status=%s). Is the server running at %s ?${C_OFF}\n" "$html_status" "$BASE_URL"
    exit 1
fi

# invoke_case <label> <expected> <curl-header-args...>
invoke_case() {
    local label="$1"; shift
    local expected="$1"; shift
    # remaining args are pairs of "-H" "Header: value"

    printf "${C_TITLE}=== %s ===${C_OFF}\n" "$label"
    printf "    ${C_EXP}Expected: %s${C_OFF}\n" "$expected"

    # -s silent, -o discard body, -w print status + location, -X POST, -d body
    # Do NOT follow redirects (-L omitted) — we want to see the 302 itself.
    local out
    out="$(curl -s -o /dev/null \
                -w '%{http_code}|%{redirect_url}' \
                -X POST \
                -H 'Content-Type: application/x-www-form-urlencoded' \
                "$@" \
                --data "$BODY" \
                "$POST_URL" 2>/dev/null || echo "ERR|")"

    local code="${out%%|*}"
    local loc="${out#*|}"

    local color="$C_OK"
    case "$code" in
        302) [[ "$expected" == *"302"* ]] || color="$C_BAD" ;;
        400) [[ "$expected" == *"400"* ]] || color="$C_BAD" ;;
        ERR) color="$C_ERR" ;;
        *)   color="$C_BAD" ;;
    esac

    if [[ -n "$loc" && "$loc" != "|" ]]; then
        printf "    Actual:   ${color}%s${C_OFF}  Location: %s\n\n" "$code" "$loc"
    else
        printf "    Actual:   ${color}%s${C_OFF}\n\n" "$code"
    fi
}

invoke_case "1) Same-origin browser POST (Sec-Fetch-Site: same-origin)" \
            "302 -> /hello  (browser same-origin form POST is safe)" \
            -H "Origin: ${BASE_URL}" \
            -H "Sec-Fetch-Site: same-origin" \
            -H "Sec-Fetch-Mode: navigate" \
            -H "Sec-Fetch-Dest: document"

invoke_case "2) Cross-site browser POST (Sec-Fetch-Site: cross-site)" \
            "400 Bad Request  (CSRF middleware rejects; deferred throw on model-bind)" \
            -H "Origin: https://evil.example" \
            -H "Sec-Fetch-Site: cross-site" \
            -H "Sec-Fetch-Mode: cors" \
            -H "Sec-Fetch-Dest: empty"

invoke_case "3) Non-browser client (no Sec-Fetch-* headers)" \
            "302 -> /hello  (legacy/curl clients are allowed)"

invoke_case "4) Same-site (subdomain-shape) POST (Sec-Fetch-Site: same-site)" \
            "400 Bad Request  (only 'same-origin'/'none' are trusted; 'same-site' is denied)" \
            -H "Origin: ${BASE_URL}" \
            -H "Sec-Fetch-Site: same-site" \
            -H "Sec-Fetch-Mode: cors" \
            -H "Sec-Fetch-Dest: empty"
