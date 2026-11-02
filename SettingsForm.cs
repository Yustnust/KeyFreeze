using System;
using System.Windows.Forms;

namespace KeyFreeze;

internal sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly TextBox _passphrase;
    private readonly TextBox _comboKey;
    private readonly NumericUpDown _holdMs;
    private readonly CheckBox _autoLock;
    private readonly NumericUpDown _idleMin;
    private readonly CheckBox _overlay;
    private readonly CheckBox _watchdog;
    private readonly TableLayoutPanel _panel;
    private int _row;

    public SettingsForm(Settings settings)
    {
        _settings = settings;
        Text = "KeyFreeze 设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new System.Drawing.Size(380, 320);

        _panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 8,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Padding = new Padding(12)
        };
        _panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _passphrase = new TextBox { Text = _settings.Passphrase, Width = 180 };
        _comboKey = new TextBox { Text = ((char)_settings.ComboKey).ToString(), MaxLength = 1, Width = 40 };
        _holdMs = new NumericUpDown { Minimum = 500, Maximum = 5000, Increment = 100, Value = _settings.ComboHoldMs };
        _autoLock = new CheckBox { Checked = _settings.AutoLockIdle, Text = "闲置自动锁定" };
        _idleMin = new NumericUpDown { Minimum = 1, Maximum = 120, Value = _settings.AutoLockIdleMinutes, Width = 60 };
        _overlay = new CheckBox { Checked = _settings.ShowOverlay, Text = "显示锁定遮罩" };
        _watchdog = new CheckBox { Checked = _settings.EnableWatchdog, Text = "启用看门狗" };

        AddRow("解锁口令：", _passphrase);
        AddRow("组合键字母：", _comboKey);
        AddRow("组合键长按(ms)：", _holdMs);
        AddRow("自动锁定：", _autoLock);
        AddRow("闲置分钟：", _idleMin);
        AddRow("遮罩：", _overlay);
        AddRow("看门狗：", _watchdog);
        Controls.Add(_panel);

        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(100, 262) };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(200, 262) };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AddRow(string label, Control control)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _panel.Controls.Add(lbl);
        _panel.SetCellPosition(lbl, new TableLayoutPanelCellPosition(0, _row));
        _panel.Controls.Add(control);
        _panel.SetCellPosition(control, new TableLayoutPanelCellPosition(1, _row));
        _row++;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(_passphrase.Text))
            {
                MessageBox.Show("口令不能为空。", "KeyFreeze", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
            _settings.Passphrase = _passphrase.Text.Trim();
            var k = _comboKey.Text.Trim();
            if (k.Length == 1) _settings.ComboKey = char.ToUpperInvariant(k[0]);
            _settings.ComboHoldMs = (int)_holdMs.Value;
            _settings.AutoLockIdle = _autoLock.Checked;
            _settings.AutoLockIdleMinutes = (int)_idleMin.Value;
            _settings.ShowOverlay = _overlay.Checked;
            _settings.EnableWatchdog = _watchdog.Checked;
        }
        base.OnFormClosing(e);
    }
}
