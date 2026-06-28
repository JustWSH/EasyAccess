# EasyAccess

Windows 常驻后台工具。检测到文件打开/保存对话框时，在对话框底部自动显示后台已打开的资源管理器文件夹列表，一键导航到目标文件夹。

## 技术栈

- 语言：C# / .NET 8
- UI 框架：WinUI 3 (Windows App SDK 2.2)
- 项目类型：WinUI 3 Unpackaged（`WindowsPackageType=None`）
- IDE：Visual Studio 2026
- 目标平台：Windows 10 1809+ (build 17763) / Windows 11
- 最低 API 版本：10.0.17763.0 (TargetPlatformMinVersion)
- 发布方式：Self-Contained Single-File（Release 模式）

## 项目结构

```
EasyAccess/
├── EasyAccess.slnx                 # 解决方案文件
├── .gitignore
├── DEVELOP.md                      # 本文件
├── EasyAccess/                     # WinUI 3 主项目
│   ├── EasyAccess.csproj
│   ├── app.manifest                # DPI 感知声明 (PerMonitorV2)
│   ├── App.xaml / App.xaml.cs      # 应用入口，集成所有模块
│   ├── MainWindow.xaml / .cs       # 隐藏主窗口（消息泵宿主）
│   ├── Core/                       # 核心逻辑层
│   │   ├── DialogDetector.cs       # 对话框检测引擎
│   │   ├── FolderCollector.cs      # 文件夹列表收集
│   │   └── Navigator.cs            # 路径导航注入
│   ├── Infra/                      # 基础设施层
│   │   ├── ConfigManager.cs        # JSON 配置管理
│   │   ├── Logger.cs               # 文件日志（按日期轮转）
│   │   └── SingleInstance.cs       # Named Mutex 防多开
│   ├── Interop/                    # 系统交互层
│   │   ├── WinEventHook.cs         # SetWinEventHook 封装
│   │   └── ShellWindowsInterop.cs  # IShellWindows COM 封装
│   ├── UI/                         # UI 层
│   │   ├── OverlayWindow.xaml.cs   # WinUI 3 覆盖层窗口
│   │   └── TrayIcon.cs             # 系统托盘图标（含右键菜单配置）
│   └── Util/                       # 工具层
│       ├── NativeMethods.cs        # P/Invoke 签名集中定义（含 Win32 API、GDI、DWM、TrayIcon）
│       └── UacHelper.cs            # UAC 权限检测工具
├── config/                         # 配置文件目录
├── logs/                           # 日志目录（运行时生成）
├── EasyAccess (Package)/           # WAP 打包项目（Unpackaged 模式下不使用）
├── utilitys/                       # C++ 辅助工具（CMake 构建）
│   ├── PrintWindows/               # 窗口元素查看器（Win32 API）
│   └── ProbeUia/                   # UI Automation 树查看器
└── docs/
    ├── EasyAccess-Development-Guide.html      # 开发指导 v1 (C++ 方案)
    └── EasyAccess-Development-Guide-v2.html   # 开发指导 v2 (C# 方案)
```

## 构建

```bash
# 开发构建（Debug，需要已安装 Windows App Runtime）
dotnet build EasyAccess/EasyAccess.csproj

# 运行
dotnet run --project EasyAccess/EasyAccess.csproj

# 发布 Self-Contained 单文件（无需安装运行时）
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true
```

**Debug vs Release 模式**：
- **Debug**：依赖本地安装的 Windows App Runtime，用于开发调试
- **Release**：Self-Contained + WindowsAppSDKSelfContained，产物可独立运行

