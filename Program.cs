using System;
using System.Threading;
using System.Windows.Forms;

namespace KeyFreeze;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    private static void Main()
    {
        _mutex = new Mutex(true, "Local\\KeyFreeze_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("KeyFreeze 已经在运行了。", "KeyFreeze", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());

        _mutex.ReleaseMutex();
    }
}
