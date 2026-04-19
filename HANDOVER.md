# PersonalAssistant 项目交接文档

## 项目概述

一个 .NET 8.0 Windows 托盘程序，集成**排班制番茄钟**、待办事项、每日日志三大功能。现代化卡片式UI设计。

## 当前状态：全部开发完成，构建通过，可运行

- 构建命令：`taskkill /IM PersonalAssistant.exe /F 2>nul; cd D:/Github/clock/PersonalAssistant && dotnet build 2>&1`
- 运行命令：`D:/Github/clock/PersonalAssistant/bin/Debug/net8.0-windows/PersonalAssistant.exe`
- 输出路径：`D:/Github/clock/PersonalAssistant/bin/Debug/net8.0-windows/PersonalAssistant.exe`
- 设置存储：`%AppData%/PersonalAssistant/settings.json`
- 数据库：`%AppData%/PersonalAssistant/data.db`
- 截图目录：`D:/Github/clock/PersonalAssistant/png/`

## 项目结构

```
D:/Github/clock/
├── PersonalAssistant_SOP.md          ← 原始需求文档（中文）
├── HANDOVER.md                        ← 本文件
├── PersonalAssistant/
│   ├── PersonalAssistant.csproj       ← .NET 8.0, WPF+WinForms, SQLite, 图标
│   ├── App.xaml                       ← 完整现代化主题：颜色、按钮、TextBox、ComboBox、CheckBox、ListBox、TabRadioStyle
│   ├── App.xaml.cs                    ← 启动入口：Mutex单实例、托盘图标、悬浮窗、MainWindow单例、RestOverlay管理
│   ├── MainWindow.xaml/.cs            ← 三Tab主面板（RadioButton风格Tab），值转换器，复选框事件处理
│   ├── Assets/
│   │   └── app.ico                    ← 橙红渐变圆形+白色P字母
│   ├── Core/
│   │   ├── AppTimer.cs                ← 计时器引擎：支持自由模式和排班模式，2秒轮询ScheduleWatcher
│   │   ├── ScheduleEngine.cs          ← 排班引擎：BuildTodayPlan/FillBlocks/FindCurrentBlock/GetGapStatus
│   │   ├── DatabaseService.cs         ← SQLite CRUD（todos/logs/sessions三张表）
│   │   ├── NotificationService.cs     ← 气泡通知+系统音效
│   │   └── SettingsService.cs         ← JSON配置读写
│   ├── Models/
│   │   ├── TimerSession.cs            ← TimerPhase枚举 + PhaseChangedEventArgs + BlockType枚举
│   │   ├── WorkSchedule.cs            ← TimeSlot（Start/End/Enabled/Label）+ WorkBlock + WorkSchedule
│   │   ├── AppSettings.cs             ← 含 WorkSchedule? Schedule 属性
│   │   ├── TodoItem.cs
│   │   └── LogEntry.cs
│   ├── ViewModels/
│   │   ├── PomodoroViewModel.cs       ← 含 RelayCommand 类，排班模式状态显示
│   │   ├── TodoViewModel.cs           ← 含 RelayCommand<T> 类，ToggleDone为public
│   │   └── LogViewModel.cs            ← 含 LogGroup 类
│   ├── Views/
│   │   ├── FloatingWindow.xaml/.cs    ← 透明悬浮窗，白色倒计时+黑色描边，PhaseLabel显示排班阶段
│   │   ├── RestOverlayWindow.xaml/.cs ← 强制休息全屏遮罩
│   │   └── SettingsWindow.xaml/.cs    ← 双Tab设置面板（通用+工作时间），程序化构建时段行
│   ├── Helpers/
│   │   └── WindowHelper.cs            ← Win32 API（鼠标穿透/置顶）
│   └── png/                           ← 用户截图目录（MCP分析用）
```

## 已实现功能

### 核心功能
1. **排班制番茄钟**：按时间段自动运行，默认上午9:30-12:00、下午14:00-18:30，自动填充专注/休息块
2. **自由模式番茄钟**：手动开始/暂停/继续/跳过/重置
3. **悬浮窗**：透明背景，右上角显示，白色倒计时+黑色描边，排班模式显示阶段名
4. **强制休息遮罩**：全屏半透明灰色，休息期间不可关闭
5. **喝水提醒**：每30min气泡通知（可配置）
6. **待办事项**：添加/勾选完成/删除，优先级（低/中/高）带红蓝绿圆点标记
7. **每日日志**：手动+自动记录，按日期分组，支持导出TXT
8. **设置面板**：双Tab布局（通用/工作时间），支持专注/休息时长、排班时段管理、今日预览
9. **单实例**：Mutex防重复启动 + MainWindow单例（不会开多个主窗口）
10. **托盘图标**：右键菜单（暂停/继续/跳过/主面板/设置/退出）

