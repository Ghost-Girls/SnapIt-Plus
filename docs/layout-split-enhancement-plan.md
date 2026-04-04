# SnapIt-Plus 布局分割增强方案设计文档

---

## 一、现有架构深度分析

### 1.1 核心数据流

```
用户操作(SnapAreaEditor.Split)
    ↓
创建 SnapBorder（视觉分割线）→ 加入 MainGrid
    ↓
SnapControl.GenerateSnapAreas()
    ↓
收集所有 SnapBorder → 转换为 Segment 列表
    ↓
Settings.Calculate() — Cyotek 平面细分算法
    ↓
计算交点 → 生成 Rectangle[] （所有区域）
    ↓
每个 Rectangle 生成一个 SnapAreaEditor（设计模式）/ SnapArea（运行模式）
```

### 1.2 关键数据结构

| 组件 | 文件 | 职责 |
|------|------|------|
| [`Layout`](SnapIt.Common/Entities/Layout.cs) | 数据模型 | 持有 `List<Line> LayoutLines` 和 `List<LayoutOverlay>` |
| [`Line`](SnapIt.Common/Graphics/Line.cs) | 数据模型 | 单条分割线：Point(起点)、Start、End、Size、SplitDirection |
| [`SnapBorder`](SnapIt.Controls/SnapBorder.xaml.cs) | 控件 | 可拖拽的分割线可视化，对应一条 `Line` |
| [`SnapAreaEditor`](SnapIt.Controls/SnapAreaEditor.cs) | 控件 | 设计模式下的可编辑区域，持有 Split 命令 |
| [`Settings.Calculate()`](SnapIt.Common/Math/FindRectangle/Settings.cs) | 算法核心 | 从 Segment 集合计算所有矩形区域 |

### 1.3 当前分割逻辑的局限性

