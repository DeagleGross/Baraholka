namespace System.Net.Security.Prototype;

/// <summary>
/// Identifies the I/O ownership shape of a <see cref="TlsSession"/>.
/// </summary>
public enum TlsTransportMode
{
    /// <summary>
    /// The session is not attached to any transport. The caller drives I/O explicitly,
    /// passing ciphertext/plaintext spans through ProcessHandshake / Encrypt / Decrypt.
    /// Cross-platform: works wherever the underlying TLS provider supports memory buffers
    /// (OpenSSL on Linux, Schannel on Windows, Network.framework on Apple).
    /// </summary>
    Detached = 0,

    /// <summary>
    /// The session owns the socket transport and performs reads/writes itself.
    /// Available only where the TLS provider exposes a socket-bound mode (Linux/OpenSSL).
    /// </summary>
    SocketBound = 1,
}