**日志位置**：
- 日志生成在 exe 旁边的 `logs\` 目录
- 文件名格式：`EasyAccess_YYYYMMDD.log`
- 自动轮转，保留最近 7 天

## 依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| Microsoft.WindowsAppSDK | 2.2.0 | WinUI 3 运行时 |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.1839 | Win32/COM 构建工具 |

## 架构

四层单进程架构：

- **UI 层**：OverlayWindow (WinUI 3)、TrayIcon
- **核心逻辑层**：DialogDetector、FolderCollector、Navigator
- **系统层**：WinEventHook (P/Invoke)、COM Shell、SendInput (P/Invoke)
- **基础设施层**：SingleInstance、ConfigManager、Logger

## 当前状态

P0 阶段已完成，核心功能可用。P1/P2 部分功能已实现（不含白名单和UIA模块）。

### 已实现功能

| 功能 | 状态 | 说明 |
|------|------|------|
| 标准对话框检测 | ✅ | 通过 `#32770` 类名 + 控件特征（Edit + ComboBox + Button）检测 |
| 文件夹列表收集 | ✅ | 通过 `Shell.Application` COM 接口遍历资源管理器打开的文件夹 |
| Overlay 窗口 | ✅ | WinUI 3 窗口，显示在对话框下方外部 |
| 导航注入 | ✅ | 点击面包屑切换编辑模式 → 设置路径 → 模拟 Enter |
| 防多开 | ✅ | Named Mutex 单实例运行 |
| 系统托盘图标 | ✅ | 右键菜单直接切换配置（显示开关、日志级别、最大项目数、语言），支持中英文 |
| 对话框关闭隐藏 Overlay | ✅ | ShowWindow(SW_HIDE) 隐藏窗口 |
| 暗色/亮色主题适配 | ✅ | Overlay 跟随系统主题自动切换 |
| Per-Monitor DPI 适配 | ✅ | 根据对话框 DPI 缩放 Overlay 尺寸 |
| UAC 提权检测 | ✅ | 检测管理员权限对话框并显示 Toast 提示 |
| 文件夹列表缓存 | ✅ | 对话框关闭前缓存文件夹列表，避免重复 COM 调用 |
| 导航后跳过刷新 | ✅ | 点击文件夹导航后，跳过焦点变化触发的列表刷新，仅窗口重新激活时刷新 |

### 技术要点

- **对话框检测**：`SetWinEventHook` 监听 `EVENT_OBJECT_CREATE` 和 `EVENT_SYSTEM_FOREGROUND` 事件
- **导航注入**：检测到面包屑控件（ToolbarWindow32）时，先点击切换到编辑模式，再找到 Edit 控件设置路径
- **Overlay 定位**：显示在对话框下方外部（`y = dialogRect.Bottom + gap`），不遮挡对话框内容
- **Overlay 隐藏**：使用 `ShowWindow(SW_HIDE/SW_SHOW)` 控制窗口可见性
- **Overlay 圆角**：使用 `CreateRoundRectRgn` + `SetWindowRgn` 裁剪窗口为圆角矩形（WinUI 3 不支持透明背景）
- **Overlay 窗口初始化**：窗口样式（移除标题栏、WS_EX_LAYERED 等）在 `ShowOverlay` 首次调用时通过 `InitializeWindowStyles()` 设置，不依赖 `Activated` 事件
- **窗口边框移除**：使用 `DwmExtendFrameIntoClientArea` 和移除 `WS_CAPTION | WS_THICKFRAME` 样式
- **托盘图标**：通过 Win32 窗口子类化处理托盘消息，P/Invoke 签名统一定义在 `NativeMethods.cs`
- **托盘菜单配置**：使用 `MF_CHECKED` 标记显示开关状态，子菜单选择日志级别和最大项目数，支持中英文切换（语言子菜单）
- **文件夹列表缓存**：手动缓存机制，对话框关闭时清除缓存，有缓存时直接复用不刷新
- **主题适配**：读取注册表 `AppsUseLightTheme` 判断系统主题
- **DPI 适配**：使用 `GetDpiForWindow` 获取 DPI，按比例缩放尺寸
- **UAC 检测**：使用 `OpenProcessToken` + `GetTokenInformation` 检测进程权限
- **导航后跳过刷新**：`_justNavigated` 标记 + `CancellationTokenSource` 管理 1 秒延时重置
- **COM 对象管理**：`ShellWindowsInterop` 循环中对每个 `window` 对象调用 `Marshal.ReleaseComObject`

### 下一步（P1/P2 剩余功能）

- 白名单引擎实现（JSON 加载、匹配、热更新）
- 主流应用适配：Chrome、Edge、VS Code、Office、WPS
- UI Automation 导航注入（uia_set_value + clipboard_set 两种策略）
- Win11 多标签路径获取（需要更深入的 COM 接口探索）

