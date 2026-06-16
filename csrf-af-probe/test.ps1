<#
    test.ps1 - Exercises antiforgery behavior of the probe app (net10.0, existing AF, no CSRF middleware).

    Usage:
        1. In one terminal:   dotnet run    (from this folder)  -> listens on http://localhost:5099
        2. In another:        ./test.ps1

    Requires PowerShell 7+ (uses -SkipHttpErrorCheck). Run `pwsh` if needed.

    Each scenario prints: scenario name, HTTP status, and body.
#>

$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5099'

function Show($name, $resp) {
    $code = [int]$resp.StatusCode
    $body = $resp.Content
    $color = if ($code -ge 200 -and $code -lt 300) { 'Green' } else { 'Red' }
    Write-Host ("{0,-48} -> {1}" -f $name, $code) -ForegroundColor $color
    if ($body) { Write-Host ("    body: {0}" -f $body) -ForegroundColor DarkGray }
}

Write-Host "`n=== Antiforgery probe (UseAntiforgery() is enabled) ===`n" -ForegroundColor Cyan

# --- Step 1: fetch a valid cookie + request token into a session ---
$tokenResp = Invoke-WebRequest -Uri "$base/token" -SessionVariable session -SkipHttpErrorCheck
$tok = $tokenResp.Content | ConvertFrom-Json
$field = $tok.formFieldName            # default: __RequestVerificationToken
$reqToken = $tok.requestToken
Write-Host "Fetched token. formFieldName='$field'`n" -ForegroundColor Cyan

# ============================================================================
# FORM endpoint (/form) - antiforgery REQUIRED
# ============================================================================

# 1) Form POST WITH valid cookie + valid request token  => expect 200
$body = @{ name = 'hello'; $field = $reqToken }
$r = Invoke-WebRequest -Uri "$base/form" -Method Post -Body $body -WebSession $session -SkipHttpErrorCheck
Show '1. /form  WITH cookie + valid token' $r

# 2) Form POST WITH cookie but NO request token         => expect 400
$body = @{ name = 'hello' }
$r = Invoke-WebRequest -Uri "$base/form" -Method Post -Body $body -WebSession $session -SkipHttpErrorCheck
Show '2. /form  WITH cookie, NO token' $r

# 3) Form POST WITH valid token but NO cookie (fresh session) => expect 400
$body = @{ name = 'hello'; $field = $reqToken }
$r = Invoke-WebRequest -Uri "$base/form" -Method Post -Body $body -SessionVariable fresh -SkipHttpErrorCheck
Show '3. /form  NO cookie, token only' $r

# 4) Form POST with NOTHING (no cookie, no token)       => expect 400
$body = @{ name = 'hello' }
$r = Invoke-WebRequest -Uri "$base/form" -Method Post -Body $body -SessionVariable fresh2 -SkipHttpErrorCheck
Show '4. /form  bare (no cookie, no token)' $r

# ============================================================================
# JSON endpoint (/json) - antiforgery NOT inferred (non-form)
# ============================================================================

# 5) JSON POST with no token at all                     => expect 200 (not protected)
$r = Invoke-WebRequest -Uri "$base/json" -Method Post -Body '{"name":"hello"}' -ContentType 'application/json' -SkipHttpErrorCheck
Show '5. /json  no token (non-form)' $r

# ============================================================================
# FORM endpoint that opted out (/form-noaf) - DisableAntiforgery()
# ============================================================================

# 6) Form POST, no token, endpoint opted out            => expect 200
$body = @{ name = 'hello' }
$r = Invoke-WebRequest -Uri "$base/form-noaf" -Method Post -Body $body -SkipHttpErrorCheck
Show '6. /form-noaf  no token (opted out)' $r

Write-Host "`n=== done ===`n" -ForegroundColor Cyan
