using SnapIt.Application.Contracts;
using SnapIt.Common;
using SnapIt.Common.Contracts;
using SnapIt.Common.Entities;
using SnapIt.Common.Graphics;
using SnapIt.Controls;
using SnapIt.Services.Contracts;

namespace SnapIt.Application;

public class SnapManager : ISnapManager
{
    private readonly IWindowManager windowManager;
    private readonly ISettingService settingService;
    private readonly IWinApiService winApiService;
    private readonly IScreenManager screenManager;
    private readonly IMouseService mouseService;
    private readonly IKeyboardService keyboardService;
    private readonly IWindowsService windowsService;
    private readonly IWindowEventService windowEventService;
    private readonly ILoggerService logger;

    private SnapLoadingWindow loadingWindow;
    private bool isTrialEnded = false;

    public bool IsInitialized { get; private set; }
    public bool IsRunning { get; set; }

    public event GetStatus StatusChanged;

    public event ScreenChangedEvent ScreenChanged;

    public event LayoutChangedEvent LayoutChanged;

    public event ScreenLayoutLoadedEvent ScreenLayoutLoaded;

    public SnapManager(
        IWindowManager windowManager,
        ISettingService settingService,
        IWinApiService winApiService,
        IScreenManager screenManager,
        IMouseService mouseService,
        IKeyboardService keyboardService,
        IWindowsService windowsService,
        IWindowEventService windowEventService,
        ILoggerService loggerService)
    {
        this.windowManager = windowManager;
        this.settingService = settingService;
        this.winApiService = winApiService;
        this.screenManager = screenManager;
        this.mouseService = mouseService;
        this.keyboardService = keyboardService;
        this.windowsService = windowsService;
        this.windowEventService = windowEventService;
        logger = loggerService;

        keyboardService.SnapStartStop += KeyboardService_SnapStartStop;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        if (isTrialEnded)
            return;

        //globalHook = Hook.GlobalEvents();

        await windowManager.InitializeAsync();
        await screenManager.InitializeAsync();
        await winApiService.InitializeAsync();
        await settingService.InitializeAsync();
        await keyboardService.InitializeAsync();
        await mouseService.InitializeAsync();
        await windowsService.InitializeAsync();
        await windowEventService.InitializeAsync();
        windowEventService.StartMonitoring();

        if (Dev.IsActive && Dev.ShowSnapWindowOnStartup)
        {
            windowManager.Show();
        }

        screenManager.SetSnapManager(this);

        mouseService.MoveWindow += MoveWindow;
        mouseService.SnappingCancelled += SnappingCancelled;

        keyboardService.MoveWindow += MoveWindow;
        keyboardService.SnappingCancelled += SnappingCancelled;
        keyboardService.ChangeLayout += KeyboardService_ChangeLayout;

        IsRunning = true;
        StatusChanged?.Invoke(true);
        ScreenLayoutLoaded?.Invoke(settingService.SnapScreens, settingService.Layouts);

        IsInitialized = true;
    }

    public void StartStop()
    {
        if (IsRunning)
        {
            Dispose();
        }
        else
        {
            _ = InitializeAsync();
        }
    }

    private void MoveWindow(SnapAreaInfo snapAreaInfo, bool isLeftClick)
    {
        MoveWindow(snapAreaInfo.ActiveWindow, snapAreaInfo.Rectangle, isLeftClick);
    }

    private void SnappingCancelled()
    {
        windowManager.Hide();
        mouseService.Interrupt();
    }

    private void KeyboardService_SnapStartStop()
    {
        StartStop();
    }

    private void KeyboardService_ChangeLayout(SnapScreen snapScreen, Layout layout)
    {
        Dispose();
        _ = InitializeAsync();

        LayoutChanged?.Invoke(snapScreen, layout);
    }

    public void SetIsTrialEnded(bool isEnded)
    {
        if (isEnded)
        {
            isTrialEnded = true;
            Dispose();
        }
        else
        {
            isTrialEnded = false;
        }
    }

    public void Dispose()
    {
        windowManager.Dispose();

        mouseService.MoveWindow -= MoveWindow;
        mouseService.SnappingCancelled -= SnappingCancelled;
        mouseService.Dispose();

        keyboardService.MoveWindow -= MoveWindow;
        keyboardService.SnappingCancelled -= SnappingCancelled;
        //keyboardService.SnapStartStop -= KeyboardService_SnapStartStop;
        windowEventService.StopMonitoring();
        keyboardService.ChangeLayout -= KeyboardService_ChangeLayout;
        keyboardService.Dispose();

        keyboardService.SetSnappingStopped();

        IsRunning = false;
        StatusChanged?.Invoke(false);
        IsInitialized = false;
    }

