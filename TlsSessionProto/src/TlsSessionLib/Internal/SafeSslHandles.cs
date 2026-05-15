using System.Runtime.InteropServices;

namespace System.Net.Security.Prototype.Internal;

internal sealed class SafeSslContext : SafeHandle
{
    public SafeSslContext() : base(IntPtr.Zero, ownsHandle: true) { }

    public SafeSslContext(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            OpenSslInterop.SSL_CTX_free(handle);
            SetHandle(IntPtr.Zero);
        }
        return true;
    }

    public IntPtr DangerousHandle => handle;
}

internal sealed class SafeSsl : SafeHandle
{
    public SafeSsl() : base(IntPtr.Zero, ownsHandle: true) { }

    public SafeSsl(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            try
            {
                OpenSslInterop.SSL_set_quiet_shutdown(handle, 1);
                OpenSslInterop.SSL_shutdown(handle);
            }
            catch
            {
                // best-effort
            }

            OpenSslInterop.SSL_free(handle);
            SetHandle(IntPtr.Zero);
        }
        return true;
    }

    public IntPtr DangerousHandle => handle;
}
