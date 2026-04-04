# SnapIt-Plus Overlay 功能增强方案设计文档

---

## 一、现有架构深度分析

### 1.1 Overlay 数据流概览

```
用户操作（Add Overlay / 拖拽 / 拉伸）
    ↓
SnapOverlayEditor（设计模式下的可视化编辑器）
    ├── FullOverlay：完整覆盖区域（可拖拽、8方向拉伸）
    ├── MiniOverlay：缩小预览图（可在FullOverlay内部拖拽定位）
    └── DesignPanel：悬浮工具栏（删除/切换/坐标输入）
    ↓
SnapControl.GenerateSnapOverlays()
    ↓
收集所有 SnapOverlayEditor → 转换为 LayoutOverlay 列表
    ↓
Layout.LayoutOverlays 持久化
```

### 1.2 核心类与职责

| 组件 | 文件 | 职责 |
|------|------|------|
| [`LayoutOverlay`](SnapIt.Common/Entities/LayoutOverlay.cs) | 数据模型 | 持有 `Point`（位置）、`Size`（尺寸）、`MiniOverlay`（嵌套子overlay） |
| [`SnapOverlayEditor`](SnapIt.Controls/SnapOverlayEditor.xaml.cs) | 控件 | 设计模式的 overlay 编辑器，支持拖拽和8方向拉伸 |
| [`SnapControl.AddOverlay()`](SnapIt.Controls/SnapControl.xaml.cs#L300-L323) | 工厂方法 | 创建默认尺寸(40%缩放)的 overlay，偏移量递增 |
| [`SnapControl.RemoveOverlay()`](SnapIt.Controls/SnapControl.xaml.cs#L325-L337) | 删除方法 | 从 UI 和数据模型中移除 |
| [`SnapArea`](SnapIt.Controls/SnapArea.cs) | 区域控件 | 布局分割后的每个矩形区域，持有 `AreaNumber`、`ScreenSnapArea()` |

### 1.3 当前拖拽与拉伸逻辑分析

#### 拖拽移动（[`OnMouseMove()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L265-L388)，`ResizeHitType.Body`）

```csharp
// 第299-311行：直接累加偏移量，无任何边界检查
case ResizeHitType.Body:
    new_x += offset_x;
    new_y += offset_y;
    break;

// 第357-386行：仅检查 width>0 && height>0，不限制位置范围
if (new_width > 0 && new_height > 0)
{
    SetPos(point, size);  // 直接设置，无裁剪
}
```

**问题根因**：`SetPos()` 只做赋值，不与父容器 (`MainOverlay`) 的边界做交集运算。

#### 拉伸调整大小（`ResizeHitType.UL/UR/LR/LL/L/R/T/B`）

```csharp
// 同样只检查 new_width > 0 && new_height > 0
// 无吸附(Snap)逻辑，无对齐到网格/边框线的机制
```

**问题根因**：拉伸时坐标是连续浮点值，没有任何"靠近某条线就自动对齐"的逻辑。

#### 创建位置（[`AddOverlay()`](SnapIt.Controls/SnapControl.xaml.cs#L300-L323)）

```csharp
public void AddOverlay()
{
    var scale = 0.4;  // 固定40%缩放
    var size = new Size { Width = ActualWidth * scale, Height = ActualHeight * scale };
    // overlayMargin 递增20px，无上限
    overlayMargin += 20;
    // ...
}
```

**问题根因**：
- 固定比例创建，不考虑当前布局的实际区域分布
- `overlayMargin` 无限递增，必然导致后续 overlay 超出容器

### 1.4 关键坐标系关系

```
SnapControl (容器)
├── MainGrid (布局网格)
│   ├── MainAreas (区域层) ← SnapAreaEditor / SnapArea 在这里
│   ├── MainFullOverlay (全屏覆盖层)
│   ├── MainOverlay (Overlay层) ← SnapOverlayEditor 在这里
│   │   └── SnapOverlayEditor (HorizontalAlignment=Left, VerticalAlignment=Top)
│   │       ├── Margin.Left, Margin.Top → 相对于 MainOverlay 的位置
│   │       ├── Width, Height → 尺寸
│   │       └── MiniOverlay (内部嵌套)
│   └── 边界线 (topBorder/bottomBorder/leftBorder/rightBorder + 用户添加的线)
```

**关键发现**：`SnapOverlayEditor` 使用 `HorizontalAlignment=Left, VerticalAlignment=Top` + `Margin` 定位，这意味着它的坐标空间就是 `MainOverlay` 的内部空间。**约束它不出界只需 Clamp 到 `[0, MainOverlay.ActualWidth-Width]` 和 `[0, MainOverlay.ActualHeight-Height]`。**

---

## 二、问题一：Overlay 拖拽越界

### 2.1 问题复现

```
MainOverlay 容器边界:
┌──────────────────────────────┐
│                              │
│   ┌────────┐                 │
│   │overlay │─── 拖拽 ────→   │  ← 可以拖出去！
│   │  OK    │                 │
│   └────────┘         ❌      │
│                  (超出边界)   │
└──────────────────────────────┘
```

### 2.2 方案：坐标钳制（Clamp）⭐

#### 思路
在 [`OnMouseMove()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L357-L386) 中，计算出新位置后，将其钳制到父容器范围内。

#### 实现

在 `SnapOverlayEditor.OnMouseMove()` 的 `if (new_width > 0 && new_height > 0)` 块内，`SetPos()` 调用前加入：

```csharp
// 获取父容器（MainOverlay）的有效范围
var parentWidth = SnapControl.MainOverlay.ActualWidth;
var parentHeight = SnapControl.MainOverlay.ActualHeight;

// 钳制位置：不允许负数，不允许超出右/下边界
new_x = Math.Max(0, Math.Min(new_x, parentWidth - new_width));
new_y = Math.Max(0, Math.Min(new_y, parentHeight - new_height));

// 钳制尺寸：拉伸时也不允许超出容器
new_width = Math.Min(new_width, parentWidth - new_x);
new_height = Math.Min(new_height, parentHeight - new_y);
```

#### 影响范围

此修改同时解决：
- ✅ **Body 拖拽越界** — 移动时被限制在容器内
- ✅ **拉伸越界** — 8方向拉伸时也不会超出容器边界
- ✅ **AddOverlay 累积偏移越界** — 即使初始位置因 overlayMargin 过大而越界，用户第一次拖拽就会修正

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 改动极小（~6行代码） | 不阻止初始创建时的越界（需配合问题三优化 AddOverlay） |
| 同时修复拖拽+拉伸两种场景 | |
| 性能无影响（只是 Math.Max/Min） | |

---

## 三、问题二：拉伸吸附（Snap to Grid/Lines）

### 3.1 问题描述

用户手动拉伸 overlay 边缘时，希望当边缘 **接近** 某条分割线或区域边界时，能 **自动吸附** 到该线上。

```
当前行为:                    期望行为:
┌──────────┬────┐           ┌──────────┬────┐
│          │    │           │          │    │
│ overlay  │    │   拉伸    │ overlay  │    │
│     ├────┤    │  ────→   │ ─────────┼────┘  ← 吸附！
│     │❌差│    │           │    ✅完美对齐    │
│     │2px │    │           │                 │
└──────────┴────┘           └─────────┴───────┘
            ↑                         ↑
     与分割线差2px               自动对齐到分割线
```

### 3.2 吸附目标定义

Overlay 边缘可以吸附到以下目标：

| 目标类型 | 来源 | 说明 |
|---------|------|------|
| **容器边界** | MainGrid 的四条边 | Top=0, Bottom=ActualHeight, Left=0, Right=ActualWidth |
| **垂直分割线** | 所有 `SplitDirection=Vertical` 的 SnapBorder 的 X 坐标 | 即每条竖线的 ReferenceBorder.Margin.Left 位置 |
| **水平分割线** | 所有 `SplitDirection=Horizontal` 的 SnapBorder 的 Y 坐标 | 即每条横线的 ReferenceBorder.Margin.Top 位置 |
| **区域边界** | 每个 SnapArea/SnapAreaEditor 的四条边 | 可从 Margin + Width/Height 计算 |

### 3.3 方案：吸附引擎 ⭐

#### 核心算法

```csharp
public class SnapEngine
{
    private const double SNAP_THRESHOLD = 8;  // 吸附阈值（像素）
    private List<double> snapLinesX;          // 所有可吸附的X坐标
    private List<double> snapLinesY;          // 所有可吸附的Y坐标

    public void BuildSnapLines(SnapControl snapControl)
    {
        snapLinesX = [0, snapControl.MainGrid.ActualWidth];  // 容器左右边界
        snapLinesY = [0, snapControl.MainGrid.ActualHeight]; // 容器上下边界

        // 收集所有分割线坐标
        foreach (var border in snapControl.FindChildren<SnapBorder>().Where(b => b.IsDraggable))
        {
            if (border.SplitDirection == SplitDirection.Vertical)
                snapLinesX.Add(border.Margin.Left + SnapBorder.THICKNESSHALF);
            else
                snapLinesY.Add(border.Margin.Top + SnapBorder.THICKNESSHALF);
        }

        // 收集所有区域的边界
        foreach (var area in snapControl.FindChildren<SnapAreaEditor>())
        {
            var left = area.Margin.Left;
            var right = left + area.Width;
            var top = area.Margin.Top;
            var bottom = top + area.Height;

            snapLinesX.Add(left);
            snapLinesX.Add(right);
            snapLinesY.Add(top);
            snapLinesY.Add(bottom);
        }
    }

    public double SnapValue(double value, List<double> snapLines)
    {
        foreach (var line in snapLines)
        {
            if (Math.Abs(value - line) < SNAP_THRESHOLD)
                return line;  // 吸附！
        }
        return value;  // 不吸附，返回原值
    }

    public (double x, double y, double w, double h) SnapRect(double x, double y, double w, double h)
    {
        // 左边缘吸附
        var snappedLeft = SnapValue(x, snapLinesX);
        // 上边缘吸附
        var snappedTop = SnapValue(y, snapLinesY);
        // 右边缘吸附
        var snappedRight = SnapValue(x + w, snapLinesX);
        // 下边缘吸附
        var snappedBottom = SnapValue(y + h, snapLinesY);

        return (
            snappedLeft,
            snappedTop,
            snappedRight - snappedLeft,
            snappedBottom - snappedTop
        );
    }
}
```

#### 集成点

在 [`SnapOverlayEditor.OnMouseMove()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L357-L386) 中，`SetPos()` 调用之前：

```csharp
if (new_width > 0 && new_height > 0)
{
    // 应用吸附
    if (_mouseHitType != ResizeHitType.Body)
    {
        // 只有拉伸时才吸附（拖拽整体移动时不吸附，保持流畅）
        var engine = new SnapEngine();
        engine.BuildSnapLines(SnapControl);
        (new_x, new_y, new_width, new_height) = engine.SnapRect(new_x, new_y, new_width, new_height);
    }

    SetPos(new Point(new_x, new_y), new Size(new_width, new_height));
    // ...
}
```

#### 视觉反馈（可选增强）

当吸附发生时，可以给用户视觉提示：

```
方案A：光标变化 — 吸附瞬间光标闪烁或变化
方案B：边缘高亮 — 被吸附到的线短暂高亮
方案C：数值显示 — PositionGrid 中被吸附的数值变色/加粗
```

推荐先实现核心吸附逻辑，视觉反馈作为迭代增强。

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 大幅提升布局精度和操作效率 | 需要新增一个 SnapEngine 类（~60行） |
| 与专业设计工具（Figma、VS）体验一致 | 每次MouseMove都需遍历线段列表（但通常<50条，性能无忧） |
| SNAP_THRESHOLD 可配置，适应不同DPI | |

---

## 四、问题三：Add Overlay by Area（按区域选择创建）

### 4.1 需求理解

除了现有的「手动拖拽创建 overlay」方式外，新增一种 **「选择已有区域 → 自动生成 overlay」** 的方式：

```
步骤1: 布局状态                    步骤2: 点击 "Add by Area"
┌────┬────┬────┬────┐              进入区域选择模式
│    │    │    │    │
│ 1  │ 2  │ 3  │ 4  │             每个区域变为可点击:
│    │    │    │    │              [1] [2] [3] [4]
└────┴────┴────┴────┘
                                 步骤3: 选择区域 2,3
                                [ ] [✓] [✓] [ ]
                                        ↓
                              自动创建覆盖区域2+3的Overlay
                                   ┌───────┐
                                   │ 2 | 3 │
                                   └───────┘
```

**特殊情况 — 选择不连续区域（对角选择）：**

```
选择区域 1 和 4（对角）:

┌────┬────┬────┬────┐
│ ██ │    │    │    │  ← 选中的区域高亮
│ 1  │    │    │ 4  │
│ ██ │    │    │ ██ │
├────┼────┼────┤    │
│    │    │    │    │
│    │    │    │ ██ │
└────┴────┴────┴────┘

生成的 Overlay = 包裹所有选中区域的 最小外接矩形 (Bounding Box)

      ┌──────────────┐
      │              │
      │   [1]   [4]  │   ← 从区域1左上角 到 区域4右下角
      │              │
      └──────────────┘
```

### 4.2 方案设计

#### 4.2.1 交互流程

```
┌─────────────────────────────────────────────────┐
│  Design Panel (工具栏)                           │
│  [Add Overlay] [Add by Area ✓] [其他按钮...]    │  ← 新增按钮
└─────────────────────────────────────────────────┘

点击 "Add by Area" 后进入区域选择模式:
  1. 所有 SnapAreaEditor 变为可选择状态（高亮边框 + 编号显示）
  2. 点击区域 → 切换选中/取消（支持多选）
  3. 底部出现确认栏: [Create Overlay] [Cancel]
  4. 点击 Create → 计算选中区域的 BoundingBox → 创建 Overlay
  5. 退出选择模式
```

#### 4.2.2 核心算法：多区域 BoundingBox 计算

```csharp
public static Rect CalculateBoundingBox(IEnumerable<SnapAreaEditor> selectedAreas)
{
    if (!selectedAreas.Any()) return Rect.Empty;

    double minX = double.MaxValue, minY = double.MaxValue;
    double maxX = double.MinValue, maxY = double.MinValue;

    foreach (var area in selectedAreas)
    {
        minX = Math.Min(minX, area.Margin.Left);
        minY = Math.Min(minY, area.Margin.Top);
        maxX = Math.Max(maxX, area.Margin.Left + area.Width);
        maxY = Math.Max(maxy, area.Margin.Top + area.Height);
    }

    return new Rect(new Point(minX, minY), new Point(maxX, maxY));
}
```

**效果验证：**

```
场景1: 连续选择 2,3
  Area2: Margin=(200,0), W=200, H=300
  Area3: Margin=(400,0), W=200, H=300
  → BoundingBox = (200,0, 400,300)  ✅ 正确覆盖2+3

场景2: 对角选择 1,4
  Area1: Margin=(0,0),   W=200, H=300
  Area4: Margin=(600,0), W=200, H=300
  → BoundingBox = (0,0, 800,300)  ✅ 包裹全部4个区域的宽

场景3: 非规则选择 1,3
  Area1: Margin=(0,0),   W=200, H=300
  Area3: Margin=(400,0), W=200, H=300
  → BoundingBox = (0,0, 600,300)  ✅ 也包含区域2（这是预期行为）
```

#### 4.2.3 状态管理

需要在 `SnapControl` 上新增选择模式状态：

```csharp
public bool IsAreaSelectionMode { get; set; }  // 是否处于区域选择模式
public HashSet<SnapAreaEditor> SelectedAreas { get; set; }  // 已选区域集合
```

#### 4.2.4 UI 改动点

| 文件 | 改动 |
|------|------|
| **新增** `SnapControl.cs` | `IsAreaSelectionMode` DP、`SelectedAreas` 属性、`EnterAreaSelectionMode()`、`ExitAreaSelectionMode()`、`CreateOverlayFromAreas()` 方法 |
| **新增** 或扩展工具栏 XAML | 「Add by Area」按钮 |
| [`SnapAreaEditor`](SnapIt.Controls/SnapAreaEditor.cs) | 新增 `IsSelected` DP、点击切换选中的逻辑、选中态视觉样式 |
| [`SnapAreaEditor.xaml`](SnapIt.Controls/Styles/SnapAreaEditor.xaml) | 选中态 Trigger（边框高亮/填充色变化） |

#### 4.2.5 选中态视觉效果

```xml
<!-- SnapAreaEditor Style 中新增 Trigger -->
<Trigger Property="IsSelected" Value="True">
    <Setter TargetName="Area" Property="Background" Value="{Binding Theme.HighlightBrush, ...}" />
    <Setter TargetName="Area" Property="Opacity" Value="0.6" />
</Trigger>
```

```
正常状态:                     选中状态:
┌──────────────┐              ╔══════════════╗
│              │              ║  2  ✓       ║  ← 高亮 + 半透明
│              │              ║              ║
│              │              ╚══════════════╝
└──────────────┘
```

#### 4.2.6 确认栏 UI

区域选择模式下，在 `SnapControl` 底部显示浮动确认栏：

```
┌──────────────────────────────────────────────────┐
│  已选择 2 个区域          [✓ Create]  [✕ Cancel]  │
└──────────────────────────────────────────────────┘
```

---

## 五、三个问题的关联与依赖关系

```
问题一（越界限制）     问题二（吸附）          问题三（按区域创建）
     │                   │                       │
     ▼                   ▼                       ▼
┌─────────────┐   ┌─────────────┐       ┌─────────────────┐
│ Clamp坐标   │   │ SnapEngine  │       │ AreaSelection   │
│ ~6行代码    │   │ ~60行代码   │       │ Mode + BBox     │
└──────┬──────┘   └──────┬──────┘       └────────┬────────┘
       │                  │                       │
       └──────────────────┼───────────────────────┘
                          │
                          ▼
               都作用于 SnapOverlayEditor
               的 OnMouseMove() / SetPos()
```

**建议实施顺序**：问题一 → 问题二 → 问题三
- 问题一是基础防护，改动最小，应最先做
- 问题二是体验增强，依赖问题一的坐标体系
- 问题三是新功能模块，相对独立但最复杂

---

## 六、实施路线图

```
Phase 1 (基础防护)              Phase 2 (体验增强)              Phase 3 (新功能)
─────────────────              ────────────────               ─────────────
✅ OnMouseMove 加 Clamp        ✅ 新建 SnapEngine 类           ✅ 新增 IsAreaSelectionMode
✅ 同时修复拖拽+拉伸越界        ✅ 集成到拉伸路径               ✅ SnapAreaEditor 选中态
                               ✅ 可选: 吸附视觉反馈          ✅ BoundingBox 算法
                                                          ✅ 确认栏 UI
                                                          ✅ "Add by Area" 按钮
```

---

## 七、附录：相关代码索引

| 功能 | 文件 | 关键行号 |
|------|------|---------|
| Overlay 创建 | [`SnapControl.AddOverlay()`](SnapIt.Controls/SnapControl.xaml.cs#L300-L323) | L300-L323 |
| Overlay 删除 | [`SnapControl.RemoveOverlay()`](SnapIt.Controls/SnapControl.xaml.cs#L325-L337) | L325-L337 |
| Overlay 拖拽 | [`SnapOverlayEditor.OnMouseMove()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L265-L388) | L265-L388 |
| Overlay 拉伸Hit检测 | [`SetHitType()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L390-L418) | L390-L418 |
| Overlay 位置设置 | [`SetPos()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L191-L215) | L191-L215 |
| Overlay 数据序列化 | [`GetOverlay()`](SnapIt.Controls/SnapOverlayEditor.xaml.cs#L151-L163) | L151-L163 |
| Overlay XAML模板 | [`SnapOverlayEditor.xaml`](SnapIt.Controls/SnapOverlayEditor.xaml) | 全文 |
| LayoutOverlay数据模型 | [`LayoutOverlay.cs`](SnapIt.Common/Entities/LayoutOverlay.cs) | 全文 |
| 区域生成（获取所有Rectangle） | [`GenerateSnapAreas()`](SnapIt.Controls/SnapControl.xaml.cs#L615-L685) | L615-L685 |
| 区域控件 | [`SnapArea.cs`](SnapIt.Controls/SnapArea.cs) | 全文 |
| 区域编辑器控件 | [`SnapAreaEditor.cs`](SnapIt.Controls/SnapAreaEditor.cs) | 全文 |
| 分割线集合 | [`SnapBorder`](SnapIt.Controls/SnapBorder.xaml.cs) | 全文 |
| MainOverlay 容器 | `SnapControl.MainOverlay` | XAML中定义 |