    public void ScreenChangedEvent()
    {
        settingService.ReInitialize();

        if (IsRunning)
        {
            Dispose();
            _ = InitializeAsync();
        }

        ScreenChanged?.Invoke(settingService.SnapScreens);
    }

    private void MoveWindow(ActiveWindow currentWindow, Rectangle rectangle, bool isLeftClick)
    {
        if (currentWindow != ActiveWindow.Empty)
        {
            if (rectangle != null && !rectangle.Equals(Rectangle.Empty))
            {
                var originalRect = new Rectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom, rectangle.Dpi);

                logger.LogInfo($"[SnapManager.MoveWindow] Window=\"{currentWindow.Title}\" Class=\"{currentWindow.ClassName}\" Handle={currentWindow.Handle} isLeftClick={isLeftClick}");
                logger.LogInfo($"[SnapManager.MoveWindow] InputRect=({rectangle.Left},{rectangle.Top})-({rectangle.Right},{rectangle.Bottom}) sz={rectangle.Width}x{rectangle.Height}");
                logger.LogInfo($"[SnapManager.MoveWindow] CurrentBoundry=({currentWindow.Boundry.Left},{currentWindow.Boundry.Top})-({currentWindow.Boundry.Right},{currentWindow.Boundry.Bottom}) sz={currentWindow.Boundry.Width}x{currentWindow.Boundry.Height}");

                winApiService.GetWindowMargin(currentWindow, out Rectangle withMargin);

                if (!withMargin.Equals(Rectangle.Empty))
                {
                    // FancyZones 方式：每边独立计算 Delta，不做 /2 对称假设
                    int leftDelta = (int)(withMargin.Left - currentWindow.Boundry.Left);
                    int rightDelta = (int)(withMargin.Right - currentWindow.Boundry.Right);
                    int bottomDelta = (int)(withMargin.Bottom - currentWindow.Boundry.Bottom);

                    logger.LogInfo($"[SnapManager.MoveWindow] Margin: leftDelta={leftDelta} rightDelta={rightDelta} bottomDelta={bottomDelta}");

                    rectangle.Left -= leftDelta;
                    rectangle.Right -= rightDelta;
                    rectangle.Bottom -= bottomDelta;

                    logger.LogInfo($"[SnapManager.MoveWindow] AfterMarginAdjustment=({rectangle.Left},{rectangle.Top})-({rectangle.Right},{rectangle.Bottom}) sz={rectangle.Width}x{rectangle.Height}");
                }
                else
                {
                    logger.LogInfo($"[SnapManager.MoveWindow] No margin adjustment needed (ExtendedFrameBounds empty)");
                }

                if (isLeftClick)
                {
                    logger.LogInfo($"[SnapManager.MoveWindow] Left-click mode: dispatching to background thread with 100ms delay");
                    new Thread(() =>
                    {
                        Thread.Sleep(100);

                        winApiService.MoveWindow(currentWindow, rectangle);

                        if (!rectangle.Dpi.Equals(currentWindow?.Dpi))
                        {
                            logger.LogInfo($"[SnapManager.MoveWindow] DPI mismatch detected: rectDpi={rectangle.Dpi} windowDpi={currentWindow?.Dpi}, retrying");
                            winApiService.MoveWindow(currentWindow, rectangle);
                        }
                    }).Start();
                }
                else
                {
                    logger.LogInfo($"[SnapManager.MoveWindow] Keyboard mode: dispatching immediately");
                    winApiService.MoveWindow(currentWindow, rectangle);

                    if (!rectangle.Dpi.Equals(currentWindow?.Dpi))
                    {
                        logger.LogInfo($"[SnapManager.MoveWindow] DPI mismatch detected: rectDpi={rectangle.Dpi} windowDpi={currentWindow?.Dpi}, retrying");
                        winApiService.MoveWindow(currentWindow, rectangle);
                    }
                }

                Telemetry.TrackEvent("MoveActiveWindow - Mouse");
            }
            else
            {
                logger.LogWarn($"[SnapManager.MoveWindow] Rectangle is null or empty for window \"{currentWindow.Title}\"");
            }
        }
        else
        {
            logger.LogWarn($"[SnapManager.MoveWindow] currentWindow is ActiveWindow.Empty");
        }
    }
}