using System;
using System.Threading;

namespace KeyFreeze;

internal sealed class UnlockDetector : IDisposable
{
    private readonly Settings _settings;
    private readonly System.Text.StringBuilder _buffer = new();
    private readonly object _lock = new object();

    private int _lastKeyTicks;
    private int? _comboStart;
    private bool _comboArmed;
    private volatile bool _active;
    private Timer? _comboTimer;

    // 按键状态完全由钩子回调维护：不依赖 GetAsyncKeyState（钩子吞掉输入后它读不到状态）。
    private volatile bool _cCtrl, _cAlt, _cShift, _cKey;

    public event EventHandler? UnlockRequested;

    public UnlockDetector(Settings settings) => _settings = settings;

    public void Start()
    {
        _active = true;
        _buffer.Clear();
        _lastKeyTicks = 0;
        _comboStart = null;
        _comboArmed = false;
        _cCtrl = _cAlt = _cShift = _cKey = false;
        _comboTimer?.Dispose();
        _comboTimer = new Timer(ComboTick, null, 0, 100);
        Diagnostics.Log($"DETECTOR START comboKey=0x{_settings.ComboKey:X2} holdMs={_settings.ComboHoldMs}");
    }

    public void Stop()
    {
        _active = false;
        _comboTimer?.Dispose();
        _comboTimer = null;
        Diagnostics.Log("DETECTOR STOP");
    }

    public void OnKey(int vk, bool isDown)
    {
        if (!_active) return;
        TrackComboKey(vk, isDown);
        if (isDown) CheckPassphrase(vk);
    }

    private void TrackComboKey(int vk, bool down)
    {
        // 低层钩子对左右修饰键返回 0xA0~0xA5，而不是 VK_SHIFT/CONTROL/MENU (0x10~0x12)
        if (vk == 0x11 || vk == 0xA2 || vk == 0xA3) _cCtrl = down;
        else if (vk == 0x12 || vk == 0xA4 || vk == 0xA5) _cAlt = down;
        else if (vk == 0x10 || vk == 0xA0 || vk == 0xA1) _cShift = down;
        else if (vk == _settings.ComboKey) _cKey = down;
    }

    private void ComboTick(object? state)
    {
        if (!_active) return;
        lock (_lock)
        {
            bool all = _cCtrl && _cAlt && _cShift && _cKey;

            if (all && !_comboArmed)
            {
                _comboArmed = true;
                Diagnostics.Log("COMBO all keys down");
            }
            else if (!all && _comboArmed)
            {
                _comboArmed = false;
                Diagnostics.Log("COMBO released");
            }

            if (all)
            {
                int now = Environment.TickCount;
                if (_comboStart == null) _comboStart = now;
                else if (now - _comboStart.Value >= _settings.ComboHoldMs)
                    Trigger("combo");
            }
            else
            {
                _comboStart = null;
            }
        }
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
            Trigger("passphrase");
        }
    }

    private void Trigger(string reason)
    {
        if (!_active) return;
        _active = false;
        Diagnostics.Log("TRIGGER unlock via " + reason);
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
