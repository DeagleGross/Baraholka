using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Probe whether HTTP.SYS (via HttpListener) accepts bare LF (\n) as a line
// terminator in HTTP/1.1 requests. Sends 4 raw TCP requests and prints the
// raw response status line so we can see whether HTTP.SYS rejects with 400.

const int Port = 18080;
var prefix = $"http://localhost:{Port}/";

using var listener = new HttpListener();
listener.Prefixes.Add(prefix);
listener.Start();
Console.WriteLine($"HttpListener (HTTP.SYS) started on {prefix}");

var cts = new CancellationTokenSource();
var serverTask = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        HttpListenerContext ctx;
        try
        {
            ctx = await listener.GetContextAsync();
        }
        catch
        {
            break;
        }

        try
        {
            using var reader = new StreamReader(ctx.Request.InputStream);
            var body = await reader.ReadToEndAsync();

            var trailers = ctx.Request.Headers["X-Trailer"] ?? "(none)";
            var msg = $"server-saw: method={ctx.Request.HttpMethod} body-len={body.Length} x-trailer-hdr={trailers}";
            Console.WriteLine("  " + msg);

            ctx.Response.StatusCode = 200;
            var buf = Encoding.ASCII.GetBytes(msg);
            await ctx.Response.OutputStream.WriteAsync(buf);
            ctx.Response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("  server-exception: " + ex.GetType().Name + ": " + ex.Message);
            try { ctx.Response.Close(); } catch { }
        }
    }
});

await RunCase("1. Control: well-formed CRLF GET",
    "GET / HTTP/1.1\r\n" +
    "Host: localhost\r\n" +
    "Connection: close\r\n" +
    "\r\n");

await RunCase("2. Bare LF terminators on request line + headers",
    "GET / HTTP/1.1\n" +
    "Host: localhost\n" +
    "Connection: close\n" +
    "\n");

await RunCase("3. CRLF everywhere including chunked trailer (control for chunked)",
    "POST / HTTP/1.1\r\n" +
    "Host: localhost\r\n" +
    "Transfer-Encoding: chunked\r\n" +
    "Trailer: X-Trailer\r\n" +
    "Connection: close\r\n" +
    "\r\n" +
    "5\r\nhello\r\n" +
    "0\r\n" +
    "X-Trailer: ok\r\n" +
    "\r\n");

await RunCase("4. Chunked with bare LF in trailer section (the Kestrel scenario)",
    "POST / HTTP/1.1\r\n" +
    "Host: localhost\r\n" +
    "Transfer-Encoding: chunked\r\n" +
    "Trailer: X-Trailer\r\n" +
    "Connection: close\r\n" +
    "\r\n" +
    "5\r\nhello\r\n" +
    "0\n" +
    "X-Trailer: ok\n" +
    "\n");

cts.Cancel();
listener.Stop();

static async Task RunCase(string label, string rawRequest)
{
    Console.WriteLine();
    Console.WriteLine("=== " + label + " ===");
    Console.WriteLine("REQUEST (escaped):");
    Console.WriteLine("  " + Escape(rawRequest));

    try
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Port);
        using var stream = client.GetStream();
        var bytes = Encoding.ASCII.GetBytes(rawRequest);
        await stream.WriteAsync(bytes);

        using var ms = new MemoryStream();
        var buf = new byte[4096];
        client.ReceiveTimeout = 2000;
        try
        {
            int n;
            while ((n = await stream.ReadAsync(buf)) > 0)
            {
                ms.Write(buf, 0, n);
                if (ms.Length > 8192) break;
            }
        }
        catch (IOException) { }

        var resp = Encoding.ASCII.GetString(ms.ToArray());
        var firstLine = resp.Split('\n')[0].TrimEnd('\r');
        Console.WriteLine("RESPONSE status line: " + (string.IsNullOrEmpty(firstLine) ? "(no response / connection closed)" : firstLine));
    }
    catch (Exception ex)
    {
        Console.WriteLine("client-exception: " + ex.GetType().Name + ": " + ex.Message);
    }

    await Task.Delay(150);
}

static string Escape(string s) =>
    s.Replace("\r", "\\r").Replace("\n", "\\n\n        ");
