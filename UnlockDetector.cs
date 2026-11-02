using System;

namespace KeyFreeze;

internal sealed class UnlockDetector : IDisposable
{
    private readonly Settings _settings;
    private readonly System.Text.StringBuilder _buffer = new();
    private long _lastKeyTicks;
    private long? _comboStart;
    private bool _cCtrl, _cAlt, _cShift, _cKey;
    private volatile bool _active;

    public event EventHandler? UnlockRequested;

    public UnlockDetector(Settings settings) => _settings = settings;

    public void Start()
    {
        _active = true;
        _buffer.Clear();
        _lastKeyTicks = 0;
        _comboStart = null;
        _cCtrl = _cAlt = _cShift = _cKey = false;
    }

    public void Stop() => _active = false;

    public void OnKey(int vk, bool isDown)
    {
        if (!_active) return;
        UpdateCombo(vk, isDown);
        if (isDown) CheckPassphrase(vk);
    }

    private void CheckPassphrase(int vk)
    {
        var now = Environment.TickCount64;
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

    private void UpdateCombo(int vk, bool down)
    {
        if (vk == NativeMethods.VK_CONTROL) _cCtrl = down;
        else if (vk == NativeMethods.VK_MENU) _cAlt = down;
        else if (vk == NativeMethods.VK_SHIFT) _cShift = down;
        else if (vk == _settings.ComboKey) _cKey = down;

        if (_cCtrl && _cAlt && _cShift && _cKey)
        {
            if (_comboStart == null) _comboStart = Environment.TickCount64;
            else if (Environment.TickCount64 - _comboStart.Value >= _settings.ComboHoldMs)
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
