# WorkStatusLight / Claude Code 工作状态桌面指示灯

[English](#english) | [中文](#中文)

---

## English

A Windows system tray application that displays a Claude Code work status indicator on your desktop. Shows red/yellow/green lights to reflect the current working state of Claude Code. Controllable via HTTP API, making it easy to integrate with Claude Code hooks, automation tools, scripts, or other applications.

### Display Effects

| State | Display |
|---|---|
| Off | ![default](assets/default.png) |
| Red Solid | ![red](assets/red.png) |
| Yellow Solid | ![yellow](assets/yellow.png) |
| Green Solid | ![green](assets/green.png) |

> Blinking states alternate between the colored light and the off state at 500ms intervals.

### Features

- System tray application with right-click menu
- Work status indicator: Red / Yellow / Green (solid or blinking) to reflect Claude Code's working state
- Draggable floating window, always on top
- Transparent layered window (no background)
- Startup animation: Red -> Yellow -> Green (0.5s each)
- HTTP API on `localhost:8800` for remote control
- Per-monitor DPI aware (High DPI support)

### Requirements

- Windows 10 or later
- .NET 9.0 SDK

### Build & Run

```bash
dotnet build
dotnet run
```

Or publish as a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### HTTP API

The app listens on `http://localhost:8800/`. All responses are JSON with CORS enabled.

| Endpoint | Description |
|---|---|
| `GET /red` | Red solid |
| `GET /red-blink` | Red blinking |
| `GET /yellow` | Yellow solid |
| `GET /yellow-blink` | Yellow blinking |
| `GET /green` | Green solid |
| `GET /green-blink` | Green blinking |
| `GET /off` | Turn off |
| `GET /status` | Query current state |

#### Examples

```bash
# Set red light
curl http://localhost:8800/red

# Set green blinking
curl http://localhost:8800/green-blink

# Query current status
curl http://localhost:8800/status
# => {"status":"ok","light":"green"}
```

#### Response Format

```json
{"status":"ok","light":"red"}
```

#### Claude Code Integration

You can integrate this tool with Claude Code hooks to automatically reflect work status. Configure hooks in your Claude Code `settings.json` to call the API on different events.

**Example: `~/.claude/settings.json`**

> **Note:** The following is a reference example. Adjust the matcher and commands to fit your own workflow.

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/yellow-blink",
            "async": true
          }
        ]
      }
    ],
    "Notification": [
      {
        "matcher": "permission_prompt|elicitation_dialog",
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/red-blink",
            "async": true
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/green",
            "async": true
          }
        ]
      }
    ]
  }
}
```

**Matcher field:** Matches against event-specific values. For `Notification`, it uses a regex pattern to match notification types (e.g. `permission_prompt|elicitation_dialog`). Omit the `matcher` field to match all events of that type.

| Hook Event | Matcher | Light State | Meaning |
|---|---|---|---|
| `UserPromptSubmit` | (none) | Yellow blinking | User submitted a prompt, Claude starting work |
| `Notification` | `permission_prompt\|elicitation_dialog` | Red blinking | Claude needs permission or user input |
| `Stop` | (none) | Green solid | Claude finished, waiting for input |

### Author

**辉越软件 (HuiYue Software)** - [www.nextdreaminc.cn](http://www.nextdreaminc.cn)

---

## 中文

一款 Windows 系统托盘应用，在桌面上显示 Claude Code 工作状态指示灯。通过红/黄/绿灯反映 Claude Code 的当前工作状态。通过 HTTP API 控制，方便与 Claude Code Hooks、自动化工具、脚本或其他应用集成。

### 显示效果

| 状态 | 显示效果 |
|---|---|
| 熄灭 | ![default](assets/default.png) |
| 红灯常亮 | ![red](assets/red.png) |
| 黄灯常亮 | ![yellow](assets/yellow.png) |
| 绿灯常亮 | ![green](assets/green.png) |

> 闪烁状态为彩色灯与熄灭状态以 500ms 间隔交替显示。

### 功能特性

- 系统托盘应用，支持右键菜单
- 工作状态指示灯：红 / 黄 / 绿（常亮或闪烁），反映 Claude Code 的工作状态
- 可拖拽浮动窗口，始终置顶
- 透明分层窗口（无背景）
- 启动动画：红 -> 黄 -> 绿（各 0.5 秒）
- HTTP API 监听 `localhost:8800`，支持远程控制
- 支持 Per-Monitor DPI 感知（高 DPI 适配）

### 系统要求

- Windows 10 或更高版本
- .NET 9.0 SDK

### 构建与运行

```bash
dotnet build
dotnet run
```

或发布为独立可执行文件：

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### HTTP API

应用监听 `http://localhost:8800/`，所有响应均为 JSON 格式，已启用 CORS。

| 接口 | 说明 |
|---|---|
| `GET /red` | 红灯常亮 |
| `GET /red-blink` | 红灯闪烁 |
| `GET /yellow` | 黄灯常亮 |
| `GET /yellow-blink` | 黄灯闪烁 |
| `GET /green` | 绿灯常亮 |
| `GET /green-blink` | 绿灯闪烁 |
| `GET /off` | 全部熄灭 |
| `GET /status` | 查询当前状态 |

#### 示例

```bash
# 设置红灯
curl http://localhost:8800/red

# 设置绿灯闪烁
curl http://localhost:8800/green-blink

# 查询当前状态
curl http://localhost:8800/status
# => {"status":"ok","light":"green"}
```

#### 响应格式

```json
{"status":"ok","light":"red"}
```

#### Claude Code 集成

可以将此工具与 Claude Code Hooks 集成，在不同事件时自动反映工作状态。在 Claude Code 的 `settings.json` 中配置 Hooks，调用 API 设置对应的灯光状态。

**示例：`~/.claude/settings.json`**

> **注意：** 以下为参考示例，请根据自己的实际工作流程调整 matcher 和命令。

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/yellow-blink",
            "async": true
          }
        ]
      }
    ],
    "Notification": [
      {
        "matcher": "permission_prompt|elicitation_dialog",
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/red-blink",
            "async": true
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "curl -s http://localhost:8800/green",
            "async": true
          }
        ]
      }
    ]
  }
}
```

**matcher 字段说明：** 匹配事件相关的特定值。对于 `Notification` 事件，使用正则表达式匹配通知类型（如 `permission_prompt|elicitation_dialog`）。省略 `matcher` 字段则匹配该类型的全部事件。

| Hook 事件 | Matcher | 灯光状态 | 含义 |
|---|---|---|---|
| `UserPromptSubmit` | （无） | 黄灯闪烁 | 用户提交了提示，Claude 开始工作 |
| `Notification` | `permission_prompt\|elicitation_dialog` | 红灯闪烁 | Claude 需要权限确认或用户输入 |
| `Stop` | （无） | 绿灯常亮 | Claude 完成工作，等待输入 |

### 作者

**辉越软件 (HuiYue Software)** - [www.nextdreaminc.cn](http://www.nextdreaminc.cn)
