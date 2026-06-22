# EasyAccess

Windows 常驻后台工具。检测到文件打开/保存对话框时，在对话框底部自动显示后台已打开的资源管理器文件夹列表，一键导航到目标文件夹。

## 技术栈

- 语言：C# / .NET 8
- UI 框架：WinUI 3 (Windows App SDK 2.2)
- 项目类型：WinUI 3 Unpackaged（`WindowsPackageType=None`）
- IDE：Visual Studio 2026
- 目标平台：Windows 10 1903+ / Windows 11
- 发布方式：Self-Contained Single-File（Release 模式）

## 项目结构

```
EasyAccess/
├── EasyAccess.slnx                 # 解决方案文件
├── .gitignore
├── claude.md                       # AI Agent 指令
├── DEVELOP.md                      # 本文件
├── EasyAccess/                     # WinUI 3 主项目
│   ├── EasyAccess.csproj
│   ├── app.manifest
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / .cs       # 主窗口（当前为空窗口 + Mica 背景）
│   └── Properties/
│       └── PublishProfiles/
├── EasyAccess (Package)/           # WAP 打包项目（Unpackaged 模式下不使用）
├── utilitys/                       # C++ 辅助工具（CMake 构建）
│   ├── PrintWindows/               # 窗口元素查看器（Win32 API）
│   │   ├── CMakeLists.txt
│   │   ├── main.cpp
│   │   └── WindowElements/
│   └── ProbeUia/                   # UI Automation 树查看器
│       ├── CMakeLists.txt
│       ├── main.cpp
│       └── WindowElements/
└── docs/
    ├── EasyAccess-Development-Guide.html      # 开发指导 v1 (C++ 方案)
    ├── EasyAccess-Development-Guide-v2.html   # 开发指导 v2 (C# 方案)
    └── compose/
        └── plans/
```

## 构建

```bash
# 开发构建（Debug，需要已安装 Windows App Runtime）
dotnet build

# 发布 Self-Contained 单文件（无需安装运行时）
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true
```

**Debug vs Release 模式**：
- **Debug**：依赖本地安装的 Windows App Runtime，用于开发调试
- **Release**：Self-Contained + WindowsAppSDKSelfContained，产物可独立运行

**CMake 辅助工具构建**（utilitys/ 目录）：
```bash
cd utilitys/PrintWindows && cmake -B build && cmake --build build --config Release
cd utilitys/ProbeUia && cmake -B build && cmake --build build --config Release
```

## 依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| Microsoft.WindowsAppSDK | 2.2.0 | WinUI 3 运行时 |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.1839 | Win32/COM 构建工具 |

## 架构

四层单进程架构：

- **UI 层**：OverlayWindow (WinUI 3)、TrayIcon
- **核心逻辑层**：DialogDetector、WhitelistEngine、FolderCollector、Navigator
- **系统层**：WinEventHook (P/Invoke)、UIAutomation (.NET)、COM Shell、SendInput (P/Invoke)
- **基础设施层**：SingleInstance、ConfigManager、Logger

## 关键设计决策

- 直接使用 WinUI 3 Window（非 XAML Islands 混合架构）
- SetWindowLongPtr(GWL_HWNDPARENT) 实现 overlay 所有权关系
- Unpackaged 模式：无需 MSIX 安装，exe 直接运行
- Self-Contained 发布：用户无需安装 .NET 运行时和 Windows App Runtime（~60-80MB）
- 暂不实现开机自启功能
- 白名单 JSON 配置实现应用级对话框检测

## 当前状态

早期开发阶段。已从 WinUI 3 模板创建项目脚手架，MainWindow 使用 Mica 背景，核心模块尚未实现。

已创建 C++ 辅助工具：
- **PrintWindows**：按 F9 打印前台窗口的 Win32 窗口树（句柄、类名、标题、位置）
- **ProbeUia**：按 F9 打印前台窗口的 UI Automation 树（控件类型、Name、AutomationId）

## 文档

详见 `docs/EasyAccess-Development-Guide-v2.html`（C# 版开发指导文档）。
