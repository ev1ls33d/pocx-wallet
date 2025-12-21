using System;
using System.Runtime.InteropServices;

namespace PocxWallet.Core.VanityAddress;

/// <summary>
/// P/Invoke wrapper for native CUDA-accelerated vanity search
/// Provides true GPU acceleration via native VanitySearch-PocX integration
/// </summary>
public static class NativeVanitySearch
{
    private const string LibraryName = "pocxwallet_native";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VanitySearchResult
    {
        public int Found;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Mnemonic;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Address;
        
        public ulong Attempts;
        public double ElapsedSeconds;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProgressCallback(ulong attempts, double rate);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int vanitysearch_init();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int vanitysearch_find(
        [MarshalAs(UnmanagedType.LPStr)] string pattern,
        int useGpu,
        ulong maxAttempts,
        ProgressCallback? progressCallback,
        out VanitySearchResult result);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void vanitysearch_stop();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void vanitysearch_cleanup();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr vanitysearch_get_error();

    public static string? GetLastError()
    {
        var ptr = vanitysearch_get_error();
        return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
    }

    /// <summary>
    /// Check if native library is available
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            vanitysearch_init();
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}
