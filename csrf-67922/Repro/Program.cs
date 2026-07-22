using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/hello", () =>
{
    return TypedResults.Ok("hello");
});

app.MapPost("/posthiddenfield", ([FromForm] string field) =>
{
    return TypedResults.LocalRedirect("/hello");
});

app.MapGet("/html", async (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync("""
        <!DOCTYPE html>
        <html>
        <body>
        <form action="/posthiddenfield" method="post">
            <input type="hidden" name="field" value="value">
            <button type="submit">Send POST</button>
        </form>
        </body>
        </html>
        """);
});

app.Run();
