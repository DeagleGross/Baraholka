namespace System.Net.Security.Prototype;

/// <summary>
/// Outcome of a non-blocking TLS operation. Provider-opaque (does not leak OpenSSL error codes).
/// </summary>
public enum TlsOperationStatus
{
    /// <summary>Operation completed. For Decrypt, <c>produced == 0</c> means peer sent close_notify.</summary>
    Complete = 0,

    /// <summary>The TLS layer needs more bytes from the peer before it can make progress.</summary>
    WantRead = 1,

    /// <summary>The TLS layer needs to send bytes to the peer before it can make progress.</summary>
    WantWrite = 2,

    /// <summary>The transport is gone (RST, unexpected EOF before close_notify).</summary>
    Closed = 3,
}
