# PersonalAssistant 开发SOP

> 目标：一个 .NET 8.0 Windows 托盘程序，集成番茄钟悬浮窗、待办事项、每日日志三大功能。

---

## 一、项目基础规范

| 项目 | 规范 |
|------|------|
| 框架 | .NET 8.0 Windows（`net8.0-windows`） |
| UI | WPF（悬浮窗 + 设置窗口） |
| 托盘 | `System.Windows.Forms.NotifyIcon`（引用 WinForms） |
| 存储 | `Microsoft.Data.Sqlite`（SQLite 本地库） |
| 计时器 | `System.Timers.Timer`（后台线程安全） |
| 打包 | `PublishSingleFile=true` + `SelfContained=false` |
| 输出目录 | 不产生多余DLL（`CopyLocalLockFileAssemblies=false`） |
| 命名空间 | `PersonalAssistant.*` |

### 1.1 .csproj 关键配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
  </ItemGroup>
</Project>
```

### 1.2 解决方案结构

```
PersonalAssistant/
├── App.xaml / App.xaml.cs          ← 启动入口，托盘初始化
├── Assets/
│   └── app.ico
├── Core/
│   ├── AppTimer.cs                 ← 计时器引擎
│   ├── NotificationService.cs      ← 通知/音效
│   └── DatabaseService.cs          ← SQLite 数据访问
├── Models/
│   ├── TimerSession.cs
│   ├── TodoItem.cs
│   └── LogEntry.cs
├── ViewModels/
│   ├── PomodoroViewModel.cs
│   ├── TodoViewModel.cs
│   └── LogViewModel.cs
├── Views/
│   ├── FloatingWindow.xaml         ← 悬浮倒计时窗口
│   ├── MainWindow.xaml             ← 主面板（待办+日志）
│   └── SettingsWindow.xaml
└── Helpers/
    └── WindowHelper.cs             ← 置顶、鼠标穿透等
```

---

## 二、Phase 1：番茄钟 + 悬浮窗（核心）

### 2.1 功能说明

- 默认循环：**专注 45 分钟 → 强制休息 10 分钟**（可在设置中调整）
- 休息时：自动弹出全屏遮罩或置顶悬浮提示，阻断操作
- 喝水提醒：每 30 分钟（可配置）发送系统通知 + 声音
- 悬浮窗：始终置顶，透明度可调，显示剩余时间和当前阶段
- 右键托盘菜单：暂停/继续、跳过当前阶段、打开主面板、退出

### 2.2 状态机设计

```
[空闲] → 开始 → [专注中] → 时间到 → [强制休息] → 休息结束 → [专注中]
              ↑                                              ↓
              └──────────────── 循环 ─────────────────────┘

任意状态 → 暂停 → [已暂停] → 继续 → 原状态
```

### 2.3 核心类：AppTimer.cs

```csharp
// 关键属性
public TimerPhase CurrentPhase { get; }  // Focus / Break
public TimeSpan Remaining { get; }
public int CompletedCycles { get; }

// 关键事件
public event EventHandler<PhaseChangedEventArgs> PhaseChanged;
public event EventHandler Tick;               // 每秒触发
public event EventHandler WaterReminderDue;   // 喝水提醒

// 关键方法
public void Start() / Pause() / Resume() / Skip() / Reset()
```

### 2.4 悬浮窗 FloatingWindow.xaml 要求

- `WindowStyle="None"` + `AllowsTransparency="True"` + `Topmost="True"`
- 显示：大字体倒计时 + 当前阶段文字（专注中 / 休息中）
- 支持鼠标拖动移动位置
- 休息阶段时窗口变色（红底提示）
- 最小化后恢复时记住位置

### 2.5 强制休息实现

```
休息开始时：
  1. 弹出 RestOverlayWindow（全屏半透明遮罩）
  2. 遮罩上显示倒计时 + "休息中，请离开屏幕"
  3. 禁止关闭（CanClose = false）
  4. 休息结束自动关闭遮罩
