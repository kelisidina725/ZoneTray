using System.Diagnostics;

namespace ZoneTray;

internal static class Program
{
    private const string InstanceMutexName =
        @"Local\ZoneTray-Instance-8f3c1d2e-4a5b-4c6d-9e7f-1a2b3c4d5e6f";

    private const string FzExeName = "PowerToys.FancyZones.exe";
    private const string FzSubdir = "src";

    [STAThread]
    private static int Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // 重复启动：实例已在运行，静默退出。
            return 0;
        }

        try
        {
            ApplicationConfiguration.Initialize();

            var rootDir = AppContext.BaseDirectory;
            var fzPath = Path.Combine(rootDir, FzSubdir, FzExeName);
            if (!File.Exists(fzPath))
            {
                MessageBox.Show(
                    $"未找到 FancyZones 二进制：\n{fzPath}\n\n请先运行 build\\GenerateDeployment.ps1 生成部署目录。",
                    "ZoneTray",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            using var tray = new TrayApp(fzPath);
            Application.Run(tray);
            return 0;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
