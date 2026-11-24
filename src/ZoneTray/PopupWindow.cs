using System.Runtime.InteropServices;

namespace ZoneTray;

/// <summary>
/// 托盘宿主没有主窗口，弹窗以无属主形式创建时拿不到前台权（Windows 前台锁），
/// 窗口会以未激活状态创建、不抬升到 z 序顶层；落在非前台显示器上时会被该屏幕的窗口压住，
/// 又因不进任务栏而无法找回。这里在弹窗存活期间置顶，并在显示后主动抢一次前台。
/// </summary>
internal static class PopupWindow
{
    internal static DialogResult ShowModal(Form dialog)
    {
        // TopMost 窗口固定位于普通窗口之上，不受前台锁限制，跨显示器同样有效。
        dialog.TopMost = true;
        dialog.Shown += (_, _) => BringToForeground(dialog.Handle);
        return dialog.ShowDialog();
    }

    private static void BringToForeground(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var foreground = GetForegroundWindow();
        if (foreground == hwnd)
        {
            return;
        }

        // SetForegroundWindow 只在调用方持有前台权时生效；临时附加到前台线程的输入队列换取一次前台权。
        var foregroundThread = foreground == IntPtr.Zero ? 0u : GetWindowThreadProcessId(foreground, out _);
        var currentThread = GetCurrentThreadId();
        var attached = false;
        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attached = AttachThreadInput(currentThread, foregroundThread, true);
            }
            SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
