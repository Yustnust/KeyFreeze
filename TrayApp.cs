using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KeyFreeze;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly InputBlocker _blocker;
    private readonly UnlockDetector _detector;
    private readonly Overlay _overlay;
    private readonly Settings _settings;
    private readonly Watchdog _watchdog;
    private readonly System.Windows.Forms.Timer _idleTimer;
    private readonly SynchronizationContext _sync;
    private volatile bool _locked;

    public TrayApp()
    {
        if (SynchronizationContext.Current == null)
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        _sync = SynchronizationContext.Current!;

        _settings = Settings.Load();
        _blocker = new InputBlocker();
        _detector = new UnlockDetector(_settings);
        _overlay = new Overlay(UnlockHint());
        _watchdog = new Watchdog(_blocker, _settings, () => _locked);

        _blocker.KeyEventReceived += (_, e) => _detector.OnKey(e.VkCode, e.IsDown);
        _detector.UnlockRequested += (_, _) => _sync.Post(_ => Unlock(), null);

        _tray = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "KeyFreeze - 未锁定",
            ContextMenuStrip = BuildMenu()
        };

        _idleTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _idleTimer.Tick += IdleTick;
        _idleTimer.Start();

        _watchdog.Start();

        _tray.ShowBalloonTip(3000, "KeyFreeze 已启动",
            "右键托盘图标可锁定 / 解锁。锁定后输入口令或组合键解锁。", ToolTipIcon.Info);
    }

    private string UnlockHint()
    {
        string combo = "Ctrl+Alt+Shift+" + (char)_settings.ComboKey;
        return $"键盘和鼠标已锁定\n\n输入口令 “{_settings.Passphrase}”\n或 按住 {combo} 约 1.5 秒解锁";
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("锁定", null, (_, _) => Lock()));
        menu.Items.Add(new ToolStripMenuItem("解锁", null, (_, _) => Unlock()));
        menu.Items.Add(new ToolStripMenuItem("设置...", null, (_, _) => ShowSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => Exit()));
        return menu;
    }

    private void Lock()
    {
        if (_locked) return;
        _locked = true;
        _overlay.SetHint(UnlockHint());
        if (_settings.ShowOverlay) _overlay.Show();
        _detector.Start();
        _blocker.Start();
        _tray.Text = "KeyFreeze - 已锁定";
        _tray.ShowBalloonTip(2000, "已锁定", "键盘与鼠标输入已被拦截。", ToolTipIcon.Info);
    }

    private void Unlock()
    {
        if (!_locked) return;
        _locked = false;
        _detector.Stop();
        _blocker.Stop();
        if (_overlay.Visible) _overlay.Hide();
        _tray.Text = "KeyFreeze - 未锁定";
        _tray.ShowBalloonTip(2000, "已解锁", "输入已恢复。", ToolTipIcon.Info);
    }

    private void IdleTick(object? sender, EventArgs e)
    {
        _watchdog.UiHeartbeat();
        if (_locked || !_settings.AutoLockIdle) return;

        var lii = new NativeMethods.LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.LASTINPUTINFO));
        if (NativeMethods.GetLastInputInfo(ref lii))
        {
            uint idle = NativeMethods.GetTickCount() - lii.dwTime;
            if (idle / 1000 >= (uint)_settings.AutoLockIdleMinutes * 60)
                Lock();
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings.Save();
            _overlay.SetHint(UnlockHint());
        }
    }

    private void Exit()
    {
        _watchdog.Stop();
        Unlock();
        _overlay.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        Application.Exit();
    }

    private static Icon LoadAppIcon()
    {
        var asm = typeof(TrayApp).Assembly;
        using var rs = asm.GetManifestResourceStream("KeyFreeze.app.ico")!;
        var ms = new MemoryStream();
        rs.CopyTo(ms);
        ms.Position = 0;
        return new Icon(ms);
    }
}
