# TlsSession prototype

## What this demonstrates

```
TlsContext                          ← cross-platform configuration
TlsSession (abstract)               ← shared lifecycle + negotiated info + factories
  ├─ TlsDetachedSession             ← caller owns I/O; ProcessHandshake / Decrypt / Encrypt
  └─ TlsSocketBoundSession          ← session owns the socket; Handshake / Read / Write
                                      [SupportedOSPlatform("linux")]
TlsTransportMode { Detached, SocketBound }
TlsOperationStatus { Complete, WantRead, WantWrite, Closed }
```

Both session types share:

* `TlsContext` (one-time configuration; cross-platform)
* `IsHandshakeComplete`, `TargetHostName`, `Shutdown`, `Dispose` (on the base)
* `TlsOperationStatus` (provider-opaque outcome enum — no `SSL_ERROR_*` leakage)

They differ only in **who owns the I/O resource**:

| Mode | I/O owner | Handshake call | Available on |
|---|---|---|---|
| `Detached` | the caller | `ProcessHandshake(input, output, …)` | Linux (OpenSSL), Windows (Schannel-ready API), macOS |
| `SocketBound` | the session | `Handshake()` (TLS layer reads/writes the fd itself) | Linux only |

`TlsSocketBoundSession` cannot meaningfully exist on Windows because Schannel has
no equivalent of `SSL_set_fd` — SSPI is buffer-only by design. Windows callers
use `TlsDetachedSession`.

## Layout

```
src/TlsSessionLib/                  the proposed API + an OpenSSL implementation
samples/EchoServer/                 echoes one TLS message; --mode detached | socket-bound
samples/EchoClient/                 standard SslStream client
certs/make-cert.sh                  generate self-signed cert
```

## Run on WSL

```bash
cd TlsSessionProto

# 1. generate a self-signed cert
bash certs/make-cert.sh

# 2. (terminal A) start the server in either mode
dotnet run --project samples/EchoServer -- --mode detached
# or
dotnet run --project samples/EchoServer -- --mode socket-bound

# 3. (terminal B) talk to it with a stock SslStream client
dotnet run --project samples/EchoClient -- 127.0.0.1 5443 "hello tls-session"
```

You should see both modes complete the handshake against the same `SslStream`
client and echo the message back. That is the proof that one common API shape
(`TlsContext` + `TlsSession`) covers both transport-ownership models.