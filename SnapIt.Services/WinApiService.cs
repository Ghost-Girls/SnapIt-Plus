using System.Runtime.InteropServices;
using System.Windows.Forms;
using SnapIt.Common;
using SnapIt.Common.Contracts;
using SnapIt.Common.Entities;
using SnapIt.Common.Graphics;
using SnapIt.Common.InteropServices;
using SnapIt.Services.Contracts;

namespace SnapIt.Services;

public class WinApiService : IWinApiService
{
    private const int MAX_PATH = 260;
    public delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    public const uint EVENT_OBJECT_CREATE = 0x8000;
    public const uint EVENT_OBJECT_SHOW = 0x8002;
    public const uint EVENT_OBJECT_DESTROY = 0x8001;
    public const uint EVENT_OBJECT_HIDE = 0x8003;
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
    public const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    public const uint WINEVENT_SKIPOWNTHREAD = 0x0001;
    public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    public const uint WINEVENT_INCONTEXT = 0x0004;

    public const int OBJID_WINDOW = 0x00000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);

    public static IDisposable BeginPerMonitorV2()
    {
        var previous = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        return new DpiContextGuard(previous);
    }

    private class DpiContextGuard : IDisposable
    {
        private readonly IntPtr previous;
        public DpiContextGuard(IntPtr previous) { this.previous = previous; }
        public void Dispose() { SetThreadDpiAwarenessContext(previous); }
    }

    private readonly ISettingService settingService;
    private readonly ILoggerService? logger;

    public bool IsInitialized { get; private set; }

    public WinApiService(ISettingService settingService, ILoggerService? loggerService = null)
    {
        this.settingService = settingService;
        logger = loggerService;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        IsInitialized = true;
    }

    public IDictionary<nint, string> GetOpenWindows()
    {
        var shellWindow = PInvoke.User32.GetShellWindow();
        var windows = new Dictionary<nint, string>();

        PInvoke.User32.EnumWindows(delegate (nint hwnd, nint lParam)
        {
            if (hwnd == shellWindow) return true;
            if (!PInvoke.User32.IsWindowVisible(hwnd)) return true;

            var length = PInvoke.User32.GetWindowTextLength(hwnd);
            if (length == 0) return true;

            var text = new char[length + 1];
            var finalLength = PInvoke.User32.GetWindowText(hwnd, text, length + 1);
            if (finalLength == 0) return true;

            windows[hwnd] = new string(text, 0, finalLength);
            return true;
        }, nint.Zero);

        return windows;
    }

    public IEnumerable<string> GetOpenWindowsNames()
    {
        return GetOpenWindows().Select(i => i.Value).Distinct().OrderBy(i => i);
    }

    public bool IsAllowedWindowStyle(ActiveWindow activeWindow)
    {
        var style = PInvoke.User32.GetWindowLong(activeWindow.Handle, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE);

        var windowStyle = (PInvoke.User32.WindowStylesEx)(style & (uint)PInvoke.User32.WindowStylesEx.WS_EX_APPWINDOW);

        return windowStyle == PInvoke.User32.WindowStylesEx.WS_EX_APPWINDOW;
    }

    public bool IsFullscreen(ActiveWindow activeWindow)
    {
        if (activeWindow == null || activeWindow.Boundry == null)
            return false;

        PInvoke.User32.GetWindowRect(PInvoke.User32.GetDesktopWindow(), out PInvoke.RECT desktopWindow);
        var isFullScreen = activeWindow.Boundry.Left == desktopWindow.left &&
                activeWindow.Boundry.Top == desktopWindow.top &&
                activeWindow.Boundry.Right == desktopWindow.right &&
                activeWindow.Boundry.Bottom == desktopWindow.bottom;

        return isFullScreen;
    }

    public void MoveWindow(ActiveWindow activeWindow, Rectangle newRect)
    {
        MoveWindow(activeWindow, (int)newRect.X, (int)newRect.Y, (int)newRect.Width, (int)newRect.Height);
    }

    public void MoveWindow(ActiveWindow activeWindow, int X, int Y, int width, int height)
    {
        if (activeWindow == null) return;

        var handle = activeWindow.Handle;
        var title = activeWindow.Title ?? "(unknown)";
        var className = activeWindow.ClassName ?? "(unknown)";

        PInvoke.User32.GetWindowRect(handle, out PInvoke.RECT beforeRect);
        var beforeDesc = $"left={beforeRect.left},top={beforeRect.top},right={beforeRect.right},bottom={beforeRect.bottom} sz={beforeRect.right - beforeRect.left}x{beforeRect.bottom - beforeRect.top}";

        LogDebug($"[MoveWindow] Window=\"{title}\" Class=\"{className}\" Handle={handle}");
        LogDebug($"[MoveWindow] BEFORE: {beforeDesc}");
        LogDebug($"[MoveWindow] TARGET: X={X},Y={Y} W={width}xH={height}");

        PInvoke.User32.ShowWindow(handle, PInvoke.User32.WindowShowStyle.SW_SHOWNORMAL);

        PInvoke.User32.SetWindowPos(
            handle,
            PInvoke.User32.SpecialWindowHandles.HWND_TOP,
            X, Y, 0, 0,
            PInvoke.User32.SetWindowPosFlags.SWP_NOSIZE |
            PInvoke.User32.SetWindowPosFlags.SWP_SHOWWINDOW |
            PInvoke.User32.SetWindowPosFlags.SWP_NOACTIVATE);

        PInvoke.User32.SetWindowPos(
            handle,
            PInvoke.User32.SpecialWindowHandles.HWND_TOP,
            0, 0, width, height,
            PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE |
            PInvoke.User32.SetWindowPosFlags.SWP_SHOWWINDOW |
            PInvoke.User32.SetWindowPosFlags.SWP_NOACTIVATE);

        PInvoke.User32.GetWindowRect(handle, out PInvoke.RECT afterRect);
        var afterDesc = $"left={afterRect.left},top={afterRect.top},right={afterRect.right},bottom={afterRect.bottom} sz={afterRect.right - afterRect.left}x{afterRect.bottom - afterRect.top}";
        LogDebug($"[MoveWindow] AFTER: {afterDesc}");

        if (beforeRect.left != afterRect.left || beforeRect.top != afterRect.top ||
            beforeRect.right != afterRect.right || beforeRect.bottom != afterRect.bottom)
        {
            LogDebug($"[MoveWindow] VERIFY-IMMEDIATE: Match");
        }
        else
        {
            LogWarn($"[MoveWindow] VERIFY-IMMEDIATE: Position unchanged! BEFORE={beforeDesc} AFTER={afterDesc}");
        }

        _ = VerifyPositionAsync(handle, X, Y, width, height);
    }

    public void SendMessage(ActiveWindow activeWindow)
    {
        PInvoke.User32.SendMessage(activeWindow.Handle, PInvoke.User32.WindowMessage.WM_KEYDOWN, (nint)Keys.Escape, (nint)0);
    }

    public void GetWindowMargin(ActiveWindow activeWindow, out Rectangle withMargin)
    {
        if (activeWindow != null)
        {
            using (BeginPerMonitorV2())
            {
                PInvoke.User32.GetWindowRect(activeWindow.Handle, out PInvoke.RECT dpiAwareRect);
                var t = new PInvoke.RECT();
                DwmApi.DwmGetWindowAttribute(
                                activeWindow.Handle,
                                DWMWINDOWATTRIBUTE.ExtendedFrameBounds,
                                out t,
                                Marshal.SizeOf(typeof(PInvoke.RECT)));

                var boundry = activeWindow.Boundry;
                var dpiAwareW = dpiAwareRect.right - dpiAwareRect.left;
                var marginH = (dpiAwareW - (t.right - t.left)) / 2;
                LogDebug($"[GetWindowMargin] Title=\"{activeWindow.Title}\" Handle={activeWindow.Handle}");
                LogDebug($"[GetWindowMargin] WindowBoundry=({boundry.Left},{boundry.Top})-({boundry.Right},{boundry.Bottom}) sz={boundry.Width}x{boundry.Height}");
                LogDebug($"[GetWindowMargin] DpiAwareRect=({dpiAwareRect.left},{dpiAwareRect.top})-({dpiAwareRect.right},{dpiAwareRect.bottom}) sz={dpiAwareW}x{dpiAwareRect.bottom - dpiAwareRect.top}");
                LogDebug($"[GetWindowMargin] ExtendedFrameBounds=left={t.left},top={t.top},right={t.right},bottom={t.bottom} sz={t.right - t.left}x{t.bottom - t.top}");
                LogDebug($"[GetWindowMargin] Calculated marginHorizontal={marginH}");

                withMargin = new Rectangle(t.left, t.top, t.right, t.bottom);
            }
        }
        else
        {
            withMargin = Rectangle.Empty;
        }
    }

    public ActiveWindow GetActiveWindow()
    {
        var activeWindow = new ActiveWindow
        {
            Handle = PInvoke.User32.GetForegroundWindow()
        };

        // 获取窗口标题
        var chars = 256;
        var buff = new char[chars + 1];
        var length = PInvoke.User32.GetWindowText(activeWindow.Handle, buff, chars);
        if (length > 0)
        {
            activeWindow.Title = new string(buff, 0, length);
        }

        // 获取窗口类名
        var classBuff = new char[256];
        var classLength = PInvoke.User32.GetClassName(activeWindow.Handle, classBuff, 256);
        if (classLength > 0)
        {
            activeWindow.ClassName = new string(classBuff, 0, classLength);
        }

        // 获取窗口矩形（PerMonitorV2 上下文确保物理坐标）
        using (BeginPerMonitorV2())
        {
            if (PInvoke.User32.GetWindowRect(activeWindow.Handle, out PInvoke.RECT physicalRect))
            {
                activeWindow.Boundry = new Rectangle(physicalRect.left, physicalRect.top, physicalRect.right, physicalRect.bottom);
            }
        }

        if (activeWindow.Handle == nint.Zero || activeWindow.Boundry.Equals(Rectangle.Empty))
            activeWindow = ActiveWindow.Empty;

        return activeWindow;
    }

    public string GetCurrentDesktopWallpaper()
    {
        var buff = new char[MAX_PATH];

        nint ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buff, 0);

        PInvoke.User32.SystemParametersInfo(
            PInvoke.User32.SystemParametersInfoAction.SPI_GETDESKWALLPAPER,
            (uint)buff.Length,
            ptr,
            PInvoke.User32.SystemParametersInfoFlags.None);

        var currentWallpaper = new string(buff, 0, buff.Length);
        return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
    }

    public void SetWindowCornerPreference(ActiveWindow activeWindow, DWM_WINDOW_CORNER_PREFERENCE preference)
    {
        if (activeWindow == null || activeWindow.Handle == nint.Zero)
            return;

        try
        {
            int pref = (int)preference;
            DwmApi.DwmSetWindowAttribute(
                activeWindow.Handle,
                DWMWINDOWATTRIBUTE.WindowCornerPreference,
                ref pref,
                sizeof(int));
        }
        catch (Exception ex)
        {
            Dev.Log($"Failed to set window corner preference: {ex.Message}");
        }
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    private async Task VerifyPositionAsync(nint handle, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
    {
        var delays = new[] { 200, 500, 1000 };

        foreach (var delay in delays)
        {
            await Task.Delay(delay);

            if (!PInvoke.User32.IsWindow(handle))
            {
                LogWarn($"[VerifyPosition] Window handle {handle} is no longer valid at +{delay}ms");
                return;
            }

            PInvoke.User32.GetWindowRect(handle, out PInvoke.RECT currentRect);
            var actualLeft = currentRect.left;
            var actualTop = currentRect.top;
            var actualRight = currentRect.right;
            var actualBottom = currentRect.bottom;
            var actualWidth = actualRight - actualLeft;
            var actualHeight = actualBottom - actualTop;

            var match = actualLeft == expectedX && actualTop == expectedY &&
                        actualWidth == expectedWidth && actualHeight == expectedHeight;

            if (match)
            {
                LogDebug($"[VerifyPosition] +{delay}ms: Match expected=({expectedX},{expectedY}) sz={expectedWidth}x{expectedHeight}");
            }
            else
            {
                LogWarn($"[VerifyPosition] +{delay}ms: Mismatch! actual=({actualLeft},{actualTop})-({actualRight},{actualBottom}) sz={actualWidth}x{actualHeight} expected=({expectedX},{expectedY}) sz={expectedWidth}x{expectedHeight}");
            }
        }
    }

    private void LogDebug(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        Dev.Log(message, false);
        logger?.LogDebug(message, caller);
    }

    private void LogWarn(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        Dev.Log(message, false);
        logger?.LogWarn(message, caller);
    }

    private void LogError(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        Dev.Log(message, false);
        logger?.LogError(message, caller);
    }
}