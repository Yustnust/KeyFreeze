# KeyFreeze

防止宠物猫（或其他意外）在电脑运行时乱按键盘、乱动鼠标的输入锁定工具。

当你离开电脑、让程序挂着运行时，猫踩键盘、拍鼠标可能误触发邮件、删文件、关闭窗口。KeyFreeze 会拦截所有键鼠输入，直到你用专属手势解锁。

## 功能特点

- **全输入拦截**：锁定期间屏蔽键盘（含 Win 键、Alt+Tab）与鼠标（移动 / 按键 / 滚轮）。
- **双解锁手势**（锁定期间键盘鼠标全屏蔽，解锁逻辑内置于拦截层）：
  - 输入解锁口令（默认 `letmein`，可改），连打即解锁；
  - 或按住 `Ctrl + Alt + Shift + L` 约 1.5 秒解锁。
  - 两者任一触发即解锁，互为备份。
- **闲置自动锁定**：默认离开 5 分钟无操作自动锁定（可在设置中调整或关闭）。
- **锁定遮罩**：全屏半透明提示「已锁定」与解锁方式。
- **看门狗**：检测到界面卡死且处于锁定态时，强制恢复输入并退出进程。
- **崩溃自愈**：进程退出时系统自动卸载钩子，输入自动恢复。

## 使用方法

1. 启动后右下角出现托盘图标。
2. 右键托盘图标 → **锁定**。
3. 解锁：在键盘上输入口令，或按住组合键 1.5 秒。
4. 设置、退出同样在托盘右键菜单。

> 启动需要管理员权限（`app.manifest` 已设为 `requireAdministrator`），以便覆盖高权限窗口的输入。

## 构建与发布

工程为**多目标**：`net40`（Windows 7 等老系统）与 `net10.0-windows`（Windows 10 / 11）。两者共用同一份源码，一次编译同时产出，图标均已内嵌。开发机只需 .NET 10 SDK。

```powershell
# 两个目标一起构建
dotnet build -c Release
#   bin\Release\net40\KeyFreeze.exe                        -> Windows 7 用（单 exe）
#   bin\Release\net10.0-windows\KeyFreeze_Win10.exe       -> Windows 10/11 用（需 .NET 10 桌面运行时）

# 为 Windows 10/11 生成「自带运行时」的单文件 exe（无需目标机装 .NET）
dotnet publish -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true
#   bin\Release\net10.0-windows\win-x64\publish\KeyFreeze_Win10.exe  （约 110MB）
```

> **Windows 7**：使用 `net40` 版本，需先安装一次 **.NET Framework 4.0**（独立安装包，约 40MB，支持 Win7 RTM，**无需 SP1**）。
> **Windows 8 / 10 / 11**：系统已内置 .NET Framework 4.x，`net40` 版本同样可直接运行；也可选用 `net10.0-windows` 单文件版。
> 注意：`.NET 10` 版本**不能**在 Windows 7 上运行（运行时依赖 Win10 的 UCRT）。

## 安全说明

- 进程崩溃 / 退出时，系统自动移除输入钩子，输入立即恢复。
- 即便锁定，`Ctrl + Alt + Del`（安全注意序列）永远无法被软件拦截，是最后保险。
- 看门狗在界面卡死且锁定状态下会强制恢复并退出，避免被「锁死」。

## 配置

配置保存在 `%APPDATA%\KeyFreeze\config.cfg`（简单 `Key=Value` 文本），也可通过托盘菜单 **设置...** 修改：

| 项 | 默认 | 说明 |
|---|---|---|
| Passphrase | `letmein` | 解锁口令（建议改掉） |
| ComboKey | `L` | 组合键长按的字母键 |
| ComboHoldMs | `1500` | 组合键需持续按住毫秒 |
| AutoLockIdle | `true` | 闲置自动锁定开关 |
| AutoLockIdleMinutes | `5` | 闲置多少分钟触发锁定 |
| ShowOverlay | `true` | 显示锁定遮罩 |
| EnableWatchdog | `true` | 启用看门狗 |

## 技术实现

- 语言：C# / Windows Forms，多目标编译 `net40` + `net10.0-windows`（Win7 用前者，Win10/11 两者皆可）。
- 输入拦截：通过 `SetWindowsHookEx` 安装 `WH_KEYBOARD_LL` / `WH_MOUSE_LL` 低层钩子，在回调中返回非零值吞掉事件。
- 解锁判定：口令由钩子回调内的按键序列匹配；组合键由 `GetAsyncKeyState` 定时轮询物理按键状态并计时长按（不依赖键事件，钩子吞掉消息也不影响）。
- 低层钩子随进程退出自动卸载，因此进程崩溃即等于「自动解锁」。
