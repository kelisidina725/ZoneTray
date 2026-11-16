using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ZoneTray;

internal static class AppIcon
{
    internal static Icon Load()
    {
        var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("ZoneTray.ZoneTray.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}

internal sealed class TrayApp : ApplicationContext
{
    private readonly string _fzPath;
    private readonly NotifyIcon _icon;
    private ToolStripMenuItem _autostartItem = null!;
    private Process? _fzProcess;
    private bool _exiting;

    public TrayApp(string fzPath)
    {
        _fzPath = fzPath;
        _icon = new NotifyIcon
        {
            Icon = AppIcon.Load(),
            Visible = true,
            Text = "ZoneTray - 窗口区域管理",
            ContextMenuStrip = BuildMenu(),
        };
        _icon.MouseClick += OnIconMouseClick;
        StartFancyZones();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("窗口行为与布局设置...", image: null, (_, _) => ShowSettings());
        _autostartItem = new ToolStripMenuItem("开机自启")
        {
            CheckOnClick = false,
        };
        _autostartItem.Click += (_, _) => ToggleAutostart();
        menu.Items.Add(_autostartItem);
        menu.Opening += (_, _) => _autostartItem.Checked = IsAutostartEnabled();
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", image: null, (_, _) => ExitApp());
        return menu;
    }

    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ZoneTray.lnk");

    private static bool IsAutostartEnabled() => File.Exists(ShortcutPath);

    private void ToggleAutostart()
    {
        try
        {
            if (IsAutostartEnabled())
            {
                File.Delete(ShortcutPath);
                MessageBox.Show("已关闭开机自启。", "ZoneTray",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("无法创建 WScript.Shell COM 组件");
            object shell = Activator.CreateInstance(shellType)!;
            try
            {
                object shortcut = shell.GetType().InvokeMember(
                    "CreateShortcut", BindingFlags.InvokeMethod, null, shell,
                    new object[] { ShortcutPath })!;
                try
                {
                    var flags = BindingFlags.SetProperty;
                    shortcut.GetType().InvokeMember("TargetPath", flags, null, shortcut,
                        new object[] { Application.ExecutablePath });
                    shortcut.GetType().InvokeMember("WorkingDirectory", flags, null, shortcut,
                        new object[] { AppContext.BaseDirectory });
                    shortcut.GetType().InvokeMember("Description", flags, null, shortcut,
                        new object[] { "ZoneTray - 窗口区域管理" });
                    shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                }
                finally
                {
                    Marshal.ReleaseComObject(shortcut);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
            MessageBox.Show("已开启开机自启。", "ZoneTray",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError($"设置开机自启失败：\n{ex.Message}");
        }
    }

    private void OnIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        using var panel = new SettingsPanel();
        if (panel.ShowDialog() == DialogResult.OK && (panel.SettingsChanged || panel.LayoutChanged))
        {
            // 官方约定：设置/布局变更后需重启 FancyZones 才能生效。
            RestartFancyZones();
        }
    }

    private void StartFancyZones()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _fzPath,
                WorkingDirectory = Path.GetDirectoryName(_fzPath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _fzProcess = Process.Start(psi);
        }
        catch (Exception ex)
        {
            ShowError($"启动 FancyZones 失败：\n{ex.Message}");
        }
    }

    private void RestartFancyZones()
    {
        StopFancyZones();
        System.Threading.Thread.Sleep(300);
        StartFancyZones();
    }

    private void ExitApp()
    {
        if (_exiting)
        {
            return;
        }
        _exiting = true;
        StopFancyZones();
        _icon.Visible = false;
        _icon.Dispose();
        ExitThread();
    }

    private void StopFancyZones()
    {
        try
        {
            if (_fzProcess is { HasExited: false })
            {
                _fzProcess.Kill();
                _fzProcess.WaitForExit(2000);
            }
        }
        catch
        {
            // 进程可能已退出，忽略。
        }

        foreach (var p in Process.GetProcessesByName("PowerToys.FancyZones"))
        {
            try
            {
                p.Kill();
            }
            catch
            {
                // 忽略无权终止的进程。
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "ZoneTray", MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopFancyZones();
            _icon.Dispose();
            _fzProcess?.Dispose();
        }
        base.Dispose(disposing);
    }
}
