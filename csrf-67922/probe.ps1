# Sends all three client scenarios against the running Repro app for dotnet/aspnetcore#67922.
#
# Usage:
#   .\probe.ps1                          # defaults to http://localhost:5265
#   .\probe.ps1 -BaseUrl http://localhost:5266
#
# Server-start (in another terminal): dotnet run --project .\Repro

[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5265"
)

$ErrorActionPreference = 'Stop'

function Invoke-Case {
    param(
        [string]$Label,
        [string]$Expected,
        [hashtable]$Headers,
        [switch]$OmitBody
    )

    Write-Host ""
    Write-Host "=== $Label ===" -ForegroundColor Cyan
    Write-Host "    Expected: $Expected" -ForegroundColor DarkGray

    $body = if ($OmitBody) { $null } else { @{ field = "value" } }

    try {
        $resp = Invoke-WebRequest -Uri "$BaseUrl/posthiddenfield" -Method Post `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded" `
            -Headers $Headers `
            -MaximumRedirection 0 -UseBasicParsing -ErrorAction Stop
        $loc = $resp.Headers.Location
        Write-Host ("    Actual:   {0} {1}{2}" -f `
            [int]$resp.StatusCode, $resp.StatusCode, $(if ($loc) { "  Location: $loc" } else { "" })) `
            -ForegroundColor Green
    } catch {
        $r = $_.Exception.Response
        if ($r) {
            $loc = $r.Headers.Location
            Write-Host ("    Actual:   {0} {1}{2}" -f `
                [int]$r.StatusCode, $r.StatusCode, $(if ($loc) { "  Location: $loc" } else { "" })) `
                -ForegroundColor Yellow
        } else {
            Write-Host ("    Actual:   ERROR {0}" -f $_.Exception.Message) -ForegroundColor Red
        }
    }
}

Write-Host "Probing $BaseUrl" -ForegroundColor White

# Sanity: is the server up?
try {
    $ping = Invoke-WebRequest -Uri "$BaseUrl/html" -Method Get -UseBasicParsing -ErrorAction Stop
    Write-Host ("GET /html  =>  {0}" -f [int]$ping.StatusCode) -ForegroundColor DarkGray
} catch {
    Write-Host "Server not reachable at $BaseUrl. Start it with: dotnet run --project .\Repro" -ForegroundColor Red
    exit 1
}

Invoke-Case -Label "1) Same-origin browser POST (Sec-Fetch-Site: same-origin)" `
    -Expected "302 -> /hello  (browser same-origin form POST is safe)" `
    -Headers @{
        "Origin"         = $BaseUrl
        "Referer"        = "$BaseUrl/html"
        "Sec-Fetch-Site" = "same-origin"
        "Sec-Fetch-Mode" = "navigate"
        "Sec-Fetch-Dest" = "document"
        "Sec-Fetch-User" = "?1"
    }

Invoke-Case -Label "2) Cross-site browser POST (Sec-Fetch-Site: cross-site)" `
    -Expected "400 Bad Request  (CSRF middleware rejects; deferred throw on model-bind)" `
    -Headers @{
        "Origin"         = "http://evil.example"
        "Sec-Fetch-Site" = "cross-site"
        "Sec-Fetch-Mode" = "cors"
        "Sec-Fetch-Dest" = "empty"
    }

Invoke-Case -Label "3) Non-browser client (no Sec-Fetch-* headers)" `
    -Expected "302 -> /hello  (legacy/curl clients are allowed)" `
    -Headers @{}

Invoke-Case -Label "4) Same-site (subdomain-shape) POST (Sec-Fetch-Site: same-site)" `
    -Expected "400 Bad Request  (only 'same-origin'/'none' are trusted; 'same-site' is denied)" `
    -Headers @{
        "Origin"         = $BaseUrl
        "Sec-Fetch-Site" = "same-site"
        "Sec-Fetch-Mode" = "cors"
        "Sec-Fetch-Dest" = "empty"
    }

Write-Host ""
Write-Host "Done." -ForegroundColor White
