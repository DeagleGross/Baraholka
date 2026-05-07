using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/// <summary>
/// Measures the managed overhead of Socket.Accept() vs a raw accept() syscall.
///
/// Socket.Accept() does:
///   1. accept4/accept syscall (same in both)
///   2. CreateAcceptSocket → new Socket(SafeSocketHandle) → getpeername + getsockopt + managed state
///   3. Returns a fully-initialized Socket object (allocation)
///
/// RawAccept simulates what TryAcceptNonBlocking would do:
///   1. Same accept syscall
///   2. Returns just the fd wrapped in SafeSocketHandle (no Socket object)
///
/// EagainException measures the cost of the exception-driven EAGAIN path
/// when Socket.Accept() is called on a non-blocking socket with no pending connections.
/// </summary>
[MemoryDiagnoser]
public class AcceptBenchmark
{
    private const int BatchSize = 64;

    private Socket _listenSocket = null!;
    private Socket _listenSocketNonBlocking = null!;
    private IPEndPoint _endpoint = null!;
    private IPEndPoint _endpointNonBlocking = null!;
    private Socket[] _pendingClients = null!;
    private Socket[] _pendingClientsRaw = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Listen socket for Accept / RawAccept benchmarks (blocking mode)
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _listenSocket.Listen(512);
        _endpoint = (IPEndPoint)_listenSocket.LocalEndPoint!;

        // Separate listen socket for EAGAIN benchmark (non-blocking mode)
        _listenSocketNonBlocking = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenSocketNonBlocking.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _listenSocketNonBlocking.Listen(512);
        _listenSocketNonBlocking.Blocking = false;
        _endpointNonBlocking = (IPEndPoint)_listenSocketNonBlocking.LocalEndPoint!;
    }

    [IterationSetup(Targets = [nameof(SocketAccept)])]
    public void SetupSocketAccept()
    {
        _pendingClients = PreConnect(_endpoint, BatchSize);
    }

    [IterationSetup(Targets = [nameof(RawAccept)])]
    public void SetupRawAccept()
    {
        _pendingClientsRaw = PreConnect(_endpoint, BatchSize);
    }

    [IterationCleanup(Targets = [nameof(SocketAccept)])]
    public void CleanupSocketAccept() => DisposeAll(_pendingClients);

    [IterationCleanup(Targets = [nameof(RawAccept)])]
    public void CleanupRawAccept() => DisposeAll(_pendingClientsRaw);

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _listenSocket.Dispose();
        _listenSocketNonBlocking.Dispose();
    }

    /// <summary>
    /// Socket.Accept() — the current API. Creates a full Socket object per accept.
    /// Measures: syscall + getpeername + getsockopt + Socket allocation + managed state init.
    /// </summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void SocketAccept()
    {
        for (int i = 0; i < BatchSize; i++)
        {
            using Socket accepted = _listenSocket.Accept();
        }
    }

    /// <summary>
    /// Raw accept — simulates TryAcceptNonBlocking.
    /// Same syscall, but returns just a SafeSocketHandle (no Socket object).
    /// Measures: syscall + SafeSocketHandle wrap only.
    /// </summary>
    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void RawAccept()
    {
        Span<byte> addrBuffer = stackalloc byte[128];
        for (int i = 0; i < BatchSize; i++)
        {
            int addrLen = addrBuffer.Length;
            nint fd;
            unsafe
            {
                fixed (byte* pAddr = addrBuffer)
                {
                    fd = NativeAccept(_listenSocket.Handle, pAddr, ref addrLen);
                }
            }

            if (fd >= 0 && fd != INVALID_SOCKET)
            {
                // This is what TryAcceptNonBlocking would do — just wrap the fd.
                // SafeSocketHandle ctor is already public.
                using var handle = new SafeSocketHandle(fd, ownsHandle: true);
            }
        }
    }

    /// <summary>
    /// Measures the cost of EAGAIN as an exception when calling Socket.Accept()
    /// on a non-blocking listen socket with no pending connections.
    /// This is the hot-path cost that TryAcceptNonBlocking avoids by returning false.
    /// </summary>
    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void EagainException()
    {
        // Drain any stale connections first
        _listenSocketNonBlocking.Blocking = false;
        DrainAccept(_listenSocketNonBlocking);

        for (int i = 0; i < BatchSize; i++)
        {
            try
            {
                _listenSocketNonBlocking.Accept();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                // This is the normal "no pending connection" path.
                // The exception allocation + throw + catch is the overhead.
            }
        }
    }

    /// <summary>
    /// Simulates TryAcceptNonBlocking's EAGAIN path — just a return value check, no exception.
    /// </summary>
    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void EagainReturnValue()
    {
        Span<byte> addrBuffer = stackalloc byte[128];

        // Drain any stale connections first (using the non-blocking socket)
        DrainAccept(_listenSocketNonBlocking);

        for (int i = 0; i < BatchSize; i++)
        {
            int addrLen = addrBuffer.Length;
            nint fd;
            unsafe
            {
                fixed (byte* pAddr = addrBuffer)
                {
                    // Use _listenSocketNonBlocking so accept() returns EAGAIN instead of blocking
                    fd = NativeAccept(_listenSocketNonBlocking.Handle, pAddr, ref addrLen);
                }
            }

            if (fd >= 0 && fd != INVALID_SOCKET)
            {
                // Shouldn't happen — no pending connections
                NativeClose(fd);
            }
            // else: EAGAIN — just a return value check, zero overhead
        }
    }

    #region Helpers

    private static Socket[] PreConnect(IPEndPoint endpoint, int count)
    {
        var sockets = new Socket[count];
        for (int i = 0; i < count; i++)
        {
            sockets[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sockets[i].Connect(endpoint);
        }
        return sockets;
    }

    private static void DisposeAll(Socket[]? sockets)
    {
        if (sockets is null) return;
        foreach (var s in sockets)
            s?.Dispose();
    }

    private static void DrainAccept(Socket listenSocket)
    {
        while (true)
        {
            try
            {
                using Socket s = listenSocket.Accept();
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    #endregion

    #region Platform P/Invoke

    private static readonly nint INVALID_SOCKET = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? ~(nint)0   // Windows: (SOCKET)(~0)
        : -1;        // Unix: -1

    private static unsafe nint NativeAccept(nint socket, byte* addr, ref int addrLen)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fixed (int* pLen = &addrLen)
            {
                return ws2_accept(socket, addr, pLen);
            }
        }
        else
        {
            // On Unix, use the libc accept. For a real TryAcceptNonBlocking,
            // we'd use accept4(SOCK_NONBLOCK | SOCK_CLOEXEC) on Linux.
            fixed (int* pLen = &addrLen)
            {
                return libc_accept(socket, addr, pLen);
            }
        }
    }

    private static void NativeClose(nint fd)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ws2_closesocket(fd);
        else
            libc_close((int)fd);
    }

    // Windows: ws2_32.dll
    [DllImport("ws2_32.dll", EntryPoint = "accept", SetLastError = true)]
    private static extern unsafe nint ws2_accept(nint s, byte* addr, int* addrlen);

    [DllImport("ws2_32.dll", EntryPoint = "closesocket")]
    private static extern int ws2_closesocket(nint s);

    // Unix: libc
    [DllImport("libc", EntryPoint = "accept", SetLastError = true)]
    private static extern unsafe nint libc_accept(nint s, byte* addr, int* addrlen);

    [DllImport("libc", EntryPoint = "close")]
    private static extern int libc_close(int fd);

    #endregion
}
