using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

// Standard SslStream client. Connects to the prototype server (either mode) and exchanges one message.
// Uses TLS 1.2/1.3 with certificate validation disabled (self-signed test cert).
//
// Usage: dotnet run --project samples/EchoClient -- [host] [port] [message]

string host = args.Length > 0 ? args[0] : "127.0.0.1";
int    port = args.Length > 1 ? int.Parse(args[1]) : 5443;
string msg  = args.Length > 2 ? args[2] : $"hello from sslstream client at {DateTime.Now:HH:mm:ss}";

using var tcp = new TcpClient();
tcp.Connect(host, port);

using var ssl = new SslStream(
    tcp.GetStream(),
    leaveInnerStreamOpen: false,
    userCertificateValidationCallback: (_, _, _, _) => true);

ssl.AuthenticateAsClient(new SslClientAuthenticationOptions
{
    TargetHost = host,
    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
});

Console.WriteLine($"[client] handshake complete: {ssl.SslProtocol}, {ssl.NegotiatedCipherSuite}");

byte[] payload = Encoding.UTF8.GetBytes(msg);
ssl.Write(payload);
ssl.Flush();

byte[] buf = new byte[4096];
int n = ssl.Read(buf, 0, buf.Length);
Console.WriteLine($"[client] received: {Encoding.UTF8.GetString(buf, 0, n)}");

ssl.ShutdownAsync().GetAwaiter().GetResult();