### UI设计
- **颜色系统**：主色 #FF6B6B，强调色 #4ECDC4，成功 #4CAF50，文字 #2C3E50/#7F8C8D
- **自定义样式**：所有控件完全重写模板（Button/TextBox/ComboBox/CheckBox/ListBox/RadioButton/Slider）
- **ComboBox**：圆角8px，自定义下拉弹窗，hover高亮
- **TextBox**：圆角8px，聚焦边框变红，Margin="8,4"确保文字不裁剪
- **Tab导航**：RadioButton实现的药丸形Tab，无焦点虚线框（FocusVisualStyle={x:Null}）

## 已解决的技术问题

### 1. WPF+WinForms命名空间冲突
`UseWindowsForms=true` 导致类型歧义。
**解决**：全部使用完全限定名（如 `System.Windows.Controls.RadioButton`），或用别名 `using WpfOrientation = System.Windows.Controls.Orientation`

### 2. ComboBox初始化顺序
`SelectedItem` 在 `ItemsSource` 之前设置会导致空选中。
**解决**：先设 `ItemsSource = list`，再设 `SelectedIndex = list.IndexOf(X)`

### 3. MainWindow单例
每次点托盘"打开主面板"会创建新窗口。
**解决**：App.xaml.cs 保留 `_mainWindow` 引用，Show/Hide复用。`ForceClose()` 方法用于退出时真正关闭。

### 4. 按钮不可点击
RelayCommand 的 CanExecute 谓词检查输入文本，但属性变化时未触发 RaiseCanExecuteChanged。
**解决**：在 NewTitle/NewContent 的 setter 中调用 `AddCommand.RaiseCanExecuteChanged()`

### 5. CheckBox勾选冲突
IsChecked绑定 + Command 同时使用导致状态被翻转两次。
**解决**：移除Command和IsChecked绑定，改用 Click 事件 + code-behind 调用 `Todo.ToggleDone()`

### 6. TextBox文字裁剪
PART_ContentHost 的 VerticalAlignment="Center" 或 Padding 过小导致文字被裁剪。
**解决**：使用 `Margin="8,4"` 固定边距，VerticalAlignment 绑定 VerticalContentAlignment

### 7. 其他已解决
- SQLite DLL未复制 → `CopyLocalLockFileAssemblies=true`
- 图标缺失 → PowerShell生成
- 排班模式编码问题(┘┐字符) → ComboBox初始化顺序修复

## csproj 关键配置

```xml
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
<ApplicationIcon>Assets\app.ico</ApplicationIcon>
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>false</SelfContained>
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
```

## MCP 配置

MiniMax MCP 已配置，包含 web_search 和 understand_image 工具。截图分析使用 `mcp_MiniMax_understand_image`。

## 关键文件行号参考

| 文件 | 行号 | 内容 |
|------|------|------|
| App.xaml | 1-30 | 颜色系统定义 |
| App.xaml | 40-125 | 按钮样式（Base/Primary/Danger + Padding修复） |
| App.xaml | 127-155 | TextBox样式（Margin="8,4"修复裁剪） |
| App.xaml | 157-220 | ComboBox完整自定义样式（圆角+Popup） |
| App.xaml | 270+ | TabRadioStyle（FocusVisualStyle={x:Null}） |
| App.xaml.cs | 11 | Mutex单实例 |
| App.xaml.cs | 89-100 | ShowMainWindow单例模式 |
| App.xaml.cs | 140-152 | ExitApp + ForceClose |
| MainWindow.xaml | 144-165 | 待办输入行（Grid布局+优先级圆点） |
| MainWindow.xaml | 169-196 | 待办列表（CheckBox Click事件） |
| MainWindow.xaml.cs | 54-62 | Tab_Click + TodoCheckBox_Clicked |
| SettingsWindow.xaml | 73-107 | 排班页（ScrollViewer+Padding） |
| SettingsWindow.xaml.cs | 75-187 | BuildSlotRow（程序化构建时段UI） |
| Core/AppTimer.cs | 38-80 | ScheduleWatcher + InitializeScheduledMode |
| Core/ScheduleEngine.cs | 62-113 | GetGapStatus（午休/结束/等待） |
| ViewModels/TodoViewModel.cs | 22-25 | NewTitle setter调用RaiseCanExecuteChanged |

## 已知可改进项

- 图标分辨率较低（32x32），可替换为专业设计的多尺寸ico
- 通知使用 `NotifyIcon.ShowBalloonTip`，可升级为 WinRT Toast
- 没有单元测试
- 排班模式下番茄钟页面的视觉反馈可增强（如进度条）
- 设置窗口排班预览可更直观（时间轴图形化）
