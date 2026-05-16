# SnapIt-Plus

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF-5C2D91.svg)](https://github.com/dotnet/wpf)
![Build](https://img.shields.io/badge/build-passing-brightgreen)

**Read in other languages: [English](README.en.md), [中文](README.md)**

---

**SnapIt-Plus** 是一款适用于 Windows 10/11 的强大窗口管理器，通过将应用程序窗口组织到可自定义的吸附区域中，显著提升单屏和多屏环境下的工作效率。本项目源自 [SnapIt](https://github.com/enginkirmaci/SnapIt)（作者 Engin KIRMACI），SnapIt-Plus 在此基础上持续演进，不断增加新功能和改进。

设计理念：快速、直观、高度可定制——完美适配宽屏、超宽屏和多显示器场景。

---

## 截图预览

<table>
  <tr>
    <td><img src="/documents/00.png" alt="使用演示" width="400"></td>
    <td><img src="/documents/1.png" alt="布局管理" width="400"></td>
  </tr>
  <tr>
    <td align="center"><em>窗口吸附效果</em></td>
    <td align="center"><em>布局管理界面</em></td>
  </tr>
  <tr>
    <td><img src="/documents/0.png" alt="布局设计器" width="400"></td>
    <td></td>
  </tr>
  <tr>
    <td align="center"><em>布局设计器</em></td>
    <td></td>
  </tr>
</table>

---

## 功能特性

### 屏幕分区与布局系统
- 通过**垂直/水平分割线**将屏幕划分为可自定义的吸附区域
- 每个屏幕可**独立分配不同布局**
- 内置 **11 种预定义布局**，覆盖常见使用场景（竖屏/横屏）
- **可视化布局设计器**：拖拽式创建和编辑自定义布局
- 基于 **SnapEngine** 的吸附对齐（8px 吸附阈值）
- 布局可**导入/导出**为 JSON 文件

### 窗口吸附方式

**鼠标拖拽吸附**
- 将窗口拖拽到吸附区域即可自动吸附
- 可配置鼠标按键（左键/中键/右键）
- 可调节拖拽延迟
- 支持"按住标题栏拖拽"模式
- 支持修饰键切换启用/禁用

**键盘吸附**
- 使用快捷键在区域间移动窗口
- 默认快捷键：`Ctrl+Alt+方向键`
- 可覆盖 Windows 默认吸附快捷键（`Win+方向键`）
- 循环布局快捷键（`Ctrl+Alt+C`）
- 启动/停止快捷键（`Ctrl+Alt+S`）

### 应用启动器（应用组）
- 为每个屏幕配置**应用组**
- 一键启动多个应用并自动放置到指定吸附区域
- 支持启动延迟设置

### 主题系统
- 自定义吸附区域的**高亮颜色、覆盖颜色、边框颜色、边框粗细和透明度**
- 支持**深色/浅色/跟随系统**三种主题模式
- 实时预览主题效果

### 窗口排除规则
- 按窗口标题匹配，排除特定应用不参与吸附
- 三种匹配规则：**包含 / 精确 / 通配符**
- 可分别对鼠标和键盘吸附生效
- 默认排除：操作中心、开始菜单、新通知

### 多屏幕与 DPI 支持
- 完整支持多显示器环境
- 每个屏幕独立 DPI 感知
- 支持屏幕热插拔检测
- 支持不同任务栏位置的屏幕

### 应用管理
- 系统托盘图标，快速访问
- 开机自启
- 新版本检查
- 管理员权限运行
- 全屏/模态窗口自动禁用吸附
- 窗口圆角自动处理

### 设置存储
- 使用 **SQLite** 数据库（`SnapItSettings.db`），存储在 `%LocalApplicationData%\SnapIt\`
- 首次运行时**自动迁移**旧版 JSON 文件
- 优势：ACID 事务保证、读写性能提升、单文件备份

---

## 环境要求

- **操作系统**：Windows 10（Build 17763+）或 Windows 11
- **运行时**：.NET 9.0 Desktop Runtime（独立安装版需要）
- **架构**：x86 / x64

---

## 安装方式

### Microsoft Store 版本（通过 MSIX 打包）
1. 在 Visual Studio 中打开 SnapIt.Packaging 项目
2. 编译 WAP 项目生成 `.msix`
3. 通过生成的包进行安装

### 独立版安装（Inno Setup）
1. 从 [Releases](https://github.com/Ghost-Girls/SnapIt-Plus/releases) 页面下载最新的 `setup_SnapIt_x.x.x.x.exe`
2. 运行安装程序，按照向导完成安装

### 源码编译
```bash
git clone https://github.com/Ghost-Girls/SnapIt-Plus.git
cd SnapIt-Plus
```

用 Visual Studio 2022+ 打开 `SnapIt.sln`，然后编译：

```bash
# 调试构建
dotnet build SnapIt.sln -c Debug

# 独立版发布（用于 Inno Setup 打包）
dotnet publish SnapIt/SnapIt.csproj -c Standalone -a x86 -o ./SnapIt.Setup/build --self-contained false

# 生成 Inno Setup 安装包
iscc.exe SnapIt.Setup/innoSetup.iss /DMyAppVersion=5.3.0.0
```

---

## 配置选项

打开 **SnapIt** 应用程序窗口可访问所有设置：

### 布局
| 选项 | 说明 |
|------|------|
| 选择布局 | 为每个屏幕选择预定义或自定义布局 |
| 编辑布局 | 打开可视化设计器创建/修改布局 |
| 导入布局 | 从 JSON 文件加载布局 |
| 导出布局 | 将布局保存为 JSON 文件 |

### 吸附设置
| 选项 | 默认值 | 说明 |
|------|--------|------|
| 鼠标吸附 | 开启 | 启用鼠标拖拽吸附 |
| 吸附鼠标键 | 中键 | 左键 / 中键 / 右键触发吸附 |
| 拖拽延迟 | 0ms | 吸附激活前的延迟（0-1000ms） |
| 覆盖 Win+方向键 | 关闭 | 覆盖 Windows 默认吸附快捷键 |
| 键盘吸附快捷键 | Ctrl+Alt+方向键 | 在区域间移动窗口的热键 |

### 外观样式
| 选项 | 默认值 | 说明 |
|------|--------|------|
| 主题 | 深色 | 深色 / 浅色 / 跟随系统 |
| 高亮颜色 | 自定义 | 悬停时区域高亮颜色 |
| 覆盖颜色 | 自定义 | 区域覆盖层颜色 |
| 边框颜色 | 自定义 | 区域边框颜色 |
| 边框粗细 | 自定义 | 区域边框粗细 |
| 透明度 | 自定义 | 区域覆盖层透明度 |

### 应用组
| 选项 | 说明 |
|------|------|
| 添加组 | 为某个屏幕创建新的应用组 |
| 添加应用 | 将应用加入组并指定目标区域 |
| 启动延迟 | 配置启动每个应用之间的延迟 |
| 一键启动 | 启动组中所有应用并自动放置 |

### 排除规则
| 选项 | 默认值 | 说明 |
|------|--------|------|
| 鼠标排除规则 | 禁用 | 包含 / 精确 / 通配符匹配 |
| 键盘排除规则 | 禁用 | 包含 / 精确 / 通配符匹配 |

---

## 项目架构

```
SnapIt-Plus
├── SnapIt                          # 主 WPF 应用程序（Views, ViewModels, Pages）
├── SnapIt.Application              # 核心业务逻辑
│   ├── SnapManager.cs              # 中央吸附协调器
│   ├── WindowManager.cs            # 窗口位置与状态管理
│   └── ScreenManager.cs            # 多屏幕管理
├── SnapIt.Common                   # 共享库
│   ├── Entities/                   # 数据模型：Settings, Layout, Theme, Constants, Enums
│   ├── Graphics/                   # 自定义图形原语（Rect, Point, Size, Line, Dpi）
│   ├── Math/                       # FindRectangle, FindClosest 算法
│   ├── Converters/                 # WPF 值转换器
│   └── Contracts/                  # 接口与基类
├── SnapIt.Controls                 # 自定义 WPF 控件
│   ├── SnapArea.cs                 # 单个吸附区域控件
│   ├── SnapBorder.cs               # 区域边框装饰
│   ├── SnapOverlay.cs              # 区域覆盖层渲染
│   └── SnapEngine.cs               # 吸附对齐引擎
├── SnapIt.Services                 # 服务层
│   ├── Hooks/                      # 全局鼠标/键盘钩子（SharpHook）
│   ├── HotKeys/                    # 全局热键注册
│   ├── Database/                   # EF Core + SQLite（SettingsDbContext）
│   └── License/                    # Store 与独立版许可服务
├── SnapIt.Layouts                  # 预定义布局库（11 个内置 JSON 布局）
├── SnapIt.Packaging                # MSIX 打包（Windows 应用项目）
├── SnapIt.Setup                    # Inno Setup 安装脚本
├── SnapIt.Test                     # 测试/设计时项目
├── WindowInspector                 # 辅助工具：窗口信息检查器
└── SnapSample                      # SDK 使用示例
```

### 核心组件

| 组件 | 文件 | 职责 |
|------|------|------|
| **SnapManager** | [SnapManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/SnapManager.cs) | 中央协调器：管理吸附会话、区域检测和窗口放置 |
| **SnapEngine** | [SnapEngine.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Controls/SnapEngine.cs) | 吸附对齐计算（8px 阈值） |
| **ScreenManager** | [ScreenManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/ScreenManager.cs) | 多显示器枚举、DPI 处理、热插拔事件 |
| **WindowManager** | [WindowManager.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.App/WindowManager.cs) | 窗口定位、大小调整和状态管理（P/Invoke） |
| **KeyboardService** | [KeyboardService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Hooks/KeyboardService.cs) | 全局键盘钩子和热键分发 |
| **MouseService** | [MouseService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Hooks/MouseService.cs) | 全局鼠标钩子，检测拖拽吸附 |
| **SettingsService** | [SettingsService.cs](file:///c:/Users/NexusStudio/source/repos/SnapIt-Plus/SnapIt.Services/Services/SettingsService.cs) | EF Core SQLite 持久化设置和布局 |

---

## 技术栈

| 技术 | 用途 |
|------|------|
| **.NET 9.0** | 目标框架（`net9.0-windows10.0.17763.0`） |
| **WPF** | 桌面 UI 框架 |
| **WPF-UI 4.0.3** | 现代化 UI 控件库（导航、Snackbar、对话框、主题） |
| **MVVM** | 架构模式（自定义 ViewModelBase + DelegateCommand） |
| **Microsoft.Extensions.Hosting/DI** | 依赖注入与托管服务 |
| **Serilog** | 结构化文件日志 |
| **Entity Framework Core + SQLite** | 设置和布局持久化存储 |
| **SharpHook 7.0** | 全局鼠标/键盘钩子 |
| **GlobalHotKeyCore** | 全局热键注册 |
| **WpfScreenHelper** | 多屏幕 DPI 感知 |
| **WindowsDisplayAPI** | 显示/显示器 API |
| **PInvoke.SHCore** | Windows P/Invoke 互操作 |
| **XamlAnimatedGif** | GIF 动画支持 |
| **Inno Setup** | 独立版安装包制作 |

---

## 开发指南

### 前提条件
- **Visual Studio 2022+**（安装 .NET 桌面开发工作负载）
- **.NET 9.0 SDK**
- Windows 10（Build 17763+）或 Windows 11

### 构建配置
| 配置 | 说明 |
|------|------|
| `Debug` | 调试构建，用于开发 |
| `Release` | 发布构建 |
| `Standalone` | 独立版构建（非 Microsoft Store 版本） |

### 调试
将 `SnapIt` 设为启动项目，按 **F5** 即可运行。应用程序以独立 WPF 窗口运行。

### 日志
通过 **Serilog** 将日志写入应用程序数据目录，便于排查问题。

---

## CI/CD

- **CI 流水线**：推送/PR 到 `main` 时触发 — 构建所有配置
- **发布流水线**：手动触发 — 自动版本号、Inno Setup 打包、创建 GitHub Release

---

## 已知问题

### 高 DPI 环境下的窗口吸附延迟
在某些高 DPI 多显示器配置下，窗口吸附可能会有轻微延迟。这与 DPI 缩放的往返计算有关。

**状态**：正在排查中。

---

## 贡献指南

欢迎贡献代码和提交 Issue！

### 快速上手
1. Fork 本仓库
2. 为你的更改创建新分支
3. 提交更改
4. 发起 Pull Request

请遵循项目中使用的代码风格和约定。

---

## 许可证

[GNU General Public License v3.0](LICENSE)

**原作者**：Engin KIRMACI
**维护者**：[Ghost-Girls](https://github.com/Ghost-Girls)

---

*为追求整洁桌面的 Windows 效率用户而做。*