using System.Runtime.InteropServices;

namespace System.Net.Security.Prototype.Internal;

/// <summary>
/// Minimal OpenSSL 3.x P/Invoke surface for the prototype.
/// Real product code would live behind System.Net.Security's existing libssl interop.
/// </summary>
internal static unsafe class OpenSslInterop
{
    // Linux: libssl.so.3 / libcrypto.so.3 (OpenSSL 3.x).
    private const string LibSsl = "libssl.so.3";
    private const string LibCrypto = "libcrypto.so.3";

    // SSL_get_error codes
    public const int SSL_ERROR_NONE = 0;
    public const int SSL_ERROR_SSL = 1;
    public const int SSL_ERROR_WANT_READ = 2;
    public const int SSL_ERROR_WANT_WRITE = 3;
    public const int SSL_ERROR_SYSCALL = 5;
    public const int SSL_ERROR_ZERO_RETURN = 6;

    // SSL_CTX_set_options flags we care about
    public const long SSL_OP_NO_SSLv2 = 0x01000000L;
    public const long SSL_OP_NO_SSLv3 = 0x02000000L;
    public const long SSL_OP_NO_TLSv1 = 0x04000000L;
    public const long SSL_OP_NO_TLSv1_1 = 0x10000000L;

    // SSL_filetype
    public const int SSL_FILETYPE_PEM = 1;

    // ── Library init ──────────────────────────────────────────────────────
    [DllImport(LibSsl, EntryPoint = "OPENSSL_init_ssl")]
    public static extern int OPENSSL_init_ssl(ulong opts, IntPtr settings);

    // ── Method / context ──────────────────────────────────────────────────
    [DllImport(LibSsl, EntryPoint = "TLS_method")]
    public static extern IntPtr TLS_method();

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_new")]
    public static extern IntPtr SSL_CTX_new(IntPtr method);

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_free")]
    public static extern void SSL_CTX_free(IntPtr ctx);

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_set_options")]
    public static extern long SSL_CTX_set_options(IntPtr ctx, long options);

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_use_certificate_file")]
    public static extern int SSL_CTX_use_certificate_file(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string file, int type);

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_use_PrivateKey_file")]
    public static extern int SSL_CTX_use_PrivateKey_file(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string file, int type);

    [DllImport(LibSsl, EntryPoint = "SSL_CTX_check_private_key")]
    public static extern int SSL_CTX_check_private_key(IntPtr ctx);

    // ── SSL session ───────────────────────────────────────────────────────
    [DllImport(LibSsl, EntryPoint = "SSL_new")]
    public static extern IntPtr SSL_new(IntPtr ctx);

    [DllImport(LibSsl, EntryPoint = "SSL_free")]
    public static extern void SSL_free(IntPtr ssl);

    [DllImport(LibSsl, EntryPoint = "SSL_set_accept_state")]
    public static extern void SSL_set_accept_state(IntPtr ssl);

    [DllImport(LibSsl, EntryPoint = "SSL_set_connect_state")]
    public static extern void SSL_set_connect_state(IntPtr ssl);

    [DllImport(LibSsl, EntryPoint = "SSL_do_handshake")]
    public static extern int SSL_do_handshake(IntPtr ssl);

    [DllImport(LibSsl, EntryPoint = "SSL_get_error")]
    public static extern int SSL_get_error(IntPtr ssl, int ret);

    [DllImport(LibSsl, EntryPoint = "SSL_read")]
    public static extern int SSL_read(IntPtr ssl, byte* buf, int num);

    [DllImport(LibSsl, EntryPoint = "SSL_write")]
    public static extern int SSL_write(IntPtr ssl, byte* buf, int num);

    [DllImport(LibSsl, EntryPoint = "SSL_shutdown")]
    public static extern int SSL_shutdown(IntPtr ssl);

    [DllImport(LibSsl, EntryPoint = "SSL_set_quiet_shutdown")]
    public static extern void SSL_set_quiet_shutdown(IntPtr ssl, int mode);

    [DllImport(LibSsl, EntryPoint = "SSL_is_init_finished")]
    public static extern int SSL_is_init_finished(IntPtr ssl);

    // ── BIO (memory transport for Detached mode) ──────────────────────────
    [DllImport(LibCrypto, EntryPoint = "BIO_new")]
    public static extern IntPtr BIO_new(IntPtr method);

    [DllImport(LibCrypto, EntryPoint = "BIO_s_mem")]
    public static extern IntPtr BIO_s_mem();

    [DllImport(LibCrypto, EntryPoint = "BIO_free")]
    public static extern int BIO_free(IntPtr bio);

    [DllImport(LibCrypto, EntryPoint = "BIO_write")]
    public static extern int BIO_write(IntPtr bio, byte* data, int len);

    [DllImport(LibCrypto, EntryPoint = "BIO_read")]
    public static extern int BIO_read(IntPtr bio, byte* data, int len);

    // BIO_pending = BIO_ctrl(b, BIO_CTRL_PENDING=10, 0, NULL)
    [DllImport(LibCrypto, EntryPoint = "BIO_ctrl")]
    public static extern long BIO_ctrl(IntPtr bio, int cmd, long larg, IntPtr parg);

    public const int BIO_CTRL_PENDING = 10;

    public static long BIO_pending(IntPtr bio) => BIO_ctrl(bio, BIO_CTRL_PENDING, 0, IntPtr.Zero);

    [DllImport(LibSsl, EntryPoint = "SSL_set_bio")]
    public static extern void SSL_set_bio(IntPtr ssl, IntPtr rbio, IntPtr wbio);

    // ── Direct fd binding (SocketBound mode, Linux) ───────────────────────
    [DllImport(LibSsl, EntryPoint = "SSL_set_fd")]
    public static extern int SSL_set_fd(IntPtr ssl, int fd);

    // ── Errors ────────────────────────────────────────────────────────────
    [DllImport(LibCrypto, EntryPoint = "ERR_clear_error")]
    public static extern void ERR_clear_error();

    [DllImport(LibCrypto, EntryPoint = "ERR_get_error")]
    public static extern ulong ERR_get_error();

    [DllImport(LibCrypto, EntryPoint = "ERR_error_string_n")]
    public static extern void ERR_error_string_n(ulong e, byte* buf, IntPtr len);

    public static string GetLastErrorString()
    {
        ulong code = ERR_get_error();
        if (code == 0) return "no openssl error";
        Span<byte> buf = stackalloc byte[256];
        fixed (byte* p = buf)
        {
            ERR_error_string_n(code, p, (IntPtr)buf.Length);
        }
        int n = buf.IndexOf((byte)0);
        if (n < 0) n = buf.Length;
        return System.Text.Encoding.UTF8.GetString(buf[..n]);
    }
}
