using System.Drawing;
using System.Windows.Forms;

namespace KeyFreeze;

internal sealed class Overlay : Form
{
    private readonly Label _label;

    public Overlay(string hint)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.82;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        ControlBox = false;

        _label = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 22, FontStyle.Bold),
            Text = hint
        };
        Controls.Add(_label);
    }

    public void SetHint(string text) => _label.Text = text;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80000 /*WS_EX_LAYERED*/ | 0x08000000 /*WS_EX_NOACTIVATE*/ | 0x20 /*WS_EX_TRANSPARENT*/;
            return cp;
        }
    }
}
