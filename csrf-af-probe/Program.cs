using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();

var app = builder.Build();

// The whole point of the probe: antiforgery middleware is in the pipeline.
app.UseAntiforgery();

/*
=== Antiforgery probe (UseAntiforgery() is enabled) ===

Fetched token. formFieldName='__RequestVerificationToken'

1. /form  WITH cookie + valid token              -> 200
    body: "form ok: name=hello"
2. /form  WITH cookie, NO token                  -> 400
3. /form  NO cookie, token only                  -> 400
4. /form  bare (no cookie, no token)             -> 400
5. /json  no token (non-form)                    -> 200
    body: "json ok: name=hello"
6. /form-noaf  no token (opted out)              -> 200
    body: "form-noaf ok: name=hello"

=== done ===
*/

// GET /token
// Sets the antiforgery COOKIE (Set-Cookie) and returns the REQUEST token in the body.
// A legitimate browser would call this (e.g. when rendering a form) and echo the
// request token back on the POST.
app.MapGet("/token", (HttpContext ctx, IAntiforgery af) =>
{
    var tokens = af.GetAndStoreTokens(ctx);
    return Results.Ok(new
    {
        requestToken = tokens.RequestToken,
        headerName = tokens.HeaderName,
        formFieldName = tokens.FormFieldName,
    });
});

// POST /form  -> binds the form. RDF auto-adds AntiforgeryMetadata.ValidationRequired,
// so UseAntiforgery() validates the token. Missing/invalid token => 400 before the
// handler runs.
app.MapPost("/form", (IFormCollection form) =>
    Results.Ok($"form ok: name={form["name"]}"));

// POST /json  -> NO form binding => no antiforgery metadata inferred => never validated,
// even with a bad/missing token. Demonstrates non-form endpoints are not CSRF-checked
// by the token system today.
app.MapPost("/json", (Payload p) =>
    Results.Ok($"json ok: name={p.Name}"));

// POST /form-noaf -> form binding but explicitly opted OUT via DisableAntiforgery().
// Always runs the handler regardless of token.
app.MapPost("/form-noaf", (IFormCollection form) =>
    Results.Ok($"form-noaf ok: name={form["name"]}"))
   .DisableAntiforgery();

app.Run("http://localhost:5099");

record Payload(string Name);
