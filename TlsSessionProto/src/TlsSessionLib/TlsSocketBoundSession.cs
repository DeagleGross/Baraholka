using System.Net.Security.Prototype.Internal;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace System.Net.Security.Prototype;

/// <summary>
/// A TLS session that owns its socket transport. The TLS layer performs reads/writes
/// directly on the socket fd; the caller drives the state machine via Handshake / Read / Write
/// and observes <see cref="TlsOperationStatus.WantRead"/> / <see cref="TlsOperationStatus.WantWrite"/>
/// to know when to wait for socket readiness (typically via epoll).
/// </summary>
/// <remarks>
/// Available only on Linux / OpenSSL. On platforms whose TLS provider does not expose a
/// socket-bound mode (Schannel on Windows, Network.framework on Apple), use
/// <see cref="TlsDetachedSession"/> instead.
/// </remarks>
[SupportedOSPlatform("linux")]
public sealed class TlsSocketBoundSession : TlsSession
{
    private readonly SafeSocketHandle _socket;
    private bool _socketRefAdded;

    private TlsSocketBoundSession(SafeSsl native, SafeSocketHandle socket, bool isServer)
        : base(native, isServer)
    {
        _socket = socket;
    }

    public override TlsTransportMode TransportMode => TlsTransportMode.SocketBound;

    /// <summary>Returns the socket the session reads/writes through.</summary>
    public SafeSocketHandle Socket => _socket;

    internal static TlsSocketBoundSession Create(TlsContext context, SafeSocketHandle socket, bool isServer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(socket);

        IntPtr ssl = OpenSslInterop.SSL_new(context.Native.DangerousHandle);
        if (ssl == IntPtr.Zero)
        {
            throw new InvalidOperationException("SSL_new failed: " + OpenSslInterop.GetLastErrorString());
        }

        var safe = new SafeSsl(ssl);
        var session = new TlsSocketBoundSession(safe, socket, isServer);

        bool addedRef = false;
        try
        {
            socket.DangerousAddRef(ref addedRef);
            int fd = (int)socket.DangerousGetHandle();
            if (OpenSslInterop.SSL_set_fd(ssl, fd) != 1)
            {
                throw new InvalidOperationException(
                    "SSL_set_fd failed: " + OpenSslInterop.GetLastErrorString());
            }
        }
        catch
        {
            if (addedRef) socket.DangerousRelease();
            safe.Dispose();
            throw;
        }

        session._socketRefAdded = addedRef;

        if (isServer) OpenSslInterop.SSL_set_accept_state(ssl);
        else          OpenSslInterop.SSL_set_connect_state(ssl);

        return session;
    }

    public TlsOperationStatus Handshake()
    {
        OpenSslInterop.ERR_clear_error();
        int rc = OpenSslInterop.SSL_do_handshake(Native.DangerousHandle);
        if (rc == 1)
        {
            MarkHandshakeComplete();
            return TlsOperationStatus.Complete;
        }

        int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
        return err switch
        {
            OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
            OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
            OpenSslInterop.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
            OpenSslInterop.SSL_ERROR_SYSCALL => TlsOperationStatus.Closed,
            _ => throw new InvalidOperationException(
                $"SSL_do_handshake failed (err={err}): {OpenSslInterop.GetLastErrorString()}"),
        };
    }

    public unsafe TlsOperationStatus Read(Span<byte> buffer, out int bytesRead)
    {
        if (!IsHandshakeComplete)
        {
            throw new InvalidOperationException("Handshake has not completed.");
        }

        OpenSslInterop.ERR_clear_error();

        int rc;
        fixed (byte* p = buffer)
        {
            rc = OpenSslInterop.SSL_read(Native.DangerousHandle, p, buffer.Length);
        }

        if (rc > 0)
        {
            bytesRead = rc;
            return TlsOperationStatus.Complete;
        }

        bytesRead = 0;
        int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
        return err switch
        {
            OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
            OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
            OpenSslInterop.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Complete,
            OpenSslInterop.SSL_ERROR_SYSCALL => TlsOperationStatus.Closed,
            _ => throw new InvalidOperationException(
                $"SSL_read failed (err={err}): {OpenSslInterop.GetLastErrorString()}"),
        };
    }

    public unsafe TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten)
    {
        if (!IsHandshakeComplete)
        {
            throw new InvalidOperationException("Handshake has not completed.");
        }

        OpenSslInterop.ERR_clear_error();

        int rc;
        fixed (byte* p = buffer)
        {
            rc = OpenSslInterop.SSL_write(Native.DangerousHandle, p, buffer.Length);
        }

        if (rc > 0)
        {
            bytesWritten = rc;
            return TlsOperationStatus.Complete;
        }

        bytesWritten = 0;
        int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
        return err switch
        {
            OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
            OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
            OpenSslInterop.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
            OpenSslInterop.SSL_ERROR_SYSCALL => TlsOperationStatus.Closed,
            _ => throw new InvalidOperationException(
                $"SSL_write failed (err={err}): {OpenSslInterop.GetLastErrorString()}"),
        };
    }

    public override TlsOperationStatus Shutdown()
    {
        OpenSslInterop.ERR_clear_error();
        int rc = OpenSslInterop.SSL_shutdown(Native.DangerousHandle);
        if (rc >= 1) return TlsOperationStatus.Complete;
        if (rc == 0) return TlsOperationStatus.WantRead;
        int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
        return err switch
        {
            OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
            OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
            _ => TlsOperationStatus.Closed,
        };
    }

    private protected override void DisposeCore()
    {
        if (_socketRefAdded)
        {
            try { _socket.DangerousRelease(); }
            catch { }
            _socketRefAdded = false;
        }
    }
}
