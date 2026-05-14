using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;

namespace SnapSample;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT
{
    public uint length;
    public uint flags;
    public uint showCmd;
    public POINT ptMinPosition;
    public POINT ptMaxPosition;
    public RECT rcNormalPosition;
}

public class WindowItem
{
    public nint Handle { get; set; }
    public string Title { get; set; }
    public string ClassName { get; set; }
    public string DisplayText => $"[{Handle}] {Title} ({ClassName})";
}

public partial class MainWindow : Window
{
    private const int SW_SHOWNORMAL = 1;
    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;
    private const int SWP_ASYNCWINDOWPOS = 0x4000;
    private const int SWP_SHOWWINDOW = 0x0040;

    private WindowItem selectedWindow;
    private string logFilePath;
    private readonly object logLock = new object();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => InitializeLogFile();
    }

    private void InitializeLogFile()
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        logFilePath = Path.Combine(logDir, $"SnapSample_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        
        File.WriteAllText(logFilePath, $"=== 日志文件创建于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        AppendLog($"日志文件: {logFilePath}");
        
        RefreshWindowList();
    }

    #region P/Invoke

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    private static readonly nint HWND_TOP = nint.Zero;
    private static readonly nint HWND_TOPMOST = new(-1);

    #endregion

    private delegate void LogAction(string message);

    private void RefreshWindowList()
    {
        var items = new List<WindowItem>();
        var shellWindow = GetShellWindow();

        EnumWindows((hWnd, _) =>
        {
            if (hWnd == shellWindow) return true;
            if (!IsWindowVisible(hWnd)) return true;

            var length = GetWindowTextLength(hWnd);
            if (length == 0) return true;

            var sb = new System.Text.StringBuilder(length + 1);
            GetWindowText(hWnd, sb, length + 1);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            var classSb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, classSb, 256);
            var className = classSb.ToString();

            items.Add(new WindowItem
            {
                Handle = hWnd,
                Title = title,
                ClassName = className
            });
            return true;
        }, nint.Zero);

        WindowList.ItemsSource = items.OrderBy(i => i.Title).ToList();
    }

    [DllImport("user32.dll")]
    private static extern nint GetShellWindow();

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindowList();
    }

    private void WindowList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (WindowList.SelectedItem is WindowItem item)
        {
            selectedWindow = item;
            GetWindowRect(item.Handle, out RECT rect);
            SelectedInfo.Text = $"[{item.Handle}] \"{item.Title}\"  当前位置: ({rect.left},{rect.top})-({rect.right},{rect.bottom}) {rect.right - rect.left}x{rect.bottom - rect.top}";
        }
    }

    private void BtnH_Click(object sender, RoutedEventArgs e)
    {
        selectedWindow = null;
        WindowList.SelectedItem = null;
        SelectedInfo.Text = "未选择窗口";
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
    }

    #region 左区补偿探测

    private void BtnComp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out var offset))
        {
            RunStrategy($"补偿 offset={offset}", (hWnd, tx, ty, tw, th, log) =>
                RunCompensationTest(hWnd, tx, ty, tw, th, offset, log));
        }
    }

    private void BtnCompCustom_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TbCustomOffset.Text, out var offset))
        {
            RunStrategy($"补偿 offset={offset}", (hWnd, tx, ty, tw, th, log) =>
                RunCompensationTest(hWnd, tx, ty, tw, th, offset, log));
        }
        else
        {
            AppendLog("[错误] 自定义偏移量格式无效");
        }
    }

    private async Task RunCompensationTest(nint hWnd, int targetX, int targetY, int targetW, int targetH, int offsetX, LogAction log)
    {
        const int rightMargin = 7;
        const int bottomMargin = 7;

        var compX = targetX + offsetX;
        var compY = targetY;
        var compW = targetW + rightMargin - offsetX;
        var compH = targetH + bottomMargin;
        var compRight = compX + compW;
        var compBottom = compY + compH;

        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");
        log($"补偿参数: leftMargin={offsetX}, rightMargin={rightMargin}, bottomMargin={bottomMargin}");
        log($"目标区域: ({targetX},{targetY})-({targetX + targetW},{targetY + targetH}) sz={targetW}x{targetH}");
        log($"补偿后: ({compX},{compY})-({compRight},{compBottom}) sz={compW}x{compH}");

        SetWindowPos(hWnd, HWND_TOP, targetX, targetY, targetW, targetH, SWP_SHOWWINDOW | SWP_ASYNCWINDOWPOS);
        await Task.Delay(100);

        GetWindowRect(hWnd, out RECT afterPos);
        log($"SetWindowPos 定位后: ({afterPos.left},{afterPos.top})-({afterPos.right},{afterPos.bottom}) sz={afterPos.right - afterPos.left}x{afterPos.bottom - afterPos.top}");

        var placement = new WINDOWPLACEMENT();
        placement.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();
        GetWindowPlacement(hWnd, ref placement);
        placement.showCmd = SW_SHOWNORMAL;
        placement.rcNormalPosition = new RECT { left = compX, top = compY, right = compRight, bottom = compBottom };
        placement.flags = 0x0004;

        var sw = Stopwatch.StartNew();
        var ok1 = SetWindowPlacement(hWnd, ref placement);
        var err1 = Marshal.GetLastWin32Error();
        var ok2 = SetWindowPlacement(hWnd, ref placement);
        var err2 = Marshal.GetLastWin32Error();
        sw.Stop();
        log($"SetWindowPlacement #1 → ok={ok1} win32Err={err1}");
        log($"SetWindowPlacement #2 → ok={ok2} win32Err={err2} 耗时={sw.ElapsedMilliseconds}ms");
        log($"  rcNormalPosition=({compX},{compY})-({compRight},{compBottom})");

        await Task.Delay(250);
        GetWindowRect(hWnd, out RECT after250);
        var w250 = after250.right - after250.left;
        var h250 = after250.bottom - after250.top;
        var match250 = after250.left == compX && after250.top == compY && w250 == compW && h250 == compH;
        log($"+250ms: ({after250.left},{after250.top})-({after250.right},{after250.bottom}) sz={w250}x{h250}  {(match250 ? "✅ MATCH" : "❌ BOUNCE")}");

        if (!match250)
        {
            var deltaX = after250.left - compX;
            var deltaW = w250 - compW;
            log($"❗ 回弹: 左边缘偏移 {deltaX}px, 宽度差异 {deltaW}px");
        }

        await VerifyAsync(hWnd, compX, compY, compW, compH, log);
    }

    #endregion

    #region 策略入口

    private void BtnA1_Click(object sender, RoutedEventArgs e) => RunStrategy("① SetWindowPos 同步", Strategy_SetWindowPosSync);
    private void BtnA2_Click(object sender, RoutedEventArgs e) => RunStrategy("② SetWindowPos 异步(基线)", Strategy_SetWindowPosAsync);
    private void BtnB_Click(object sender, RoutedEventArgs e) => RunStrategy("③ 先SW_RESTORE再同步", Strategy_RestoreThenSync);
    private void BtnC_Click(object sender, RoutedEventArgs e) => RunStrategy("④ MoveWindow API", Strategy_MoveWindow);
    private void BtnD_Click(object sender, RoutedEventArgs e) => RunStrategy("⑤ SetWindowPlacement", Strategy_SetWindowPlacement);
    private void BtnE_Click(object sender, RoutedEventArgs e) => RunStrategy("⑥ 分步调整(先大后小)", Strategy_TwoStep);
    private void BtnF_Click(object sender, RoutedEventArgs e) => RunStrategy("⑦ 三次重试(50ms间隔)", Strategy_Retry);
    private void BtnG_Click(object sender, RoutedEventArgs e) => RunStrategy("⑧ 先最大化再调整", Strategy_MaximizeThenSet);

    #endregion

    private async void RunStrategy(string label, Func<nint, int, int, int, int, LogAction, Task> action)
    {
        if (selectedWindow == null)
        {
            AppendLog("[错误] 请先选择一个目标窗口");
            return;
        }

        if (!IsWindow(selectedWindow.Handle))
        {
            AppendLog("[错误] 目标窗口句柄已失效，请刷新列表重新选择");
            return;
        }

        var targetX = int.Parse(TbTargetX.Text);
        var targetY = int.Parse(TbTargetY.Text);
        var targetW = int.Parse(TbTargetW.Text);
        var targetH = int.Parse(TbTargetH.Text);

        AppendLog(new string('=', 60));
        AppendLog($"[{label}] 开始 — 目标: ({targetX},{targetY}) {targetW}x{targetH}");
        AppendLog($"  窗口: [{selectedWindow.Handle}] \"{selectedWindow.Title}\" ({selectedWindow.ClassName})");

        try
        {
            await action(selectedWindow.Handle, targetX, targetY, targetW, targetH, msg => AppendLog("  " + msg));
        }
        catch (Exception ex)
        {
            AppendLog($"  [异常] {ex.Message}");
        }

        AppendLog($"[{label}] 完成");
    }

    #region 策略 A1: SetWindowPos 同步

    private async Task Strategy_SetWindowPosSync(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        var sw = Stopwatch.StartNew();
        var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_NOZORDER);
        sw.Stop();
        var err = Marshal.GetLastWin32Error();

        log($"SetWindowPos → ok={ok} win32Err={err} 耗时={sw.ElapsedMilliseconds}ms");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 A2: SetWindowPos 异步 (基线)

    private async Task Strategy_SetWindowPosAsync(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        var sw = Stopwatch.StartNew();
        var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_ASYNCWINDOWPOS);
        sw.Stop();
        var err = Marshal.GetLastWin32Error();

        log($"SetWindowPos(ASYNC) → ok={ok} win32Err={err} 耗时={sw.ElapsedMilliseconds}ms");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 B: 先 SW_RESTORE 再同步

    private async Task Strategy_RestoreThenSync(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        ShowWindow(hWnd, SW_RESTORE);
        await Task.Delay(50);
        log($"ShowWindow(SW_RESTORE) 完成");

        var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_NOZORDER);
        var err = Marshal.GetLastWin32Error();
        log($"SetWindowPos → ok={ok} win32Err={err}");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 C: MoveWindow API

    private async Task Strategy_MoveWindow(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        ShowWindow(hWnd, SW_SHOWNORMAL);
        await Task.Delay(20);

        var ok = MoveWindow(hWnd, tx, ty, tw, th, true);
        var err = Marshal.GetLastWin32Error();
        log($"MoveWindow → ok={ok} win32Err={err}");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 D: SetWindowPlacement

    private async Task Strategy_SetWindowPlacement(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        var placement = new WINDOWPLACEMENT();
        placement.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();
        placement.showCmd = SW_SHOWNORMAL;
        placement.rcNormalPosition = new RECT { left = tx, top = ty, right = tx + tw, bottom = ty + th };

        var ok = SetWindowPlacement(hWnd, ref placement);
        var err = Marshal.GetLastWin32Error();

        log($"SetWindowPlacement → ok={ok} win32Err={err}  rcNormalPosition=({tx},{ty})-({tx + tw},{ty + th})");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 E: 分步调整 — 先放到一个更大尺寸，100ms后再调到目标

    private async Task Strategy_TwoStep(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        var stepW = tw + 400;
        var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, stepW, th, SWP_SHOWWINDOW | SWP_NOZORDER);
        var err = Marshal.GetLastWin32Error();
        log($"Step1: SetWindowPos({tx},{ty},{stepW},{th}) → ok={ok} win32Err={err}");

        await Task.Delay(100);

        GetWindowRect(hWnd, out RECT mid);
        log($"Step1 后: ({mid.left},{mid.top})-({mid.right},{mid.bottom}) sz={mid.right - mid.left}x{mid.bottom - mid.top}");

        ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_NOZORDER);
        err = Marshal.GetLastWin32Error();
        log($"Step2: SetWindowPos({tx},{ty},{tw},{th}) → ok={ok} win32Err={err}");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 F: 三次重试 50ms 间隔

    private async Task Strategy_Retry(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        for (int i = 1; i <= 3; i++)
        {
            var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_NOZORDER);
            var err = Marshal.GetLastWin32Error();

            GetWindowRect(hWnd, out RECT after);
            log($"重试#{i}: SetWindowPos → ok={ok} win32Err={err}  结果: ({after.left},{after.top})-({after.right},{after.bottom}) sz={after.right - after.left}x{after.bottom - after.top}");

            if (after.left == tx && after.top == ty && (after.right - after.left) == tw && (after.bottom - after.top) == th)
            {
                log($"重试#{i}: ✅ 命中目标，提前退出");
                await VerifyAsync(hWnd, tx, ty, tw, th, log);
                return;
            }

            await Task.Delay(50);
        }

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 策略 G: 先最大化再调整

    private async Task Strategy_MaximizeThenSet(nint hWnd, int tx, int ty, int tw, int th, LogAction log)
    {
        GetWindowRect(hWnd, out RECT before);
        log($"BEFORE: ({before.left},{before.top})-({before.right},{before.bottom}) sz={before.right - before.left}x{before.bottom - before.top}");

        ShowWindow(hWnd, SW_MAXIMIZE);
        await Task.Delay(100);

        GetWindowRect(hWnd, out RECT maximized);
        log($"最大化后: ({maximized.left},{maximized.top})-({maximized.right},{maximized.bottom}) sz={maximized.right - maximized.left}x{maximized.bottom - maximized.top}");

        ShowWindow(hWnd, SW_RESTORE);
        await Task.Delay(50);

        var ok = SetWindowPos(hWnd, HWND_TOP, tx, ty, tw, th, SWP_SHOWWINDOW | SWP_NOZORDER);
        var err = Marshal.GetLastWin32Error();
        log($"SetWindowPos(恢复后) → ok={ok} win32Err={err}");

        await VerifyAsync(hWnd, tx, ty, tw, th, log);
    }

    #endregion

    #region 轮询验证

    private async Task VerifyAsync(nint hWnd, int expectedX, int expectedY, int expectedW, int expectedH, LogAction log)
    {
        GetWindowRect(hWnd, out RECT imm);
        var immW = imm.right - imm.left;
        var immH = imm.bottom - imm.top;
        var immMatch = imm.left == expectedX && imm.top == expectedY && immW == expectedW && immH == expectedH;
        log($"立即验证: ({imm.left},{imm.top})-({imm.right},{imm.bottom}) sz={immW}x{immH}  {(immMatch ? "✅ MATCH" : "❌ MISMATCH")}");

        var delays = new[] { 200, 500, 1000 };
        foreach (var delay in delays)
        {
            await Task.Delay(delay);

            if (!IsWindow(hWnd))
            {
                log($"+{delay}ms: 窗口句柄已失效");
                return;
            }

            GetWindowRect(hWnd, out RECT cur);
            var curW = cur.right - cur.left;
            var curH = cur.bottom - cur.top;
            var match = cur.left == expectedX && cur.top == expectedY && curW == expectedW && curH == expectedH;

            log($"+{delay}ms: ({cur.left},{cur.top})-({cur.right},{cur.bottom}) sz={curW}x{curH}  {(match ? "✅ MATCH" : "❌ MISMATCH")}");
        }
    }

    #endregion

    private void AppendLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}\n";
            LogBox.AppendText(logMessage);
            LogBox.ScrollToEnd();

            lock (logLock)
            {
                try
                {
                    if (!string.IsNullOrEmpty(logFilePath))
                    {
                        File.AppendAllText(logFilePath, logMessage);
                    }
                }
                catch (Exception ex)
                {
                    LogBox.AppendText($"[日志写入错误] {ex.Message}\n");
                }
            }
        });
    }
}