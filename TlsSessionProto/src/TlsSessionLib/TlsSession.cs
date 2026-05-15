using System.Net.Security.Prototype.Internal;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace System.Net.Security.Prototype;

/// <summary>
/// A TLS session. Concrete implementations differ in how they own the transport:
/// <see cref="TlsDetachedSession"/> (caller owns I/O, cross-platform) or
/// <see cref="TlsSocketBoundSession"/> (session owns the socket; Linux/OpenSSL only).
/// </summary>
/// <remarks>
/// Carries the shared lifecycle, handshake-state, and negotiated-info surface.
/// Subclasses contribute the I/O methods that match their transport mode.
/// </remarks>
public abstract class TlsSession : IDisposable
{
    internal SafeSsl Native { get; }
    internal bool IsServer { get; }

    private bool _handshakeComplete;
    private bool _disposed;

    private protected TlsSession(SafeSsl native, bool isServer)
    {
        Native = native;
        IsServer = isServer;
    }

    /// <summary>The transport-ownership shape of this session.</summary>
    public abstract TlsTransportMode TransportMode { get; }

    /// <summary>True once the TLS handshake has completed.</summary>
    public bool IsHandshakeComplete => _handshakeComplete || RefreshHandshakeFlag();

    /// <summary>SNI / target host. Set on a client session before the first handshake call.</summary>
    public string? TargetHostName { get; set; }

    /// <summary>Drives an orderly TLS shutdown (close_notify).</summary>
    public abstract TlsOperationStatus Shutdown();

    /// <summary>
    /// Detached factory. Caller drives I/O and uses
    /// <see cref="TlsDetachedSession.ProcessHandshake"/> / Encrypt / Decrypt.
    /// </summary>
    public static TlsDetachedSession CreateDetached(TlsContext context, bool isServer)
        => TlsDetachedSession.Create(context, isServer);

    /// <summary>
    /// Socket-bound factory (Linux/OpenSSL only). Session owns the socket and exposes
    /// <see cref="TlsSocketBoundSession.Handshake"/> / Read / Write.
    /// </summary>
    [SupportedOSPlatform("linux")]
    public static TlsSocketBoundSession CreateSocketBound(
        TlsContext context, SafeSocketHandle socket, bool isServer)
        => TlsSocketBoundSession.Create(context, socket, isServer);

    private protected void MarkHandshakeComplete() => _handshakeComplete = true;

    private bool RefreshHandshakeFlag()
    {
        if (_handshakeComplete) return true;
        if (Native.IsInvalid || Native.IsClosed) return false;
        if (OpenSslInterop.SSL_is_init_finished(Native.DangerousHandle) == 1)
        {
            _handshakeComplete = true;
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeCore();
        Native.Dispose();
        GC.SuppressFinalize(this);
    }

    private protected virtual void DisposeCore() { }
}
