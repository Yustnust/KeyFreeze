using System;
using System.IO;
using System.Threading;

namespace KeyFreeze;

internal sealed class Watchdog : IDisposable
{
    private readonly InputBlocker _blocker;
    private readonly Settings _settings;
    private readonly Func<bool> _isLocked;
    private System.Threading.Timer? _timer;
    private int _uiHeartbeat = Environment.TickCount;

    private static readonly string LogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyFreeze", "watchdog.log");

    public Watchdog(InputBlocker blocker, Settings settings, Func<bool> isLocked)
    {
        _blocker = blocker;
        _settings = settings;
        _isLocked = isLocked;
    }

    public void UiHeartbeat() => _uiHeartbeat = Environment.TickCount;

    public void Start()
    {
        if (!_settings.EnableWatchdog) return;
        UiHeartbeat();
        _timer = new System.Threading.Timer(Tick, null, 2000, 2000);
    }

    private void Tick(object? state)
    {
        if (!_settings.EnableWatchdog) return;
        if (!_isLocked()) return;

        if (Environment.TickCount - _uiHeartbeat > 20000)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:u} Watchdog: 检测到 UI 卡死且处于锁定状态，强制恢复输入并退出。\n");
            }
            catch { }
            _blocker.ForceUnhook();
            Environment.Exit(3);
        }
    }

    public void Stop() => _timer?.Dispose();

    public void Dispose() => Stop();
}