[`SnapAreaEditor.Split()`](SnapIt.Controls/SnapAreaEditor.cs#L116-L138) 的实现：

```csharp
// 垂直分割：始终在 X 中点处画线 → 50% | 50%
point = new Point((rect.TopLeft.X + rect.BottomRight.X) / 2, rect.TopLeft.Y);

// 水平分割：始终在 Y 中点处画线 → 50% | 50%
point = new Point(rect.TopLeft.X, (rect.TopLeft.Y + rect.BottomRight.Y) / 2);
```

**根本原因**：每次 `Split()` 只创建 **1 条线**，且位置硬编码为中点。无法一次性创建多条均分线。

### 1.4 为什么"只能分一半"

系统架构是 **「基于线段的平面切割」** 而非 **「树形嵌套区域」**：

- 不存在"父区域→子区域"的层级关系
- 所有分割线都是扁平存储在 `Layout.LayoutLines` 中
- 区域是通过算法从线的交点**反算**出来的
- 因此在一个区域上操作时，新线只影响该区域的 Rect 范围，但线本身是全局的

```
初始状态：     ┌─────────────┐
               │             │  ← 1个区域
               │             │
               └─────────────┘

垂直分割后：    ┌──────┬──────┐
               │      │      │  ← 2个区域 (1条垂直线)
               │  A   │  B   │
               └──────┴──────┘

再分割A：       ┌──────┬──────┐
               ├───┬──┤      │  ← 3个区域 (1垂直+1水平)
               │ C │ D│  B   │
               └───┴──┴──────┘
                    ↑
              这条线只在A的范围内，
              但它是一条贯穿的全局水平线
```

---

## 二、问题一：N 等分分割

### 2.1 问题本质

当前 `Split(direction)` 只能产生 1 条中位线。要实现 33.3%×3 或 25%×4 的均分，需要 **在同一区域内一次插入 N-1 条等距线**。

### 2.2 方案A：快捷 N 等分按钮组（推荐 ⭐）

#### 思路
在现有的 SplitVertically / SplitHorizontally 按钮旁，增加一组预设等分按钮：

```
[⊥ 垂直] [≡ 水平] [⊥ 3等分] [≡ 3等分] [⊥ 4等分] [≡ 4等分]
```

或更紧凑的下拉式设计：

```
[▼ 垂直分割 v] [▼ 水平分割 v]
   ├ 2等分 (50%|50%)     ← 当前默认行为
   ├ 3等分 (33%|33%|33%)
   ├ 4等分 (25%|25%|25%|25%)
   └ 5等分 (20%|...|20%)
```

#### 实现要点

**1）扩展 `Split()` 方法签名**

```csharp
// 当前签名
private void Split(SplitDirection direction)

// 扩展为
private void Split(SplitDirection direction, int divideCount = 2)
```

**2）核心修改 — [`SnapAreaEditor.Split()`](SnapIt.Controls/SnapAreaEditor.cs#L116-L138)**

```csharp
private void Split(SplitDirection direction, int divideCount = 2)
{
    var rect = GetRect();

    if (divideCount < 2) return;

    // 生成 N-1 条等距分割线
    for (int i = 1; i < divideCount; i++)
    {
        Point point;
        Size size;

        double ratio = (double)i / divideCount;

        if (direction == SplitDirection.Vertical)
        {
            point = new Point(
                rect.TopLeft.X + (rect.Width * ratio),
                rect.TopLeft.Y);
            size = new Size(double.NaN, rect.Height);
        }
        else
        {
            point = new Point(
                rect.TopLeft.X,
                rect.TopLeft.Y + (rect.Height * ratio));
            size = new Size(rect.Width, double.NaN);
        }

        var newBorder = new SnapBorder(SnapControl, new SnapAreaTheme());
        newBorder.SetPos(point, size, direction);
        SnapControl.AddBorder(newBorder);
    }
}
```

**3）XAML 层面 — 修改 [`SnapAreaEditor.xaml`](SnapIt.Controls/Styles/SnapAreaEditor.xaml#L48-L72)**

```xml
<StackPanel Orientation="Horizontal">
    <!-- 现有按钮保持不变 -->
    <ui:Button x:Name="SplitVertically" ... />
    <ui:Button x:Name="SplitHorizantally" ... />

    <!-- 新增：N等分按钮 -->
    <ui:Button x:Name="SplitVerticalThirds"
        Content="⊥³"
        ToolTip="垂直三等分 (33%|33%|33%)"
        Command="{Binding Path=SplitVerticalThirdsCommand, ...}" />

    <ui:Button x:Name="SplitHorizontalThirds"
        Content="≡³"
        ToolTip="水平三等分 (33%|33%|33%)"
        Command="{Binding Path=SplitHorizontalThirdsCommand, ...}" />

    <!-- 可选：更多等分选项 -->
</StackPanel>
```

**4）新增 Command 定义**

```csharp
// 在 SnapAreaEditor 构造函数中
SetValue(SplitVerticalThirdsCommandProperty,
    new RelayCommand<object>(o => Split(SplitDirection.Vertical, 3)));
SetValue(SplitHorizontalThirdsCommandProperty,
    new RelayCommand<object>(o => Split(SplitDirection.Horizontal, 3)));

// 同理可扩展 4等分、5等分...
```

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 实现简单，改动量小 | 按钮数量随等分数增加而增长 |
| 用户意图明确，无需学习成本 | 占用工具栏空间 |
| 与现有架构完美兼容 | 灵活性有限（需要预设） |

---

### 2.3 方案B：滚轮调节 N 等分（高级交互）

#### 思路
将 Split 按钮改造为支持 **鼠标滚轮事件**，滚动时动态预览 N 等分效果：

```
┌─────────────────────────────────────┐
│  [⊥ 垂直  N=3 ▲]  [≡ 水平  N=2 ▲]  │  ← 滚轮增减 N
└─────────────────────────────────────┘

滚轮向上 → N++ (最多 8~10)
滚轮向下 → N-- (最少 2)
点击确认 → 执行分割
```

#### 实现要点

**1）新增状态属性**

```csharp
public int VerticalDivideCount { get; set; } = 2;
public int HorizontalDivideCount { get; set; } = 2;

// 用于实时预览的叠加层
private List<SnapBorder> previewBorders = [];
```

**2）滚轮事件处理**

```csharp
private void SplitButton_MouseWheel(object sender, MouseWheelEventArgs e)
{
    var isVertical = sender == SplitVertically;
    ref int count = ref isVertical ? ref VerticalDivideCount : ref HorizontalDivideCount;

    count += e.Delta > 0 ? 1 : -1;
    count = Math.Clamp(count, 2, 10); // 限制范围

    UpdatePreview(isVertical, count);
    e.Handled = true;
}
```

**3）实时预览机制**

当用户滚动改变 N 时，在当前区域上绘制虚线预览：
- 复用 `SnapBorder` 但设置半透明样式
- 存储在 `previewBorders` 中
- 确认时转为正式 Border，取消时清除

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 交互优雅，一个按钮覆盖所有 N | 实现复杂度较高 |
| 支持任意 N（2~10） | 需要处理预览状态的边界情况 |
| 节省 UI 空间 | 滚轮操作不够直观（用户可能不知道可以滚） |

---

### 2.4 方案C：右键菜单等分级联（折中方案）

#### 思路
在现有按钮上增加右键菜单，或者将 ContextMenu 增强：

```xml
<Grid.ContextMenu>
    <ContextMenu>
        <MenuItem Header="垂直分割" Click="SplitVertically_Click">
            <MenuItem Header="2 等分 (50%|50%)" Tag="2" />
            <MenuItem Header="3 等分 (33%|33%|33%)" Tag="3" />
            <MenuItem Header="4 等分 (25%×4)" Tag="4" />
            <MenuItem Header="5 等分 (20%×5)" Tag="5" />
            <MenuItem Header="自定义..." Tag="0" /> <!-- 弹出输入框 -->
        </MenuItem>
        <MenuItem Header="水平分割" ...>
            <!-- 同上 -->
        </MenuItem>
    </ContextMenu>
</Grid.ContextMenu>
```

注意：[`SnapAreaEditor.xaml`](SnapIt.Controls/Styles/SnapAreaEditor.xaml#L19-L38) 已经有 ContextMenu 的雏形了。

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 不增加额外按钮 | 右键发现性较差 |
| 支持自定义数值 | 移动端不适用（如有） |
| 改动极小 | 与按钮操作分离，认知负担 |

---

## 三、问题二：移除/合并分割线

### 3.1 问题本质

当前系统中 **只有添加分割线的入口**，没有删除 `SnapBorder` / `Line` 的路径。要实现合并，本质上就是：

```
移除一条 SnapBorder（从 MainGrid 和 LayoutLines 中删除）
    ↓
重新调用 GenerateSnapAreas()
    ↓
Calculate() 自动生成更大的合并区域 ✅
```

**关键洞察**：因为底层是「线段平面切割」算法，**删除一条线后，算法会自动计算出合并后的区域**。不需要手动处理"合并"逻辑！

### 3.2 方案A：双击分割线删除（推荐 ⭐⭐）

#### 思路
对 [`SnapBorder`](SnapIt.Controls/SnapBorder.xaml.cs) 添加双击事件，直接删除该分割线。

#### 实现要点

**1）[`SnapBorder.xaml.cs`](SnapIt.Controls/SnapBorder.xaml.cs) 添加双击处理**

```csharp
protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
{
    base.OnMouseDoubleClick(e);

    if (!IsDraggable) return;

    // 从布局中移除此分割线
    SnapControl.RemoveBorder(this);
}
```

**2）[`SnapControl`](SnapIt.Controls/SnapControl.xaml.cs) 新增 `RemoveBorder()` 方法**

```csharp
public void RemoveBorder(SnapBorder borderToRemove)
{
    // 1. 从 MainGrid 移除控件
    MainGrid.Children.Remove(borderToRemove);

    // 2. 如果在设计模式，同时移除对应的 SnapBorderTool
    if (IsDesignMode && borderToRemove.SnapBorderTool != null)
    {
        MainGrid.Children.Remove(borderToRemove.SnapBorderTool);
    }

    // 3. 从 Layout.LayoutLines 中移除对应的数据
    if (borderToRemove.LayoutLine != null && Layout?.LayoutLines != null)
    {
        Layout.LayoutLines.Remove(borderToRemove.LayoutLine);
    }

    // 4. 重新生成所有区域
    GenerateSnapAreas();
}
```

**3）可选：增加视觉提示和二次确认**

```csharp
protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
{
    base.OnMouseDoubleClick(e);

    if (!IsDraggable) return;

    // 高亮闪烁提示即将删除
    Border.Background = Theme.HighlightBrush;

    Task.Delay(200).ContinueWith(_ =>
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SnapControl.RemoveBorder(this);
        });
    });
}
```

**4）可选：显示 Tooltip 提示**

在 `SnapBorder` 的 XAML 模板中添加：
```xml
ToolTipService.ToolTip="双击删除此分割线"
```

#### 行为示例

```
三等分状态：          双击中间线后：
┌───┬───┬───┐       ┌───────┬───────┐
│   │   │   │  →    │       │       │
│ A │ B │ C │       │  A+B  │   C   │
│   │   │   │       │       │       │
└───┴───┴───┘       └───────┴───────┘
     ↑↑↑                   ↑
   双击这条线            自动合并为两个区域
```

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 操作直觉性强（双击=删除是常见模式） | 可能与某些系统的双击拖拽冲突 |
| 实现极其简洁（~20行代码） | 无二次确认可能误操作 |
| 天然支持任意复杂度的布局合并 | 需要用户知道这个手势 |

---

### 3.3 方案B：分割线右键菜单删除

#### 思路
在 `SnapBorder` 上挂载右键上下文菜单，提供"删除分割线"选项。

#### 实现

**1）[`SnapBorder.xaml`](SnapIt.Controls/SnapBorder.xaml) 添加 ContextMenu**

```xml
<UserControl ...>
    <UserControl.ContextMenu>
        <ContextMenu>
            <ui:MenuItem Header="删除此分割线"
                         Icon="{ui:SymbolIcon Delete24}"
                         Click="DeleteBorder_Click" />
            <Separator />
            <ui:MenuItem Header="等分布局"
                         Icon="{ui:SymbolIcon Equalifier24}"
                         Click="Equalize_Click" />
        </ContextMenu>
    </UserControl.ContextMenu>
    ...
</UserControl>
```

**2）事件处理**

```csharp
private void DeleteBorder_Click(object sender, RoutedEventArgs e)
{
    SnapControl.RemoveBorder(this);
}

// 额外功能：将同方向的线等距排列
private void Equalize_Click(object sender, RoutedEventArgs e)
{
    // 收集同一方向的所有线
    var sameDirBorders = SnapControl.FindChildren<SnapBorder>()
        .Where(b => b.IsDraggable && b.SplitDirection == SplitDirection)
        .OrderBy(b => b.SplitDirection == SplitDirection.Vertical
            ? b.Margin.Left : b.Margin.Top)
        .ToList();

    if (sameDirBorders.Count < 2) return;

    // 计算等分位置并移动
    // ... (见下文"附加功能")
}
```

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 有明确的文字提示，不会误操作 | 右键操作步骤多 |
| 可扩展更多操作（如等分、属性编辑） | 右键菜单会拦截拖拽开始时的右键 |
| 符合 Windows 用户习惯 | |

---

### 3.4 方案C：分割线端点的拖拽删除（视觉效果最佳）

#### 思路
每条分割线的两端有小的"手柄"，用户可以将手柄拖到边缘来"收缩"整条线直到消失。

```
正常状态：           拖拽端点后：
    │←───线───→│          │←──线──→
    ○          ○    →     ○
   左柄       右柄         (右柄被拖到右边界，线缩短)
                                    进一步拖拽...
                              (线完全消失 = 删除)
```

#### 实现复杂度评估

| 维度 | 复杂度 | 说明 |
|------|--------|------|
| 视觉模板修改 | 中 | 需要在 SnapBorder 两端添加手柄元素 |
| 拖拽逻辑 | 高 | 需区分"整体拖拽移动线"和"端点拖拽缩放线" |
| 删除判定 | 低 | 当线长度 < 阈值时触发 RemoveBorder |
| 与现有碰撞检测兼容性 | 中 | MoveBorder 的碰撞逻辑需适配变长线 |

#### 伪代码思路

```csharp
// SnapBorder 新增属性
public bool IsResizing { get; private set; }
public Point ResizeOrigin { get; private set; }

protected override void OnMouseDown(MouseButtonEventArgs e)
{
    var pos = e.GetPosition(this);

    // 判断是否点中了端点区域（左右各 16px）
    if (pos.X < 16 || pos.X > ActualWidth - 16)
    {
        IsResizing = true;
        ResizeOrigin = pos;
        // 开始端点拖拽模式
    }
    else
    {
        // 现有的整体拖拽逻辑
        base.OnMouseDown(e);
    }
}

protected override void OnMouseMove(MouseEventArgs e)
{
    if (IsResizing)
    {
        // 改变线的 Length 而非 Position
        // 当 Length < THICKNESS 时 → RemoveBorder
    }
    else
    {
        base.OnMouseMove(e);
    }
}
```

#### 优缺点

| 优点 | 缺点 |
|------|------|
| 视觉表达最直观 | 实现工作量最大（预估 2-3 天） |
| 无需额外 UI 元素 | 端点在手势操作下可能误触 |
| 与专业设计软件（如 Figma）体验一致 | 变长线的碰撞检测需要重构 |

---

### 3.5 关于"复杂布局下的合并 Span 行为"的特别分析

你担心的核心问题是类似这样的场景：

```
┌─────┬─────┬─────┐
│     │     │     │
│  A  │  B  │  C  │   ← 三等分
│     │     │     │
├─────┼─────┼─────┤
│     │     │     │
│  D  │  E  │  F  │   ← 再水平分割一次
│     │     │     │
└─────┴─────┴─────┘

问题：如果删掉中间那条垂直线，会发生什么？
```

**好消息**：由于使用的是 **Cyotek 平面细分算法**（[`Settings.Calculate()`](SnapIt.Common/Math/FindRectangle/Settings.cs#L39-L43)），这个问题 **已经被自动处理** 了！

算法的工作方式：
1. 收集所有线段（包括边界框的 4 条边 + 所有内部线）
2. 计算所有交点和连接关系
3. 从左上角开始，寻找所有合法的闭合矩形

当你删除中间垂直线后：
```
删除前6条内部线：     删除后5条内部线：
  V1   V2   V3          V1        V3
  │    │    │           │         │
├─┼────┼────┤        ┌─┴─────────┴─┐
H1│ A  │ B  │ C   →   │    B+C合并   │
  ├────┼────┤        │             │
H2│ D  │ E  │ F      ├─────────────┤
  │    │    │        │ D    │    F │
  └────┴────┘        └──────┴──────┘
                       V1    V3
                      (A独立)(D独立)

结果：5个区域 (A, B+C, D, E?, F)
     算法自动正确处理！✅
```

**结论**：不需要额外的 Span/合并逻辑。`RemoveBorder()` → `GenerateSnapAreas()` 这条路径已经能正确处理任意复杂度的布局简化。

---

## 四、推荐组合方案

### 4.1 MVP（最小可行方案）— 建议优先实现

| 功能 | 采用方案 | 改动文件 | 改动量估算 |
|------|----------|----------|-----------|
| N 等分 | **方案A**：快捷按钮（3等分+4等分） | [`SnapAreaEditor.cs`](SnapIt.Controls/SnapAreaEditor.cs), [`SnapAreaEditor.xaml`](SnapIt.Controls/Styles/SnapAreaEditor.xaml) | ~60行 |
| 删除分割线 | **方案A**：双击删除 | [`SnapBorder.xaml.cs`](SnapIt.Controls/SnapBorder.xaml.cs), [`SnapControl.xaml.cs`](SnapIt.Controls/SnapControl.xaml.cs) | ~30行 |

**总改动量：约 90 行代码，涉及 4 个文件**

### 4.2 进阶方案（后续迭代）

| 功能 | 采用方案 | 说明 |
|------|----------|------|
| 任意 N 等分 | 方案B（滚轮）或 方案C（右键级联） | 在 MVP 验证用户反馈后再决定 |
| 等分布局修正 | 在方案B的右键菜单中加入"等分"选项 | 将手动拖拽的不均匀线自动等距排列 |
| 撤销/重做 | 记录每次 AddBorder/RemoveBorder 操作 | 配合 Command Pattern 实现 |

---

## 五、实施路线图

```
Phase 1 (MVP)                     Phase 2 (增强)                  Phase 3 (打磨)
─────────────                     ─────────────                   ─────────────
✅ Split(direction, N) 重构        ✅ 滚轮 N 等分                   ✅ Undo/Redo
✅ 3等分/4等分按钮                 ✅ 自定义 N 输入框               ✅ 键盘快捷键
✅ 双击删除分割线                  ✅ 右键菜单增强                   ✅ 等分对齐辅助线
✅ RemoveBorder() 方法            ✅ 删除前的预览高亮               ✅ 分割线吸附网格
                                 ✅ 等分布局一键修正
```

---

## 六、关键风险与注意事项

### 6.1 数据一致性
- `RemoveBorder()` 必须同步清理三个地方：`MainGrid.Children`、`Layout.LayoutLines`、`SnapBorderTool`
- 建议：在 `RemoveBorder()` 内部加 `Debug.Assert` 校验数量一致

### 6.2 边界条件
- **不允许删除最后一条内部线**：否则退化为单区域（虽然技术上可行，但可能不符合预期）
- **不允许删除导致零面积区域的操作**：`IsCollided()` 已有保护
- **等分精度**：浮点除法可能导致像素级误差，`FixPoints()` 中的 `tolerance=4` 应该能覆盖

### 6.3 性能考量
- 每次 `AddBorder` / `RemoveBorder` 都触发 `GenerateSnapAreas()` → `Settings.Calculate()`
- 当分割线数量 > 20 时，算法复杂度为 O(n²)（n 为线段数），一般场景无需担心
- 如需优化：可考虑增量更新而非全量重算

---

## 七、附录：相关代码索引

| 功能 | 文件 | 关键行号 |
|------|------|---------|
| 分割命令定义 | [`SnapAreaEditor.cs`](SnapIt.Controls/SnapAreaEditor.cs) | L71-L95 |
| 分割执行逻辑 | [`SnapAreaEditor.Split()`](SnapIt.Controls/SnapAreaEditor.cs#L116-L138) | L116-L138 |
| 添加分割线 | [`SnapControl.AddBorder()`](SnapIt.Controls/SnapControl.xaml.cs#L274-L289) | L274-L289 |
| 区域生成算法 | [`Settings.Calculate()`](SnapIt.Common/Math/FindRectangle/Settings.cs#L39-L43) | L39-L191 |
| 分割线拖拽 | [`SnapBorder.MoveBorder()`](SnapIt.Controls/SnapBorder.xaml.cs#L138-L213) | L138-L213 |
| 碰撞检测 | [`SnapBorder.IsCollided()`](SnapIt.Controls/SnapBorder.xaml.cs#L215-L245) | L215-L245 |
| 布局加载/保存 | [`SnapControl.LoadLayout()`](SnapIt.Controls/SnapControl.xaml.cs#L374-L448) | L374-L448 |
| XAML 模板 | [`SnapAreaEditor.xaml`](SnapIt.Controls/Styles/SnapAreaEditor.xaml#L49-L71) | L49-L71 |
