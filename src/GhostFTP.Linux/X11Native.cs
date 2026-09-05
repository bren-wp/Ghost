using System.Runtime.InteropServices;

namespace GhostFTP.Linux;

internal static class X11Native
{
    private const string X11 = "libX11.so.6";

    internal const int KeyPress = 2;
    internal const int ButtonPress = 4;
    internal const int ButtonRelease = 5;
    internal const int MotionNotify = 6;
    internal const int Expose = 12;
    internal const int ConfigureNotify = 22;
    internal const int ClientMessage = 33;

    internal const long KeyPressMask = 1L << 0;
    internal const long ButtonPressMask = 1L << 2;
    internal const long ButtonReleaseMask = 1L << 3;
    internal const long PointerMotionMask = 1L << 6;
    internal const long ExposureMask = 1L << 15;
    internal const long StructureNotifyMask = 1L << 17;

    internal const nuint XkBackSpace = 0xFF08;
    internal const nuint XkTab = 0xFF09;
    internal const nuint XkReturn = 0xFF0D;
    internal const nuint XkEscape = 0xFF1B;
    internal const nuint XkHome = 0xFF50;
    internal const nuint XkLeft = 0xFF51;
    internal const nuint XkUp = 0xFF52;
    internal const nuint XkRight = 0xFF53;
    internal const nuint XkDown = 0xFF54;
    internal const nuint XkDelete = 0xFFFF;
    internal const nuint XkF2 = 0xFFBF;
    internal const nuint XkF5 = 0xFFC2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct XColor
    {
        internal nuint pixel;
        internal ushort red;
        internal ushort green;
        internal ushort blue;
        internal byte flags;
        internal byte pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XKeyEvent
    {
        internal int type;
        internal nuint serial;
        internal int send_event;
        internal IntPtr display;
        internal nuint window;
        internal nuint root;
        internal nuint subwindow;
        internal nuint time;
        internal int x;
        internal int y;
        internal int x_root;
        internal int y_root;
        internal uint state;
        internal uint keycode;
        internal int same_screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XButtonEvent
    {
        internal int type;
        internal nuint serial;
        internal int send_event;
        internal IntPtr display;
        internal nuint window;
        internal nuint root;
        internal nuint subwindow;
        internal nuint time;
        internal int x;
        internal int y;
        internal int x_root;
        internal int y_root;
        internal uint state;
        internal uint button;
        internal int same_screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XMotionEvent
    {
        internal int type;
        internal nuint serial;
        internal int send_event;
        internal IntPtr display;
        internal nuint window;
        internal nuint root;
        internal nuint subwindow;
        internal nuint time;
        internal int x;
        internal int y;
        internal int x_root;
        internal int y_root;
        internal uint state;
        internal char is_hint;
        internal int same_screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XConfigureEvent
    {
        internal int type;
        internal nuint serial;
        internal int send_event;
        internal IntPtr display;
        internal nuint @event;
        internal nuint window;
        internal int x;
        internal int y;
        internal int width;
        internal int height;
        internal int border_width;
        internal nuint above;
        internal int override_redirect;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XClientMessageEvent
    {
        internal int type;
        internal nuint serial;
        internal int send_event;
        internal IntPtr display;
        internal nuint window;
        internal nuint message_type;
        internal int format;
        internal nint data0;
        internal nint data1;
        internal nint data2;
        internal nint data3;
        internal nint data4;
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    internal struct XEvent
    {
        [FieldOffset(0)] internal int type;
        [FieldOffset(0)] internal XKeyEvent xkey;
        [FieldOffset(0)] internal XButtonEvent xbutton;
        [FieldOffset(0)] internal XMotionEvent xmotion;
        [FieldOffset(0)] internal XConfigureEvent xconfigure;
        [FieldOffset(0)] internal XClientMessageEvent xclient;
    }

    [DllImport(X11)]
    internal static extern int XInitThreads();

    [DllImport(X11)]
    internal static extern IntPtr XOpenDisplay(IntPtr display_name);

    [DllImport(X11)]
    internal static extern int XCloseDisplay(IntPtr display);

    [DllImport(X11)]
    internal static extern int XDefaultScreen(IntPtr display);

    [DllImport(X11)]
    internal static extern nuint XRootWindow(IntPtr display, int screen_number);

    [DllImport(X11)]
    internal static extern nuint XBlackPixel(IntPtr display, int screen_number);

    [DllImport(X11)]
    internal static extern nuint XWhitePixel(IntPtr display, int screen_number);

    [DllImport(X11)]
    internal static extern nuint XDefaultColormap(IntPtr display, int screen_number);

    [DllImport(X11)]
    internal static extern nuint XCreateSimpleWindow(
        IntPtr display,
        nuint parent,
        int x,
        int y,
        uint width,
        uint height,
        uint border_width,
        nuint border,
        nuint background);

    [DllImport(X11, CharSet = CharSet.Ansi)]
    internal static extern int XStoreName(IntPtr display, nuint window, string window_name);

    [DllImport(X11)]
    internal static extern int XSelectInput(IntPtr display, nuint window, long event_mask);

    [DllImport(X11)]
    internal static extern int XMapWindow(IntPtr display, nuint window);

    [DllImport(X11)]
    internal static extern int XDestroyWindow(IntPtr display, nuint window);

    [DllImport(X11)]
    internal static extern int XPending(IntPtr display);

    [DllImport(X11)]
    internal static extern int XNextEvent(IntPtr display, out XEvent xevent);

    [DllImport(X11)]
    internal static extern IntPtr XCreateGC(IntPtr display, nuint drawable, nuint valuemask, IntPtr values);

    [DllImport(X11)]
    internal static extern int XFreeGC(IntPtr display, IntPtr gc);

    [DllImport(X11)]
    internal static extern int XSetForeground(IntPtr display, IntPtr gc, nuint foreground);

    [DllImport(X11)]
    internal static extern int XFillRectangle(IntPtr display, nuint drawable, IntPtr gc, int x, int y, uint width, uint height);

    [DllImport(X11)]
    internal static extern int XDrawRectangle(IntPtr display, nuint drawable, IntPtr gc, int x, int y, uint width, uint height);

    [DllImport(X11)]
    internal static extern int XDrawLine(IntPtr display, nuint drawable, IntPtr gc, int x1, int y1, int x2, int y2);

    [DllImport(X11)]
    internal static extern int XFlush(IntPtr display);

    [DllImport(X11, CharSet = CharSet.Ansi)]
    internal static extern nuint XInternAtom(IntPtr display, string atom_name, int only_if_exists);

    [DllImport(X11)]
    internal static extern int XSetWMProtocols(IntPtr display, nuint window, ref nuint protocols, int count);

    [DllImport(X11)]
    internal static extern int XParseColor(IntPtr display, nuint colormap, string spec, ref XColor exact_def_return);

    [DllImport(X11)]
    internal static extern int XAllocColor(IntPtr display, nuint colormap, ref XColor screen_in_out);

    [DllImport(X11, CharSet = CharSet.Ansi)]
    internal static extern IntPtr XCreateFontSet(
        IntPtr display,
        string base_font_name_list,
        out IntPtr missing_charset_list,
        out int missing_charset_count,
        out IntPtr def_string);

    [DllImport(X11)]
    internal static extern void XFreeFontSet(IntPtr display, IntPtr font_set);

    [DllImport(X11)]
    internal static extern void XFreeStringList(IntPtr list);

    [DllImport(X11)]
    internal static extern void Xutf8DrawString(
        IntPtr display,
        nuint drawable,
        IntPtr font_set,
        IntPtr gc,
        int x,
        int y,
        byte[] text,
        int bytes_text);

    [DllImport(X11)]
    internal static extern int XLookupString(
        ref XKeyEvent event_struct,
        byte[] buffer_return,
        int bytes_buffer,
        out nuint keysym_return,
        IntPtr status_in_out);

    [DllImport(X11)]
    internal static extern nuint XLookupKeysym(ref XKeyEvent key_event, int index);
}
