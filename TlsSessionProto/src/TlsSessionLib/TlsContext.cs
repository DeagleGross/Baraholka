using System.Net.Security.Prototype.Internal;

namespace System.Net.Security.Prototype;

/// <summary>
/// A long-lived TLS configuration context. Create once per listener; share across many sessions.
/// </summary>
/// <remarks>
/// Thread-safe to share. Internally wraps the underlying TLS provider's context object
/// (e.g. OpenSSL <c>SSL_CTX*</c>, Schannel <c>CredHandle</c>).
/// </remarks>
public sealed class TlsContext : IDisposable
{
    internal SafeSslContext Native { get; }

    private TlsContext(SafeSslContext native) => Native = native;

    /// <summary>
    /// Creates a server-side context from PEM-encoded certificate and private key files.
    /// (Real surface would accept <see cref="SslServerAuthenticationOptions"/>; this prototype
    /// keeps it tight to focus on the session shape.)
    /// </summary>
    public static TlsContext CreateServer(string certificatePemPath, string privateKeyPemPath)
    {
        ArgumentNullException.ThrowIfNull(certificatePemPath);
        ArgumentNullException.ThrowIfNull(privateKeyPemPath);

        OpenSslInterop.OPENSSL_init_ssl(0, IntPtr.Zero);

        IntPtr method = OpenSslInterop.TLS_method();
        IntPtr ctx = OpenSslInterop.SSL_CTX_new(method);
        if (ctx == IntPtr.Zero)
        {
            throw new InvalidOperationException("SSL_CTX_new failed: " + OpenSslInterop.GetLastErrorString());
        }

        var safe = new SafeSslContext(ctx);

        OpenSslInterop.SSL_CTX_set_options(ctx,
            OpenSslInterop.SSL_OP_NO_SSLv2 |
            OpenSslInterop.SSL_OP_NO_SSLv3 |
            OpenSslInterop.SSL_OP_NO_TLSv1 |
            OpenSslInterop.SSL_OP_NO_TLSv1_1);

        if (OpenSslInterop.SSL_CTX_use_certificate_file(ctx, certificatePemPath, OpenSslInterop.SSL_FILETYPE_PEM) != 1)
        {
            safe.Dispose();
            throw new InvalidOperationException("SSL_CTX_use_certificate_file failed: " + OpenSslInterop.GetLastErrorString());
        }

        if (OpenSslInterop.SSL_CTX_use_PrivateKey_file(ctx, privateKeyPemPath, OpenSslInterop.SSL_FILETYPE_PEM) != 1)
        {
            safe.Dispose();
            throw new InvalidOperationException("SSL_CTX_use_PrivateKey_file failed: " + OpenSslInterop.GetLastErrorString());
        }

        if (OpenSslInterop.SSL_CTX_check_private_key(ctx) != 1)
        {
            safe.Dispose();
            throw new InvalidOperationException("Private key does not match certificate.");
        }

        return new TlsContext(safe);
    }

    public void Dispose() => Native.Dispose();
}
