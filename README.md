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

```powershell
# 运行（.NET 10 SDK）
dotnet run -c Release

# 发布为单文件 exe
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
# 产物：bin/Release/net10.0-windows/win-x64/publish/KeyFreeze.exe
```

## 安全说明

- 进程崩溃 / 退出时，系统自动移除输入钩子，输入立即恢复。
- 即便锁定，`Ctrl + Alt + Del`（安全注意序列）永远无法被软件拦截，是最后保险。
- 看门狗在界面卡死且锁定状态下会强制恢复并退出，避免被「锁死」。

## 配置

配置保存在 `%APPDATA%\KeyFreeze\config.json`，也可通过托盘菜单 **设置...** 修改：

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

- 语言：C# / .NET 10（Windows Forms，托盘程序）。
- 输入拦截：通过 `SetWindowsHookEx` 安装 `WH_KEYBOARD_LL` / `WH_MOUSE_LL` 低层钩子，在回调中返回非零值吞掉事件。
- 解锁判定：在钩子回调里先判断是否匹配解锁手势，再决定是否放行 / 吞掉。
- 低层钩子随进程退出自动卸载，因此进程崩溃即等于「自动解锁」。
