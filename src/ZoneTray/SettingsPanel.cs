namespace ZoneTray;

internal sealed class SettingsPanel : Form
{
    private readonly Dictionary<string, CheckBox> _checks = new();
    private readonly ComboBox _templateCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly string? _originalType;

    internal bool SettingsChanged { get; private set; }
    internal bool LayoutChanged { get; private set; }

    public SettingsPanel()
    {
        Text = "窗口行为设置 - ZoneTray";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        Padding = new Padding(16);

        var title = new Label
        {
            Text = "FancyZones 窗口行为（修改后需重启生效）",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12),
        };

        var current = SettingsStore.Read();
        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            Dock = DockStyle.Top,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        foreach (var (label, key, _) in SettingsStore.Behaviors)
        {
            var check = new CheckBox
            {
                Text = label,
                Checked = current[key],
                AutoSize = true,
                Margin = new Padding(2, 4, 2, 4),
            };
            _checks[key] = check;
            layout.Controls.Add(check);
        }

        _originalType = LayoutTemplates.ReadCurrentType();
        var currentIndex = Array.FindIndex(LayoutTemplates.All, t => t.Type == _originalType);
        for (var i = 0; i < LayoutTemplates.All.Length; i++)
        {
            _templateCombo.Items.Add(LayoutTemplates.All[i].Label);
        }
        _templateCombo.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;

        var templateGroup = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(2, 12, 2, 4),
            WrapContents = false,
        };
        templateGroup.Controls.Add(new Label
        {
            Text = "分区布局：",
            AutoSize = true,
            Padding = new Padding(0, 8, 6, 0),
        });
        templateGroup.Controls.Add(_templateCombo);

        var pathLabel = new Label
        {
            Text = $"配置文件：{SettingsStore.FilePath}",
            AutoSize = true,
            ForeColor = Color.Gray,
            Margin = new Padding(2, 10, 2, 0),
        };

        var ok = new Button { Text = "应用并重启", DialogResult = DialogResult.OK, Width = 110, Height = 32 };
        ok.Click += OnApply;
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80, Height = 32 };

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 6, 0, 0),
        };
        buttonPanel.Controls.Add(ok);
        buttonPanel.Controls.Add(cancel);

        Controls.Add(buttonPanel);
        Controls.Add(pathLabel);
        Controls.Add(templateGroup);
        Controls.Add(layout);
        Controls.Add(title);

        AcceptButton = ok;
        CancelButton = cancel;
        AutoSize = true;
    }

    private void OnApply(object? sender, EventArgs e)
    {
        var original = SettingsStore.Read();
        var updated = new Dictionary<string, bool>();
        foreach (var (_, key, _) in SettingsStore.Behaviors)
        {
            updated[key] = _checks[key].Checked;
        }

        SettingsChanged = BehaviorsDiffer(original, updated);
        if (SettingsChanged)
        {
            try
            {
                SettingsStore.Write(updated);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"写入配置失败：\n{ex.Message}", "ZoneTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }
        }

        var chosen = LayoutTemplates.All[_templateCombo.SelectedIndex];
        LayoutChanged = chosen.Type != _originalType;
        if (LayoutChanged)
        {
            try
            {
                LayoutTemplates.Apply(chosen);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"写入布局失败：\n{ex.Message}", "ZoneTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }

    private static bool BehaviorsDiffer(IReadOnlyDictionary<string, bool> a, IReadOnlyDictionary<string, bool> b)
    {
        foreach (var (_, key, _) in SettingsStore.Behaviors)
        {
            if (a[key] != b[key])
            {
                return true;
            }
        }
        return false;
    }
}
