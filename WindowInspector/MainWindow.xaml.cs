using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace WindowInspector;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshTimer;
    private nint _selectedHandle;
    private readonly string _logPath;
    private readonly object _logLock = new();
    private string _lastLogSnapshot = "";

    public MainWindow()
    {
        InitializeComponent();

        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, $"inspector_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        AppendLog("=== Window Inspector started ===");

        RefreshWindowList();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _refreshTimer.Tick += (_, _) => RefreshSelection();
        _refreshTimer.Start();
    }

    private void AppendLog(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}";
        Debug.WriteLine(line);
        lock (_logLock)
        {
            try { File.AppendAllText(_logPath, line + "\n"); }
            catch { }
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        var items = new List<WindowItem>();
        var shellHwnd = GetShellWindow();

        EnumWindows((hWnd, _) =>
        {
            if (hWnd == shellHwnd) return true;
            if (!IsWindowVisible(hWnd)) return true;

            var titleLen = GetWindowTextLength(hWnd);
            if (titleLen == 0) return true;

            var sb = new System.Text.StringBuilder(titleLen + 1);
            GetWindowText(hWnd, sb, titleLen + 1);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            var classSb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, classSb, 256);
            var className = classSb.ToString();

            items.Add(new WindowItem
            {
                Handle = hWnd,
                Title = title,
                ClassName = className,
                Display = $"[{hWnd}] {title} ({className})"
            });
            return true;
        }, nint.Zero);

        var selectedHandle = _selectedHandle;
        WindowList.ItemsSource = items.OrderBy(i => i.Title).ToList();

        if (selectedHandle != nint.Zero)
        {
            var match = WindowList.ItemsSource.OfType<WindowItem>().FirstOrDefault(w => w.Handle == selectedHandle);
            if (match != null)
            {
                WindowList.SelectedItem = match;
                UpdateWindowInfo(match.Handle);
            }
            else
            {
                _selectedHandle = nint.Zero;
                InfoSection.Visibility = Visibility.Collapsed;
                TxtNoSelection.Visibility = Visibility.Visible;
            }
        }
    }

    private void RefreshSelection()
    {
        if (_selectedHandle != nint.Zero && IsWindow(_selectedHandle))
        {
            UpdateWindowInfo(_selectedHandle, isSilent: true);
        }
    }

    private void WindowList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (WindowList.SelectedItem is WindowItem item)
        {
            _selectedHandle = item.Handle;
            AppendLog($"=== SELECTED: \"{item.Title}\" Class=\"{item.ClassName}\" Handle={item.Handle} ===");
            UpdateWindowInfo(item.Handle);
        }
    }

    private void UpdateWindowInfo(nint hWnd, bool isSilent = false)
    {
        InfoSection.Visibility = Visibility.Visible;
        TxtNoSelection.Visibility = Visibility.Collapsed;

        var title = GetWindowText(hWnd);
        var className = GetWindowClassName(hWnd);

        TxtTitle.Text = title;
        TxtClass.Text = className;
        TxtHandle.Text = $"{hWnd} (0x{hWnd:X})";

        GetWindowRect(hWnd, out RECT totalRect);
        var totalW = totalRect.right - totalRect.left;
        var totalH = totalRect.bottom - totalRect.top;

        TxtTotalLeft.Text = $"Left:   {totalRect.left}";
        TxtTotalTop.Text = $"Top:    {totalRect.top}";
        TxtTotalRight.Text = $"Right:  {totalRect.right}";
        TxtTotalBottom.Text = $"Bottom: {totalRect.bottom}";
        TxtTotalSize.Text = $"Size: {totalW} x {totalH}";

        var hr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameRect, Marshal.SizeOf<RECT>());
        bool dwmOk = hr == 0 && !(frameRect.left == 0 && frameRect.top == 0 && frameRect.right == 0 && frameRect.bottom == 0);

        var dpi = GetWindowDpi(hWnd);

        // Build snapshot for duplicate suppression
        var snapshot = $"{hWnd}|{totalRect.left},{totalRect.top},{totalRect.right},{totalRect.bottom}|{frameRect.left},{frameRect.top},{frameRect.right},{frameRect.bottom}|{dpi}";
        bool shouldLog = !isSilent || snapshot != _lastLogSnapshot;
        _lastLogSnapshot = snapshot;

        if (!shouldLog) return;

        AppendLog($"[GetWindowRect] left={totalRect.left} top={totalRect.top} right={totalRect.right} bottom={totalRect.bottom} size={totalW}x{totalH}");

        if (dwmOk)
        {
            var frameW = frameRect.right - frameRect.left;
            var frameH = frameRect.bottom - frameRect.top;

            TxtFrameLeft.Text = $"Left:   {frameRect.left}";
            TxtFrameTop.Text = $"Top:    {frameRect.top}";
            TxtFrameRight.Text = $"Right:  {frameRect.right}";
            TxtFrameBottom.Text = $"Bottom: {frameRect.bottom}";
            TxtFrameSize.Text = $"Size: {frameW} x {frameH}";

            AppendLog($"[ExtendedFrameBounds] left={frameRect.left} top={frameRect.top} right={frameRect.right} bottom={frameRect.bottom} size={frameW}x{frameH}");

            int leftDelta = frameRect.left - totalRect.left;
            int rightDelta = frameRect.right - totalRect.right;
            int bottomDelta = frameRect.bottom - totalRect.bottom;
            int topDelta = frameRect.top - totalRect.top;

            TxtDeltaLeft.Text = $"{leftDelta:+0;-0;0}";
            TxtDeltaRight.Text = $"{rightDelta:+0;-0;0}";
            TxtDeltaBottom.Text = $"{bottomDelta:+0;-0;0}";
            TxtDeltaTop.Text = $"{topDelta:+0;-0;0}";

            TxtDeltaLeftNote.Text = leftDelta switch
            {
                < 0 => $"frame {Math.Abs(leftDelta)}px left of total → compensate left by {Math.Abs(leftDelta)}px",
                > 0 => $"frame {leftDelta}px right of total → compensate left by -{leftDelta}px",
                _ => "no offset"
            };
            TxtDeltaRightNote.Text = rightDelta switch
            {
                < 0 => $"frame {Math.Abs(rightDelta)}px left of total → compensate right by -{Math.Abs(rightDelta)}px",
                > 0 => $"frame {rightDelta}px right of total → compensate right by +{rightDelta}px",
                _ => "no offset"
            };
            TxtDeltaBottomNote.Text = bottomDelta switch
            {
                < 0 => $"frame {Math.Abs(bottomDelta)}px above total → compensate bottom by -{Math.Abs(bottomDelta)}px",
                > 0 => $"frame {bottomDelta}px below total → compensate bottom by +{bottomDelta}px",
                _ => "no offset"
            };
            TxtDeltaTopNote.Text = topDelta switch
            {
                < 0 => $"frame {Math.Abs(topDelta)}px above total (not compensated per FancyZones)",
                > 0 => $"frame {topDelta}px below total (not compensated per FancyZones)",
                _ => "no offset (not compensated per FancyZones)"
            };

            AppendLog($"[Delta] left={leftDelta:+0;-0;0} right={rightDelta:+0;-0;0} bottom={bottomDelta:+0;-0;0} top={topDelta:+0;-0;0}");

            int leftComp = totalRect.left - leftDelta;
            int rightComp = totalRect.right - rightDelta;
            int bottomComp = totalRect.bottom - bottomDelta;

            var formulaText =
                $"  adjustedLeft   = {totalRect.left} - ({leftDelta}) = {leftComp}\r\n" +
                $"  adjustedRight  = {totalRect.right} - ({rightDelta}) = {rightComp}\r\n" +
                $"  adjustedBottom = {totalRect.bottom} - ({bottomDelta}) = {bottomComp}\r\n" +
                $"  adjustedTop    = {totalRect.top} (not adjusted, FancyZones skips top)\r\n" +
                $"  adjusted size  = {rightComp - leftComp} x {bottomComp - totalRect.top}";

            TxtFormula.Text = formulaText;
            AppendLog($"[Compensated] left={leftComp} right={rightComp} bottom={bottomComp} top={totalRect.top} size={rightComp - leftComp}x{bottomComp - totalRect.top}");
        }
        else
        {
            TxtFrameLeft.Text = "DWM unavailable";
            TxtFrameTop.Text = "";
            TxtFrameRight.Text = "";
            TxtFrameBottom.Text = "";
            TxtFrameSize.Text = "";

            TxtDeltaLeft.Text = "—";
            TxtDeltaRight.Text = "—";
            TxtDeltaBottom.Text = "—";
            TxtDeltaTop.Text = "—";
            TxtDeltaLeftNote.Text = TxtDeltaRightNote.Text = TxtDeltaBottomNote.Text = TxtDeltaTopNote.Text = "";
            TxtFormula.Text = "ExtendedFrameBounds not available for this window";
            AppendLog("[DWM] ExtendedFrameBounds not available");
        }

        TxtDpi.Text = $"{dpi} ({dpi * 100 / 96d:0}% scaling)";
        AppendLog($"[DPI] {dpi} ({dpi * 100 / 96d:0}% scaling)");
    }

    private static string GetWindowText(nint hWnd)
    {
        var len = GetWindowTextLength(hWnd);
        if (len == 0) return "(empty)";
        var sb = new System.Text.StringBuilder(len + 1);
        GetWindowText(hWnd, sb, len + 1);
        return sb.ToString();
    }

    private static string GetWindowClassName(nint hWnd)
    {
        var sb = new System.Text.StringBuilder(256);
        GetClassName(hWnd, sb, 256);
        return sb.ToString();
    }

    private static int GetWindowDpi(nint hWnd)
    {
        try
        {
            var monitor = MonitorFromWindow(hWnd, 2);
            GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
            return (int)dpiX;
        }
        catch
        {
            return 96;
        }
    }

    #region P/Invoke

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    private enum MONITOR_DPI_TYPE
    {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
        MDT_DEFAULT = 0
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint GetShellWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    #endregion
}

public class WindowItem
{
    public nint Handle { get; set; }
    public string Title { get; set; }
    public string ClassName { get; set; }
    public string Display { get; set; }
}
