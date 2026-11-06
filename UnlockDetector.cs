using System;
using System.Threading;

namespace KeyFreeze;

internal sealed class UnlockDetector : IDisposable
{
    private readonly Settings _settings;
    private readonly System.Text.StringBuilder _buffer = new();
    private int _lastKeyTicks;
    private int? _comboStart;
    private volatile bool _active;
    private Timer? _comboTimer;

    public event EventHandler? UnlockRequested;

    public UnlockDetector(Settings settings) => _settings = settings;

    public void Start()
    {
        _active = true;
        _buffer.Clear();
        _lastKeyTicks = 0;
        _comboStart = null;
        _comboTimer?.Dispose();
        _comboTimer = new Timer(ComboTick, null, 0, 100);
    }

    public void Stop()
    {
        _active = false;
        _comboTimer?.Dispose();
        _comboTimer = null;
    }

    public void OnKey(int vk, bool isDown)
    {
        if (!_active || !isDown) return;
        CheckPassphrase(vk);
    }

    private void CheckPassphrase(int vk)
    {
        var now = Environment.TickCount;
        if (_lastKeyTicks != 0 && now - _lastKeyTicks > _settings.PassphraseTimeoutMs)
            _buffer.Clear();
        _lastKeyTicks = now;

        var ch = VkToChar(vk);
        if (ch == '\0') return;

        _buffer.Append(ch);
        if (_buffer.Length > _settings.Passphrase.Length)
            _buffer.Remove(0, _buffer.Length - _settings.Passphrase.Length);

        if (_buffer.Length == _settings.Passphrase.Length &&
            string.Equals(_buffer.ToString(), _settings.Passphrase, StringComparison.OrdinalIgnoreCase))
        {
            Trigger();
        }
    }

    private void ComboTick(object? state)
    {
        if (!_active) return;

        bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        bool alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
        bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
        bool key = (NativeMethods.GetAsyncKeyState(_settings.ComboKey) & 0x8000) != 0;

        if (ctrl && alt && shift && key)
        {
            int now = Environment.TickCount;
            if (_comboStart == null) _comboStart = now;
            else if (now - _comboStart.Value >= _settings.ComboHoldMs)
                Trigger();
        }
        else
        {
            _comboStart = null;
        }
    }

    private void Trigger()
    {
        if (!_active) return;
        _active = false;
        UnlockRequested?.Invoke(this, EventArgs.Empty);
    }

    private static char VkToChar(int vk)
    {
        if (vk >= 'A' && vk <= 'Z') return (char)vk;
        if (vk >= '0' && vk <= '9') return (char)vk;
        return '\0';
    }

    public void Dispose() => Stop();
}
