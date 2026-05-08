# FastAPI multi-description repro

Tiny script to verify what FastAPI does when an operation declares **two responses for the same HTTP status code** with different descriptions.

## Setup (Windows PowerShell)

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install "fastapi==0.115.*" "uvicorn==0.32.*"
```

## Run

```powershell
python app.py > openapi.json
```

`app.py` builds a FastAPI app, prints the generated OpenAPI document to stdout. Inspect `openapi.json` for the `/scenario1b` and `/scenario3` paths.

## Findings

1. **Last write wins, silently.** Populating `responses[404]` twice (different descriptions) yields only the second description in the OpenAPI output. The first is silently dropped. No warning, no error.
2. **The collapse happens at the Python language level**, not the framework level. The `responses` parameter is typed `Dict[Union[int, str], Dict]`. A `dict` cannot hold two entries for the same key, period — by the time FastAPI sees it, the duplicate is already gone.
3. **Int vs string key for same status** — FastAPI normalizes both to the string key in the OpenAPI doc; the later iteration write wins.

## Sample output

`/scenario1b` after building `d[404] = "First..."; d[404] = "Second..."`:

```json
"/scenario1b": {
  "get": {
    "responses": {
      "200": { "description": "Successful Response", ... },
      "404": { "description": "Second description for the same status" }
    }
  }
}
```

## Context

Used to verify "last wins" behavior in FastAPI when discussing the same problem in ASP.NET Core MVC's `ApiResponseTypeProvider` ([dotnet/aspnetcore#65650](https://github.com/dotnet/aspnetcore/pull/65650)).