```

---

## 三、Phase 2：待办事项 + 每日日志

### 3.1 待办功能要求

- 添加待办：标题 + 优先级（高/中/低）+ 可选截止日期
- 显示：今日待办列表，已完成可折叠
- 操作：勾选完成、删除、编辑
- 自动将"今日完成"的待办追加到日志

### 3.2 每日日志要求

- 手动写入：用户输入一条记录 → 带时间戳保存
- 自动写入：每次番茄钟完成 → 记录"完成第N个番茄钟"
- 自动写入：待办完成 → 记录"完成待办：XXX"
- 按日期分组显示
- 支持导出为 TXT 文件（日期命名）

### 3.3 数据库表结构（SQLite）

```sql
-- 待办
CREATE TABLE todos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    priority INTEGER DEFAULT 1,   -- 1=低 2=中 3=高
    due_date TEXT,
    is_done INTEGER DEFAULT 0,
    created_at TEXT NOT NULL,
    done_at TEXT
);

-- 日志
CREATE TABLE logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    date TEXT NOT NULL,           -- yyyy-MM-dd
    time TEXT NOT NULL,           -- HH:mm:ss
    content TEXT NOT NULL,
    source TEXT DEFAULT 'manual'  -- manual / pomodoro / todo
);

-- 番茄钟会话（可选，用于统计）
CREATE TABLE sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    start_at TEXT,
    end_at TEXT,
    phase TEXT,       -- focus / break
    completed INTEGER DEFAULT 0
);
```

---

## 四、Phase 3：通知与用户体验

### 4.1 通知方式

| 场景 | 方式 |
|------|------|
| 专注结束 | 系统通知 + 声音 + 悬浮窗变色 |
| 休息结束 | 系统通知 + 声音 + 全屏遮罩关闭 |
| 喝水提醒 | 系统通知（toast）+ 可关闭 |
| 待办到期 | 系统通知 |

使用 `Windows.UI.Notifications`（WinRT Toast）或 `NotifyIcon.ShowBalloonTip` 作为降级方案。

### 4.2 设置项（本地 JSON 存储）

```json
{
  "FocusMinutes": 45,
  "BreakMinutes": 10,
  "WaterReminderMinutes": 30,
  "EnableSound": true,
  "EnableWaterReminder": true,
  "FloatWindowOpacity": 0.85,
  "AutoStartWithWindows": false
}
```

---

## 五、开发顺序（推荐）

```
Step 1  搭骨架
        ├── 创建项目，配置 .csproj
        ├── App.xaml.cs 实现托盘图标 + 右键菜单
        └── DatabaseService.cs 初始化 SQLite 并建表

Step 2  实现计时器引擎
        ├── AppTimer.cs（状态机 + 事件）
        └── 单元测试验证状态切换

Step 3  悬浮窗
        ├── FloatingWindow.xaml（UI）
        ├── 绑定 PomodoroViewModel
        └── 拖动 + 置顶 + 透明度

Step 4  强制休息遮罩
        └── RestOverlayWindow.xaml

Step 5  通知服务
        └── NotificationService.cs

Step 6  主面板 UI
        ├── 待办列表（TodoViewModel）
        └── 日志列表（LogViewModel）

Step 7  设置窗口
        └── 读写 settings.json

Step 8  开机自启 + 打包
        └── 注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

---

## 六、AI 编码指令模板

每次让 GLM/MiniMax 编码时，在提示词开头附加：

> 项目：PersonalAssistant，.NET 8.0-windows，WPF + WinForms 混合。
> 语言：C#。不要解释原理，直接给出完整代码。
> 注意：不使用 AutoCAD 相关依赖，不添加 `Private=true` 的外部DLL引用。

---

## 七、注意事项

1. **Timer 线程安全**：`System.Timers.Timer` 回调在线程池，更新 UI 须用 `Dispatcher.Invoke`
2. **单实例**：用 `Mutex` 防止重复启动
3. **托盘退出**：`Application.Exit()` 前必须 `NotifyIcon.Dispose()`，否则图标残留
4. **SQLite 路径**：存放在 `%AppData%\PersonalAssistant\data.db`
5. **全屏遮罩**：`WindowState=Maximized` + `WindowStyle=None`，多显示器需处理 `Screen.AllScreens`
