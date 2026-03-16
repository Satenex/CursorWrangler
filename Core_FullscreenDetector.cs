using System;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Management;

namespace CursorWrangler
{
    public class FullscreenDetector
    {
        public IntPtr GetActiveHwnd()
        {
            return NativeMethods.GetForegroundWindow();
        }

        public WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            WindowInfo info = new WindowInfo();

            if (hwnd == IntPtr.Zero)
                return info;

            info.Hwnd = hwnd;

            // RECT
            NativeMethods.GetWindowRect(hwnd, out RECT rect);
            info.Rect = rect;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Screen screen = Screen.FromHandle(hwnd);
            Rectangle bounds = screen.Bounds;

            int widthDiff = Math.Abs(width - bounds.Width);
            int heightDiff = Math.Abs(height - bounds.Height);

            // WINDOW STATE
            bool minimized = NativeMethods.IsIconic(hwnd);
            bool visible = NativeMethods.IsWindowVisible(hwnd);

            // STYLE
            long style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            long exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            bool hasBorder =
                (style & NativeMethods.WS_BORDER) != 0 ||
                (style & NativeMethods.WS_CAPTION) != 0;

            bool toolWindow =
                (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;

            // CLASS
            StringBuilder className = new StringBuilder(256);
            NativeMethods.GetClassName(hwnd, className, 256);
            info.ClassName = className.ToString();

            // PID
            uint pid;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
            info.ProcessId = (int)pid;

            // PROCESS
            try
            {
                Process proc = Process.GetProcessById(info.ProcessId);

                info.ProcessName = proc.ProcessName;
                info.ProcessBitness = GetProcessBitness(proc);

                try
                {
                    info.ProcessPath = proc.MainModule.FileName;
                }
                catch
                {
                    info.ProcessPath = GetProcessPathWMI(info.ProcessId);
                }
            }
            catch
            {
                info.ProcessName = "";
                info.ProcessPath = "";
                info.ProcessBitness = "";
            }

            // TITLE
            StringBuilder title = new StringBuilder(512);
            NativeMethods.GetWindowText(hwnd, title, title.Capacity);
            info.Title = title.ToString();

            // TOOLWINDOW FLAG
            info.IsToolWindow = toolWindow;

            // IGNORED CLASS
            info.IgnoredClass =
                ConfigManager.WindowsClassNotDetected.Contains(info.ClassName);

            // MONITOR COVERAGE
            bool coversMonitor =
                widthDiff <= 20 &&
                heightDiff <= 20;

            // IGNORE OUR OWN PROCESS
            bool isSelf =
                info.ProcessId == Process.GetCurrentProcess().Id;

            // FINAL FULLSCREEN CHECK
            info.IsFullscreen =
                !minimized &&
                visible &&
                !toolWindow &&
                !hasBorder &&
                !isSelf &&
                !info.IgnoredClass &&
                coversMonitor;

            return info;
        }

        public static string GetProcessPathWMI(int pid)
        {
            try
            {
                using (var searcher =
                    new ManagementObjectSearcher(
                    "SELECT ExecutablePath FROM Win32_Process WHERE ProcessId=" + pid))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["ExecutablePath"]?.ToString() ?? "";
                    }
                }
            }
            catch { }

            return "";
        }

        public static string GetProcessBitness(Process proc)
        {
            try
            {
                if (!Environment.Is64BitOperatingSystem)
                    return "x86";

                bool isWow64;

                if (!NativeMethods.IsWow64Process(proc.Handle, out isWow64))
                    return "?";

                return isWow64 ? "x86" : "x64";
            }
            catch
            {
                return "?";
            }
        }
    }
}