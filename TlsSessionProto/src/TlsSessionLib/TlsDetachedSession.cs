using System.Net.Security.Prototype.Internal;

namespace System.Net.Security.Prototype;

/// <summary>
/// A TLS session whose I/O is driven by the caller via byte spans.
/// The caller is responsible for moving ciphertext to/from the peer (over a socket,
/// pipe, file, in-memory queue — anything).
/// </summary>
/// <remarks>
/// <para>
/// Cross-platform shape. On OpenSSL this is implemented with memory BIOs;
/// on Schannel it would map to <c>EncryptMessage</c>/<c>DecryptMessage</c>;
/// on Apple platforms to the equivalent buffer-callback interfaces.
/// </para>
/// <para>
/// Typical handshake loop:
/// <code>
/// while (!session.IsHandshakeComplete)
/// {
///     var status = session.ProcessHandshake(input, output, out int consumed, out int produced);
///     if (produced > 0) socket.Send(output[..produced]);
///     if (status == TlsOperationStatus.WantRead) input = socket.Receive(...);
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class TlsDetachedSession : TlsSession
{
    private readonly IntPtr _rbio;   // peer → ssl  (we BIO_write here)
    private readonly IntPtr _wbio;   // ssl  → peer (we BIO_read here)

    private TlsDetachedSession(SafeSsl native, IntPtr rbio, IntPtr wbio, bool isServer)
        : base(native, isServer)
    {
        _rbio = rbio;
        _wbio = wbio;
    }

    public override TlsTransportMode TransportMode => TlsTransportMode.Detached;

    internal static TlsDetachedSession Create(TlsContext context, bool isServer)
    {
        ArgumentNullException.ThrowIfNull(context);

        IntPtr ssl = OpenSslInterop.SSL_new(context.Native.DangerousHandle);
        if (ssl == IntPtr.Zero)
        {
            throw new InvalidOperationException("SSL_new failed: " + OpenSslInterop.GetLastErrorString());
        }

        var safe = new SafeSsl(ssl);

        IntPtr rbio = OpenSslInterop.BIO_new(OpenSslInterop.BIO_s_mem());
        IntPtr wbio = OpenSslInterop.BIO_new(OpenSslInterop.BIO_s_mem());
        if (rbio == IntPtr.Zero || wbio == IntPtr.Zero)
        {
            if (rbio != IntPtr.Zero) OpenSslInterop.BIO_free(rbio);
            if (wbio != IntPtr.Zero) OpenSslInterop.BIO_free(wbio);
            safe.Dispose();
            throw new InvalidOperationException("BIO_new failed.");
        }

        // SSL_set_bio takes ownership of both BIOs (frees them with SSL_free).
        OpenSslInterop.SSL_set_bio(ssl, rbio, wbio);

        if (isServer) OpenSslInterop.SSL_set_accept_state(ssl);
        else          OpenSslInterop.SSL_set_connect_state(ssl);

        return new TlsDetachedSession(safe, rbio, wbio, isServer);
    }

    /// <summary>
    /// Drives one step of the TLS handshake.
    /// Feed any bytes received from the peer in <paramref name="input"/>;
    /// any bytes the TLS layer wants to send to the peer are written into <paramref name="output"/>.
    /// </summary>
    /// <returns>
    /// <see cref="TlsOperationStatus.Complete"/> when the handshake is finished.
    /// <see cref="TlsOperationStatus.WantRead"/> when more input is needed from the peer.
    /// <see cref="TlsOperationStatus.WantWrite"/> when there are still bytes pending to send
    /// (call again with a larger output buffer).
    /// </returns>
    public unsafe TlsOperationStatus ProcessHandshake(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        out int consumed,
        out int produced)
    {
        consumed = 0;
        produced = 0;

        // 1. Feed received ciphertext to OpenSSL's input BIO.
        if (!input.IsEmpty)
        {
            fixed (byte* p = input)
            {
                int n = OpenSslInterop.BIO_write(_rbio, p, input.Length);
                if (n > 0) consumed = n;
            }
        }

        OpenSslInterop.ERR_clear_error();

        // 2. Drive the handshake state machine.
        int rc = OpenSslInterop.SSL_do_handshake(Native.DangerousHandle);
        TlsOperationStatus status;
        if (rc == 1)
        {
            MarkHandshakeComplete();
            status = TlsOperationStatus.Complete;
        }
        else
        {
            int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
            status = err switch
            {
                OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
                OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
                OpenSslInterop.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
                OpenSslInterop.SSL_ERROR_SYSCALL => TlsOperationStatus.Closed,
                _ => throw new InvalidOperationException(
                    $"SSL_do_handshake failed (err={err}): {OpenSslInterop.GetLastErrorString()}"),
            };
        }

        // 3. Drain pending ciphertext that OpenSSL wants us to send.
        produced = DrainOutput(output);

        // If we produced bytes and OpenSSL didn't already say WantRead, prefer WantWrite to signal "send these first".
        if (produced > 0 && status != TlsOperationStatus.Complete && status != TlsOperationStatus.Closed)
        {
            // If output buffer was filled and there is more pending, bias toward WantWrite.
            if (OpenSslInterop.BIO_pending(_wbio) > 0)
            {
                status = TlsOperationStatus.WantWrite;
            }
        }

        return status;
    }

    /// <summary>
    /// Decrypts ciphertext received from the peer into plaintext.
    /// </summary>
    public unsafe TlsOperationStatus Decrypt(
        ReadOnlySpan<byte> ciphertext,
        Span<byte> plaintext,
        out int consumed,
        out int produced)
    {
        if (!IsHandshakeComplete)
        {
            throw new InvalidOperationException("Handshake has not completed.");
        }

        consumed = 0;
        produced = 0;

        if (!ciphertext.IsEmpty)
        {
            fixed (byte* p = ciphertext)
            {
                int n = OpenSslInterop.BIO_write(_rbio, p, ciphertext.Length);
                if (n > 0) consumed = n;
            }
        }

        OpenSslInterop.ERR_clear_error();

        int rc;
        fixed (byte* outPtr = plaintext)
        {
            rc = OpenSslInterop.SSL_read(Native.DangerousHandle, outPtr, plaintext.Length);
        }

        if (rc > 0)
        {
            produced = rc;
            return TlsOperationStatus.Complete;
        }

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

    /// <summary>
    /// Encrypts plaintext into ciphertext to send to the peer.
    /// </summary>
    public unsafe TlsOperationStatus Encrypt(
        ReadOnlySpan<byte> plaintext,
        Span<byte> ciphertext,
        out int consumed,
        out int produced)
    {
        if (!IsHandshakeComplete)
        {
            throw new InvalidOperationException("Handshake has not completed.");
        }

        consumed = 0;
        produced = 0;

        OpenSslInterop.ERR_clear_error();

        if (!plaintext.IsEmpty)
        {
            int rc;
            fixed (byte* p = plaintext)
            {
                rc = OpenSslInterop.SSL_write(Native.DangerousHandle, p, plaintext.Length);
            }

            if (rc > 0)
            {
                consumed = rc;
            }
            else
            {
                int err = OpenSslInterop.SSL_get_error(Native.DangerousHandle, rc);
                return err switch
                {
                    OpenSslInterop.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
                    OpenSslInterop.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
                    OpenSslInterop.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
                    OpenSslInterop.SSL_ERROR_SYSCALL => TlsOperationStatus.Closed,
                    _ => throw new InvalidOperationException(
                        $"SSL_write failed (err={err}): {OpenSslInterop.GetLastErrorString()}"),
                };
            }
        }

        produced = DrainOutput(ciphertext);
        return TlsOperationStatus.Complete;
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

    private unsafe int DrainOutput(Span<byte> destination)
    {
        if (destination.IsEmpty) return 0;

        long pending = OpenSslInterop.BIO_pending(_wbio);
        if (pending <= 0) return 0;

        int toRead = (int)Math.Min(pending, destination.Length);
        fixed (byte* p = destination)
        {
            int n = OpenSslInterop.BIO_read(_wbio, p, toRead);
            return n > 0 ? n : 0;
        }
    }
}
