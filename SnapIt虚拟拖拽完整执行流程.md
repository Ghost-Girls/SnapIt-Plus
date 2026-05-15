# SnapIt 虚拟拖拽 → 窗口放置完整执行流程

## 概览

```
用户按下鼠标键 → 移动鼠标 → 松开鼠标键 → 窗口被放置到目标区域
    ↓                ↓              ↓                  ↓
 SharpHook       SharpHook      SharpHook          SetWindowPos
 MousePressed    MouseDragged    MouseReleased      (两步法)
```

---

## 阶段 1: 初始化 — 注册全局钩子

**入口:** [SnapManager.InitializeAsync()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/SnapManager.cs#L61-L101)

```
SnapManager.InitializeAsync()
  ├─ mouseService.InitializeAsync()
  │    └─ 注册 SharpHook 全局钩子事件
  ├─ mouseService.MoveWindow += MoveWindow      (SnapManager 订阅)
  └─ mouseService.SnappingCancelled += ...
```

**SharpHook 事件注册:** [MouseService.InitializeAsync()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/MouseService.cs#L48-L78)

```csharp
hook.MousePressed  += MouseDownEvent;   // 鼠标按下 → 开始拖拽
hook.MouseDragged  += MouseMoveEvent;   // 鼠标拖动 → 检测窗口和区域
hook.MouseReleased += MouseUpEvent;     // 鼠标松开 → 执行放置
hook.KeyPressed    += Esc_KeyDown;     // ESC → 取消
```

---

## 阶段 2: 鼠标按下 — 开始虚拟拖拽

**入口:** [MouseDownEvent()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/MouseService.cs#L165-L177)

```
用户按下鼠标键 (左键/右键)
  ├─ hook.MouseDragged += MouseMoveEvent   ← 开始监听拖动
  ├─ activeWindow = ActiveWindow.Empty      ← 重置窗口引用
  ├─ isListening = true                    ← 标记开始监听
  └─ startLocation = 当前鼠标位置           ← 记录起始位置
```

---

## 阶段 3: 鼠标拖动 — 检测窗口 + 定位目标区域

**入口:** [MouseMoveEvent()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/MouseService.cs#L103-L163)

这一步在鼠标拖动过程中**每帧**都被调用。核心逻辑：

```
MouseMoveEvent(e)
  ├─ 检查鼠标拖动距离 > 设置的阈值 (IsDelayDone)
  │
  ├─ 第一次检测 (isWindowDetected == false)
  │   └─ activeWindow = winApiService.GetActiveWindow()
  │       ├─ GetForegroundWindow() → 获取前台窗口句柄
  │       ├─ GetWindowText() → 获取窗口标题
  │       ├─ GetWindowRect() → 获取窗口矩形 (含 DWM 阴影的物理边界)
  │       └─ 返回 ActiveWindow{ Handle, Title, Boundry }
  │
  │   └─ 检查排除条件:
  │       ├─ 标题在排除列表中 → 取消
  │       ├─ 全屏窗口 (IsFullscreen) → 取消
  │       ├─ 模态窗口 (IsAllowedWindowStyle) → 取消
  │       └─ 标题栏拖拽模式 → 验证鼠标在标题栏上
  │
  ├─ 第二次及后续 (isWindowDetected == true)
  │   └─ snapAreaInfo = SelectElementWithPoint(mouseX, mouseY)
  │       └─ WindowManager → SnapWindow → InputHitTest
  │           遍历所有 SnapWindow，查找鼠标指针下方的 UI 元素
  │
  └─ 返回 snapAreaInfo { ActiveWindow, Rectangle, Screen }
```

### 3.1 区域检测细节

**入口:** [WindowManager.MouseService_SelectElementWithPoint()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/WindowManager.cs#L149-L166)

```
遍历所有 SnapWindow (每个屏幕一个)
  └─ snapWindow.SelectElementWithPoint(x, y)
       ├─ PointFromScreen() → 屏幕坐标转窗口坐标
       ├─ InputHitTest() → WPF 命中测试，找到鼠标下方的控件
       └─ 找到 SnapArea 或 SnapOverlay
            └─ screenSnapArea(Dpi) → 转换为屏幕坐标的 Rectangle
```

### 3.2 全屏覆盖 (SnapOverlay) 坐标转换

**入口:** [SnapFullOverlay.ScreenSnapArea()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Controls/SnapFullOverlay.cs#L79-L91)

```
SnapWindow 覆盖整个屏幕 (0,0)-(3840,2130) 的透明窗口
  └─ FullScreen.json 中定义:
       {
         "Point": "0,0",        ← 覆盖区起始坐标 (在 SnapWindow 内)
         "Size": "3840,2130"    ← 覆盖区大小
       }
  └─ ScreenSnapArea() 输出:
       topLeft = SnapWindow.PointToScreen(0, 0) = (0, 0)
       bottomRight = SnapWindow.PointToScreen(3840, 2130) = (3840, 2130)
       → InputRect = (0,0)-(3840,2130)  ← 传递给 MoveWindow
```

---

## 阶段 4: 鼠标松开 — 执行窗口放置

**入口:** [MouseUpEvent()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/MouseService.cs#L180-L195)

```
MouseUpEvent(e)
  ├─ hook.MouseDragged -= MouseMoveEvent   ← 停止监听拖动
  ├─ isListening = false
  ├─ HideWindows?.Invoke()                 ← 隐藏 SnapWindow 覆盖层
  └─ MoveWindow?.Invoke(snapAreaInfo, isLeftClick)
       └─ 事件传递到 SnapManager.MoveWindow()
```

---

## 阶段 5: SnapManager — Margin 计算 + Delta 补偿

**入口:** [SnapManager.MoveWindow()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/SnapManager.cs#L188-L266)

这是 **回弹问题的核心发生区域**。

### 5.1 Margin 计算

```csharp
// 获取 DWM 视觉边界 (不含阴影)
winApiService.GetWindowMargin(currentWindow, out Rectangle withMargin);
// GetWindowMargin 调用 DwmGetWindowAttribute(ExtendedFrameBounds)
// 返回视觉矩形 (DWM frame)，不含阴影/边框

// 对称假设: 左阴影 = 右阴影 = (总宽度 - 视觉宽度) / 2
var marginHorizontal = (currentWindow.Boundry.Width - withMargin.Width) / 2;

// 底部阴影 = 总高度 - 视觉高度 (跳过顶部)
var systemMargin = new Rectangle {
    Left   = marginHorizontal,
    Right  = marginHorizontal,
    Top    = 0,
    Bottom = currentWindow.Boundry.Height - withMargin.Height
};
```

### 5.2 Delta 补偿

```csharp
// 目标矩形加上 DWM 阴影补偿
rectangle.Left   -= systemMargin.Left;    // 向左扩展 (阴影)
rectangle.Top    -= systemMargin.Top;     // 向上 (通常为 0)
rectangle.Right  += systemMargin.Right;   // 向右扩展 (阴影)
rectangle.Bottom += systemMargin.Bottom;  // 向下扩展 (阴影+边框)
```

**以全屏日志为例：**

| 参数 | 值 | 说明 |
|------|-----|------|
| InputRect | `(0,0)-(3840,2130)` | 覆盖区坐标（不含阴影） |
| DWM frame | `(0,0)-(3840,2130)` | 窗口视觉边界 |
| Current Boundry | `(-7,0)-(3847,2137)` | 窗口物理边界（含7px阴影） |
| marginHorizontal | `(3854-3840)/2 = 7` | 左/右阴影宽度 |
| systemMargin.Bottom | `2137-2130 = 7` | 底部阴影高度 |
| AfterMarginAdjustment | `(-7,0)-(3847,2137)` | **X=-7 为负值！** |

### 5.3 调用路径

```
isLeftClick (鼠标左键)  →  新线程 + Sleep(100) + MoveWindow
isRightClick (鼠标右键) →  直接调用 MoveWindow ("Keyboard mode")
```

**右键路径（全屏回弹发生的路径）：**

```csharp
winApiService.MoveWindow(currentWindow, rectangle);
// 如果 DPI 不匹配，再调用一次
```

---

## 阶段 6: WinApiService — 两步法 SetWindowPos

**入口:** [MoveWindow()](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/WinApiService.cs#L116-L163)

```csharp
// 第一步: ShowWindow — 确保窗口处于正常状态
ShowWindow(handle, SW_SHOWNORMAL);

// 第二步: 仅移动位置 (不改变大小)
SetWindowPos(handle, HWND_TOP,
    targetX, targetY, 0, 0,
    SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);

// 第三步: 仅调整大小 (不改变位置)
SetWindowPos(handle, HWND_TOP,
    0, 0, targetWidth, targetHeight,
    SWP_NOMOVE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
```

### 6.1 异步验证

```csharp
VerifyPositionAsync(handle, X, Y, width, height);
// 200ms, 500ms, 1000ms 三次检查
// 通过 GetWindowRect 验证窗口是否在预期位置
```

---

## 全屏回弹问题根因分析

### 日志证据

```
行 53: BEFORE: left=-7,top=0,right=3847,bottom=2137   ← 已经在目标位置
行 54: TARGET: X=-7,Y=0 W=3854xH=2137                  ← 目标与当前位置一致
行 55: AFTER:  left=-725,top=-557,right=3129,bottom=1580  ← 却跳到了别处！
```

### 关键发现

**`Before == Target` 但执行后 `After != Target`。**

这说明问题不在于"目标坐标错了"，而在于即使**目标完全正确**，执行 `ShowWindow(SW_SHOWNORMAL) + SetWindowPos` 的**过程本身**也会触发窗口位移。

### 根因推测

`ShowWindow(SW_SHOWNORMAL)` 对有 DWM 阴影的窗口产生副作用。具体来说：

1. 窗口当前通过 Windows 原生 Snap（Win+方向键）或先前的 SnapIt 放置，位置在 `(-7,0)-(3847,2137)`，即全屏 + 7px DWM 阴影扩展
2. `ShowWindow(SW_SHOWNORMAL)` 被调用时，Windows 检查该窗口的 `rcNormalPosition`（还原位置），发现它记录的"正常位置"可能已经被之前的操作污染
3. Windows 内部执行了一次隐式的窗口还原操作，把窗口移到了某个"内部记录的"位置
4. 然后 `SetWindowPos` 虽然尝试移回 `(-7,0)`，但中间的竞态导致了 `(-725,-557)` 这个半成品结果

### 与左边区域回弹的对比

| | 左边放置 (-7,-2160) | 全屏放置 (-7,0) |
|---|---|---|
| 窗口原来在目标位置？ | **否**（从别处拖来） | **是**（已经在全屏） |
| ShowWindow 副作用 | 窗口从正常状态切到正常状态（无影响） | 窗口可能从某种"优化状态"复原 |
| 两步法是否生效 | ✅ 有效 | ❌ 无效 |

### 求解方向

问题的本质可能是：**当窗口已经被放置在某个位置且处于稳定状态后，再对它执行 `ShowWindow(SW_SHOWNORMAL)` 反而会破坏稳定状态**。

可能的解决方向：
1. **跳过 ShowWindow**: 如果窗口不需要从最小化/最大化恢复，不调用 `SW_SHOWNORMAL`
2. **先检查位置是否已匹配**: 如果 `GetWindowRect == Target`，直接跳过整个放置流程
3. **使用 SWP_NOZORDER 代替 HWND_TOP**: 避免 Z 序变化触发不必要的窗口状态变更

---

## 完整调用链总结

```
用户右键拖拽窗口
  └─ SharpHook MousePressed → MouseDownEvent
       └─ isListening = true

每帧: SharpHook MouseDragged → MouseMoveEvent
  ├─ GetActiveWindow()            → ActiveWindow{ Handle, Title, Boundry }
  └─ SelectElementWithPoint(x,y)  → 查找 SnapOverlay/SnapArea
       └─ SnapFullOverlay.ScreenSnapArea(Dpi) → Rectangle(0,0,3840,2130)

用户松开右键
  └─ SharpHook MouseReleased → MouseUpEvent
       └─ MoveWindow
            └─ SnapManager.MoveWindow()
                 ├─ GetWindowMargin() → DWM ExtendedFrameBounds
                 ├─ Delta 补偿 → AfterMarginAdjustment(-7,0,3847,2137)
                 └─ winApiService.MoveWindow(X=-7, Y=0, W=3854, H=2137)
                      ├─ ShowWindow(SW_SHOWNORMAL)        ← ⚠️ 可能触发问题
                      ├─ SetWindowPos(SWP_NOSIZE)          → 仅移动
                      ├─ SetWindowPos(SWP_NOMOVE)          → 仅调整大小
                      └─ VerifyPositionAsync(200/500/1000ms)

结果: AFTER = (-725,-557,3129,1580)  ← 回弹！
```