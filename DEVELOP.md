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
│   ├── System/                     # 系统交互层
│   │   ├── WinEventHook.cs         # SetWinEventHook 封装
│   │   └── ShellWindowsInterop.cs  # IShellWindows COM 封装
│   ├── UI/                         # UI 层
│   │   ├── OverlayWindow.xaml.cs   # WinUI 3 覆盖层窗口
│   │   └── TrayIcon.cs             # 系统托盘图标（含右键菜单配置）
│   └── Util/                       # 工具层
│       ├── NativeMethods.cs        # P/Invoke 签名集中定义（含 Win32 API、GDI、DWM）
│       └── UacHelper.cs            # UAC 权限检测工具
├── config/                         # 配置文件目录
│   └── whitelist.json              # 白名单应用配置（预留）
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
| 系统托盘图标 | ✅ | 右键菜单直接切换配置（显示开关、日志级别、最大项目数） |
| 对话框关闭隐藏 Overlay | ✅ | ShowWindow(SW_HIDE) 隐藏窗口 |
| 暗色/亮色主题适配 | ✅ | Overlay 跟随系统主题自动切换 |
| Per-Monitor DPI 适配 | ✅ | 根据对话框 DPI 缩放 Overlay 尺寸 |
| UAC 提权检测 | ✅ | 检测管理员权限对话框并显示 Toast 提示 |
| 文件夹列表缓存 | ✅ | 2秒缓存避免重复 COM 调用 |

### 技术要点

- **对话框检测**：`SetWinEventHook` 监听 `EVENT_OBJECT_CREATE` 和 `EVENT_SYSTEM_FOREGROUND` 事件
- **导航注入**：检测到面包屑控件（ToolbarWindow32）时，先点击切换到编辑模式，再找到 Edit 控件设置路径
- **Overlay 定位**：显示在对话框下方外部（`y = dialogRect.Bottom + gap`），不遮挡对话框内容
- **Overlay 隐藏**：使用 `ShowWindow(SW_HIDE/SW_SHOW)` 控制窗口可见性
- **Overlay 圆角**：使用 `CreateRoundRectRgn` + `SetWindowRgn` 裁剪窗口为圆角矩形（WinUI 3 不支持透明背景）
- **窗口边框移除**：使用 `DwmExtendFrameIntoClientArea` 和移除 `WS_CAPTION | WS_THICKFRAME` 样式
- **托盘图标**：通过 Win32 窗口子类化处理托盘消息，右键菜单使用 `CreatePopupMenu` + `TrackPopupMenu`
- **托盘菜单配置**：使用 `MF_CHECKED` 标记显示开关状态，子菜单选择日志级别和最大项目数
- **主题适配**：读取注册表 `AppsUseLightTheme` 判断系统主题
- **DPI 适配**：使用 `GetDpiForWindow` 获取 DPI，按比例缩放尺寸
- **UAC 检测**：使用 `OpenProcessToken` + `GetTokenInformation` 检测进程权限

### 下一步（P1/P2 剩余功能）

- 白名单引擎实现（JSON 加载、匹配、热更新）
- 主流应用适配：Chrome、Edge、VS Code、Office、WPS
- UI Automation 导航注入（uia_set_value + clipboard_set 两种策略）
- Win11 多标签路径获取（需要更深入的 COM 接口探索）

## 已知问题

- 部分应用关闭对话框时可能触发多个 `EVENT_OBJECT_DESTROY` 事件
- 命名空间冲突：`EasyAccess.System` 与 .NET `System` 命名空间冲突，需使用 `global::System` 明确引用
- Win11 多标签支持：当前实现使用 `IShellWindows` 枚举，可能无法获取同一窗口的所有标签页路径（Win11 资源管理器多标签）
- Overlay 透明背景：WinUI 3 不支持真正的透明背景，当前使用 `SetWindowRgn` 裁剪窗口实现圆角效果，圆角可能与 Border 不完全重合

## 配置说明

配置文件 `config.json` 存储在 exe 同目录或 `%APPDATA%\EasyAccess\`（如果 exe 在只读目录）。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ShowOverlayOnDetect | bool | true | 检测到对话框时是否显示 Overlay |
| MaxOverlayItems | int | 3 | Overlay 中最多显示的文件夹数量（可选 1-5） |
| OverlayPosition | string | "bottom" | Overlay 位置（当前固定为 bottom） |
| Theme | string | "system" | 主题选择：system/light/dark |
| LogLevel | string | "info" | 日志级别：debug/info/warn/error |
| WhitelistFile | string | "whitelist.json" | 白名单文件路径（预留） |

配置可通过托盘右键菜单直接切换（显示开关、日志级别、最大项目数），修改后立即生效并保存。

## 最近更新

- **Overlay 圆角裁剪**：使用 `SetWindowRgn` 裁剪窗口为圆角矩形，移除标题栏和边框
- **Overlay 位置修复**：显示在对话框下方外部，不再遮挡对话框内容
- **托盘菜单配置**：删除设置窗口，改为右键菜单直接切换配置（显示开关、日志级别、最大项目数）
- **UAC 提权检测**：检测管理员权限对话框时显示 Toast 提示
- **主题适配**：Overlay 跟随系统主题自动切换
- **DPI 适配**：根据对话框 DPI 正确缩放 Overlay 尺寸
- **文件夹缓存**：2秒缓存避免重复 COM 调用

## 文档

详见 `docs/EasyAccess-Development-Guide-v2.html`（C# 版开发指导文档）。