## 已知问题

- 部分应用关闭对话框时可能触发多个 `EVENT_OBJECT_DESTROY` 事件
- Win11 多标签支持：当前实现使用 `IShellWindows` 枚举，可能无法获取同一窗口的所有标签页路径（Win11 资源管理器多标签）
- Overlay 透明背景：WinUI 3 不支持真正的透明背景，当前使用 `SetWindowRgn` 裁剪窗口实现圆角效果，圆角可能与 Border 不完全重合

## 配置说明

配置文件 `config.json` 存储在 exe 同目录或 `%APPDATA%\EasyAccess\`（如果 exe 在只读目录）。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ShowOverlayOnDetect | bool | true | 检测到对话框时是否显示 Overlay |
| MaxOverlayItems | int | 3 | Overlay 中最多显示的文件夹数量（可选 1-5） |
| Theme | string | "system" | 主题选择：system/light/dark |
| LogLevel | string | "info" | 日志级别：debug/info/warn/error |
| Language | string | "zh" | 界面语言：zh (中文) / en (English) |

配置可通过托盘右键菜单直接切换（显示开关、日志级别、最大项目数），修改后立即生效并保存。

## 最近更新

- **版本更新**：版本号更新为 v1.1
- **修复 Win11 文件夹列表残留**：修复 Win11 下关闭所有文件夹后 Overlay 仍显示旧列表的问题
  - 根因：Win11 事件触发时序不同，`OnLocationChanged` 在异步文件夹收集完成前就显示了 Overlay
  - 添加 `HasCachedFolders` 属性区分"缓存存在"和"缓存有内容"
  - `OnLocationChanged` 和 `ShowOverlayForDialog` 改用 `HasCachedFolders` 检查
- **托盘菜单新增启动文件夹**：添加"打开启动文件夹"菜单项，方便用户添加开机自启动快捷方式
- **代码审查优化**：全面审查并优化废弃代码、稳定性、性能、日志、代码结构
  - 命名空间 `EasyAccess.System` → `EasyAccess.Interop`，消除所有 `global::System` 前缀
  - 删除废弃代码：DialogDetector 未调用方法、NativeMethods 未使用声明、AppConfig 未使用配置项
  - 简化 SingleInstance：删除不工作的 `TryActivateExisting`，仅保留 Mutex 防多开
  - 修复 ApplyTheme 重复设置、_justNavigated 生命周期管理、COM 对象释放
  - 性能优化：OverlayWindow 使用缓存 `_hwnd`、Logger `AutoFlush=true`
  - 精简高频 Debug 日志，TrayIcon P/Invoke 移入 NativeMethods 统一管理
  - 提取 `NativeMethods.GetAppWindow()` 公共方法
- **修复 Overlay 窗口初始化**：窗口样式设置从 `OnActivated` 移至 `InitializeWindowStyles()`，在 `ShowOverlay` 首次调用时执行，解决 `Activated` 事件不触发导致白条和圆角丢失的问题
- **修复显示Overlay设置不生效**：在 `ShowOverlayForDialog` 和 `OnLocationChanged` 中添加 `ShowOverlayOnDetect` 配置检查
- **托盘菜单多语言支持**：菜单项支持中英文切换（语言子菜单），顶部显示版本号 v1.0
- **文件夹列表缓存优化**：移除 2 秒自动过期缓存，改为手动清理（对话框关闭时），有缓存时复用不刷新
- **导航后跳过刷新**：点击文件夹导航后设置 `_justNavigated` 标记，跳过焦点变化触发的列表刷新，仅窗口重新激活时才刷新
- **Overlay 跟随移动**：监听 `EVENT_OBJECT_LOCATIONCHANGE` 事件，对话框移动时自动更新 Overlay 位置
- **Overlay 图层优化**：使用 `IsAlwaysOnTop` 置顶，对话框失去焦点时自动隐藏，重新获得焦点时恢复显示

## 文档

详见 `docs/EasyAccess-Development-Guide-v2.html`（C# 版开发指导文档）。
