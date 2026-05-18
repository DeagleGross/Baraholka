using System.Net;
using System.Net.Security.Prototype;
using System.Net.Sockets;
using System.Text;

// Minimal TLS echo server demonstrating BOTH transport modes of TlsSession.
//
// Usage:
//   dotnet run --project samples/EchoServer -- --mode detached     [--port 5443] [--cert certs/server.pem] [--key certs/server.key]
//   dotnet run --project samples/EchoServer -- --mode socket-bound [--port 5443] [--cert certs/server.pem] [--key certs/server.key]
//
// The two modes share TlsContext / TlsSession / TlsOperationStatus and produce identical wire output.
// Only the I/O ownership differs:
//   - Detached    : EchoServer calls socket.Receive/Send and feeds spans through ProcessHandshake / Decrypt / Encrypt.
//   - SocketBound : TlsSession owns the fd; EchoServer just calls Handshake / Read / Write and waits on Poll.

string mode = "detached";
int port = 5443;
string certPath = "certs/server.pem";
string keyPath  = "certs/server.key";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--mode": mode = args[++i]; break;
        case "--port": port = int.Parse(args[++i]); break;
        case "--cert": certPath = args[++i]; break;
        case "--key":  keyPath  = args[++i]; break;
    }
}

if (!File.Exists(certPath) || !File.Exists(keyPath))
{
    Console.Error.WriteLine($"Certificate or key not found ({certPath}, {keyPath}).");
    Console.Error.WriteLine("Run certs/make-cert.sh first (or pass --cert/--key paths).");
    return 1;
}

using var ctx = TlsContext.CreateServer(certPath, keyPath);

using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, port));
listener.Listen(16);

Console.WriteLine($"[server/{mode}] listening on 127.0.0.1:{port}");
Console.WriteLine($"[server/{mode}] runtime OS: {(OperatingSystem.IsLinux() ? "linux" : "non-linux")}");

while (true)
{
#pragma warning disable CS0162 // unreachable: while(true) loop body always continues or returns earlier
    var client = listener.Accept();
    Console.WriteLine($"[server/{mode}] accepted from {client.RemoteEndPoint}");
    try
    {
        switch (mode)
        {
            case "detached":    HandleDetached(client, ctx); break;
            case "socket-bound": HandleSocketBound(client, ctx); break;
            default:
                Console.Error.WriteLine($"unknown mode '{mode}' (use 'detached' or 'socket-bound')");
                return 2;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[server/{mode}] connection error: {ex.Message}");
    }
    finally
    {
        try { client.Shutdown(SocketShutdown.Both); } catch { }
        client.Dispose();
    }
}

static void HandleDetached(Socket socket, TlsContext ctx)
{
    using var session = TlsSession.CreateDetached(ctx, isServer: true);

    Span<byte> recvBuf = stackalloc byte[4096];
    Span<byte> outBuf  = stackalloc byte[4096];

    // ── Handshake loop: caller owns I/O. ──────────────────────────────────
    ReadOnlySpan<byte> input = ReadOnlySpan<byte>.Empty;
    int inputLen = 0;
    byte[] inputArr = new byte[4096];

    while (!session.IsHandshakeComplete)
    {
        var status = session.ProcessHandshake(input, outBuf, out int consumed, out int produced);

        if (consumed > 0)
        {
            // Slide unconsumed input forward (memBIO accepted everything we offered in practice,
            // but be defensive).
            int remaining = inputLen - consumed;
            if (remaining > 0) Array.Copy(inputArr, consumed, inputArr, 0, remaining);
            inputLen = remaining;
            input = inputArr.AsSpan(0, inputLen);
        }

        if (produced > 0)
        {
            socket.Send(outBuf[..produced]);
        }

        if (status == TlsOperationStatus.Complete) break;
        if (status == TlsOperationStatus.Closed)
        {
            Console.WriteLine("[server/detached] peer closed during handshake");
            return;
        }
        if (status == TlsOperationStatus.WantRead)
        {
            int n = socket.Receive(inputArr.AsSpan(inputLen));
            if (n <= 0) { Console.WriteLine("[server/detached] EOF during handshake"); return; }
            inputLen += n;
            input = inputArr.AsSpan(0, inputLen);
        }
        // WantWrite: produced > 0 above already drained; loop to try more.
    }

    Console.WriteLine("[server/detached] handshake complete");

    // ── App data: receive one message, echo it back. ──────────────────────
    int read = socket.Receive(recvBuf);
    if (read <= 0) return;

    // Decrypt loop: feed one chunk; SSL_read may need multiple iterations to surface a full record.
    var ds = session.Decrypt(recvBuf[..read], outBuf, out int dConsumed, out int dProduced);
    if (ds == TlsOperationStatus.Complete && dProduced > 0)
    {
        string msg = Encoding.UTF8.GetString(outBuf[..dProduced]);
        Console.WriteLine($"[server/detached] received: {msg.Trim()}");

        // Encrypt echo response.
        Span<byte> reply = stackalloc byte[256];
        int replyLen = Encoding.UTF8.GetBytes($"echo[detached]: {msg}", reply);
        var es = session.Encrypt(reply[..replyLen], outBuf, out _, out int eProduced);
        if (es == TlsOperationStatus.Complete && eProduced > 0)
        {
            socket.Send(outBuf[..eProduced]);
        }
    }
}

static void HandleSocketBound(Socket socket, TlsContext ctx)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.Error.WriteLine("socket-bound mode requires Linux/OpenSSL.");
        return;
    }

    socket.Blocking = true; // simplest demo path; epoll/non-blocking would be the real Kestrel shape.

    using var session = TlsSession.CreateSocketBound(ctx, socket.SafeHandle, isServer: true);

    // Drive handshake. Because the socket is blocking, OpenSSL's recv/send block until done,
    // so SSL_do_handshake returns 1 in one call (or fails).
    while (!session.IsHandshakeComplete)
    {
        var status = session.Handshake();
        if (status == TlsOperationStatus.Complete) break;
        if (status == TlsOperationStatus.Closed)
        {
            Console.WriteLine("[server/socket-bound] peer closed during handshake");
            return;
        }
        // WantRead/WantWrite: with a blocking socket this shouldn't recur in practice;
        // with a non-blocking socket the real Kestrel-style loop would Poll() here.
    }

    Console.WriteLine("[server/socket-bound] handshake complete");

    Span<byte> buffer = stackalloc byte[4096];
    var rs = session.Read(buffer, out int n);
    if (rs == TlsOperationStatus.Complete && n > 0)
    {
        string msg = Encoding.UTF8.GetString(buffer[..n]);
        Console.WriteLine($"[server/socket-bound] received: {msg.Trim()}");

        Span<byte> reply = stackalloc byte[256];
        int replyLen = Encoding.UTF8.GetBytes($"echo[socket-bound]: {msg}", reply);
        session.Write(reply[..replyLen], out _);
    }
}

return 0;
#pragma warning restore CS0162
