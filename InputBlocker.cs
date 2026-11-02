using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyFreeze;

internal sealed class InputBlocker : IDisposable
{
    private readonly NativeMethods.HookProc _keyboardProc;
    private readonly NativeMethods.HookProc _mouseProc;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private Thread? _thread;
    private uint _threadId;
    private volatile bool _running;
    private readonly ManualResetEventSlim _ready = new(false);

    public event EventHandler<KeyEvent>? KeyEventReceived;

    public sealed class KeyEvent : EventArgs
    {
        public int VkCode;
        public int Message;
        public bool IsDown;
    }

    public InputBlocker()
    {
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
    }

    public bool IsActive => _running;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(Loop) { IsBackground = true, Name = "InputBlocker" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait(5000);
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        if (_thread != null && _threadId != 0)
        {
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_UNHOOK, UIntPtr.Zero, IntPtr.Zero);
        }
    }

    public void ForceUnhook()
    {
        _running = false;
        if (_keyboardHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
        if (_mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
    }

    private void Loop()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, IntPtr.Zero, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);
        _ready.Set();

        NativeMethods.MSG msg;
        while (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
        {
            if (msg.message == NativeMethods.WM_UNHOOK) break;
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }
        Cleanup();
    }

    private void Cleanup()
    {
        if (_keyboardHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
        if (_mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vk = Marshal.ReadInt32(lParam);
            bool down = (int)wParam is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
            KeyEventReceived?.Invoke(this, new KeyEvent { VkCode = vk, Message = (int)wParam, IsDown = down });
            return (IntPtr)1; // swallow
        }
        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            return (IntPtr)1; // swallow all mouse
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
        _ready.Dispose();
    }
}
