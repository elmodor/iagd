using System.Runtime.InteropServices;
using System.Reflection;

namespace IAGrim.Linux;

internal static class WebKitGtkInterop
{
    private const string WebKit = "webkit2gtk-4.1";
    private const string GLib = "glib-2.0";
    private const string Gio = "gio-2.0";
    private const string GObject = "gobject-2.0";

    static WebKitGtkInterop() {
        NativeLibrary.SetDllImportResolver(typeof(WebKitGtkInterop).Assembly, ResolveLibrary);
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        var candidates = new[] { $"{libraryName}.so.0", $"lib{libraryName}.so.0" };
        foreach (var candidate in candidates)
        {
            if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle)) {
                return handle;
            }
        }
        return IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UriSchemeRequestCallback(IntPtr request, IntPtr userData);

    private static UriSchemeRequestCallback? _callback;

    [DllImport(WebKit, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr webkit_web_view_get_context( IntPtr webView);

    [DllImport(WebKit, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_web_context_register_uri_scheme(IntPtr context, IntPtr scheme, UriSchemeRequestCallback callback, IntPtr userData, IntPtr destroyNotify);

    [DllImport(WebKit, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr webkit_uri_scheme_request_get_path(IntPtr request);

    [DllImport(WebKit, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_uri_scheme_request_finish(IntPtr request, IntPtr stream, long streamLength, IntPtr contentType);

    [DllImport(WebKit, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr webkit_uri_scheme_request_get_uri(IntPtr request);

    [DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_malloc(nuint size);

    [DllImport(GLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_free(IntPtr memory);

    [DllImport(Gio, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_memory_input_stream_new_from_data(IntPtr data, long len, IntPtr destroy);

    [DllImport(GObject, CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_object_unref(IntPtr obj);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GDestroyNotify(IntPtr data);

    private static readonly GDestroyNotify FreeDelegate = g_free;

    public static void Register(IntPtr webView, UriSchemeRequestCallback callback)
    {
        _callback = callback;
        var context = webkit_web_view_get_context(webView);

        if (context == IntPtr.Zero)
            throw new InvalidOperationException("Could not obtain WebKitWebContext.");

        var scheme = Marshal.StringToCoTaskMemUTF8("iagrim");
        try
        {
            webkit_web_context_register_uri_scheme(context, scheme, _callback, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeCoTaskMem(scheme);
        }
    }

    public static string GetPath(IntPtr request)
    {
        var ptr = webkit_uri_scheme_request_get_path(request);

        if (ptr == IntPtr.Zero)
            return "/";

        return Marshal.PtrToStringUTF8(ptr)
               ?? "/";
    }

    public static string GetUri(
    IntPtr request)
    {
        var ptr = webkit_uri_scheme_request_get_uri(request);

        if (ptr == IntPtr.Zero)
            return string.Empty;

        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public static void Finish(IntPtr request, byte[] data, string contentType)
    {
        var nativeData = g_malloc((nuint)data.Length);

        if (nativeData == IntPtr.Zero)
            throw new OutOfMemoryException();

        Marshal.Copy(data, 0, nativeData, data.Length);

        var destroy = Marshal.GetFunctionPointerForDelegate(FreeDelegate);

        var stream = g_memory_input_stream_new_from_data(nativeData, data.Length, destroy);

        if (stream == IntPtr.Zero)
        {
            g_free(nativeData);
            throw new InvalidOperationException("Could not create GMemoryInputStream.");
        }

        var mimeType = Marshal.StringToCoTaskMemUTF8(contentType);

        try
        {
            webkit_uri_scheme_request_finish(request, stream, data.Length, mimeType);
        }
        finally
        {
            Marshal.FreeCoTaskMem(mimeType);
            g_object_unref(stream);
        }
    }
}
