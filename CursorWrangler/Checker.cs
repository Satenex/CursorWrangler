using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Diagnostics;

namespace CursorWrangler
{
    public class Checker
    {
        readonly Timer cursorTimer = new Timer();
        readonly Timer layoutWatcher = new Timer();
        readonly Timer layoutRetryTimer = new Timer();

        bool RestoringLayout = false;
        bool IsActive = true;

        IntPtr GameHwnd = IntPtr.Zero;

        string LastLayout = "00000409";

        int layoutRetryCount = 0;
        const int MAX_RETRY = 6;

        DateTime lastFocusChange = DateTime.MinValue;
		string[] IgnoredWindowClasses;
		
		string GetWindowClass(IntPtr hwnd)
		{
			var sb = new System.Text.StringBuilder(256);
			GetClassName(hwnd, sb, sb.Capacity);
			return sb.ToString();
		}

		string GetWindowTitle(IntPtr hwnd)
		{
			var sb = new System.Text.StringBuilder(256);
			GetWindowText(hwnd, sb, sb.Capacity);
			return sb.ToString();
		}
		
		IntPtr lastHwnd = IntPtr.Zero;

		string cachedProcess = "";
		string cachedProcessPath = "";
		string cachedBitness = "";
		uint cachedPid = 0;

		string cachedTitle = "";
		string cachedClass = "";

        public event EventHandler<BoolEventArgs> ActiveStateChanged;
        public event EventHandler<BoolEventArgs> ForegroundFullscreenStateChanged;
		public event EventHandler<string> DebugInfo;

        const int WM_INPUTLANGCHANGEREQUEST = 0x50;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
		
		const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rc);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr OpenProcess(int processAccess,	bool bInheritHandle, uint processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, System.Text.StringBuilder text, ref int size);

		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        public Checker()
        {
            ReloadConfig();

            cursorTimer.Interval = 150;
            cursorTimer.Tick += CursorLoop;
            cursorTimer.Start();

            layoutWatcher.Interval = 300;
            layoutWatcher.Tick += WatchLayout;
            layoutWatcher.Start();

            layoutRetryTimer.Interval = 60;
            layoutRetryTimer.Tick += LayoutRetry;
        }

		public void ReloadConfig()
		{
			IgnoredWindowClasses = Config.WindowsClassNotDetected?.ToArray() ?? new string[0];
		}

        public void ActiveStateToggled(object sender, EventArgs e)
        {
            IsActive = !IsActive;

            if (!IsActive)
            {
                Cursor.Clip = Rectangle.Empty;

                GameHwnd = IntPtr.Zero;
                RestoringLayout = false;

                layoutRetryTimer.Stop();
            }

            ActiveStateChanged?.Invoke(this, new BoolEventArgs(IsActive));
        }

        void WatchLayout(object sender, EventArgs e)
        {
            if (!IsActive)
                return;

            if (GameHwnd != IntPtr.Zero || RestoringLayout)
                return;

            IntPtr hwnd = GetForegroundWindow();

            if (hwnd == IntPtr.Zero)
                return;

            uint thread = GetWindowThreadProcessId(hwnd, out _);

            IntPtr layout = GetKeyboardLayout(thread);

            LastLayout = ((uint)layout & 0xFFFF).ToString("x8");
        }

		void CursorLoop(object sender, EventArgs e)
		{
			if (!IsActive) return;

			IntPtr hwnd = GetForegroundWindow();

			if (hwnd != lastHwnd)
			{
				lastHwnd = hwnd;

				cachedClass = GetWindowClass(hwnd);
				cachedTitle = GetWindowTitle(hwnd);

				uint thread = GetWindowThreadProcessId(hwnd, out cachedPid);
				cachedProcess = "unknown";
				cachedProcessPath = "unknown";
				cachedBitness = "unknown";

				// Обновляем значения и вызываем событие DebugInfo
				UpdateDebugInfo(hwnd);
			}

			// Периодическое обновление состояния
			UpdateDebugInfo(hwnd);
		}

		private void UpdateDebugInfo(IntPtr hwnd)
		{
			if (hwnd == IntPtr.Zero)
				return;

			string layoutLang = GetLayoutLanguage(LastLayout);
			string debug = $"Process: {cachedProcess} [{cachedBitness}]\n" +
						   $"Path: {cachedProcessPath}\n" +
						   $"PID: {cachedPid}\n" +
						   $"Thread: {GetWindowThreadProcessId(hwnd, out _) }\n" +
						   $"Title: {cachedTitle}\n" +
						   $"Class: {cachedClass}\n" +
						   $"Layout: {layoutLang}\n";

			// Отправляем текст с помощью события DebugInfo
			DebugInfo?.Invoke(this, debug);
		}
		
        void LayoutRetry(object sender, EventArgs e)
        {
            if (!IsActive)
                return;

            if ((DateTime.Now - lastFocusChange).TotalMilliseconds < 180)
                return;

            IntPtr fg = GetForegroundWindow();

            if (fg == IntPtr.Zero)
                return;

            uint thread = GetWindowThreadProcessId(fg, out _);

            IntPtr currentLayout = GetKeyboardLayout(thread);

            string current = ((uint)currentLayout & 0xFFFF).ToString("x8");

            if (current == LastLayout)
            {
                layoutRetryTimer.Stop();
                layoutRetryCount = 0;
                RestoringLayout = false;
                return;
            }

            IntPtr layout = LoadKeyboardLayout(LastLayout, 1);

            SendMessage(fg, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, layout);

            layoutRetryCount++;

            if (layoutRetryCount > MAX_RETRY)
            {
                layoutRetryTimer.Stop();
                layoutRetryCount = 0;
                RestoringLayout = false;
            }
        }

        bool IsToolWindow(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            return (style & WS_EX_TOOLWINDOW) != 0;
        }

        bool IsFullscreen(IntPtr hwnd)
        {
            IntPtr desktop = GetDesktopWindow();
            IntPtr shell = GetShellWindow();

            if (hwnd == desktop || hwnd == shell)
                return false;

            if (!GetWindowRect(hwnd, out RECT rect))
                return false;

            Rectangle screen = Screen.FromHandle(hwnd).Bounds;

            bool fullscreen =
				Math.Abs((rect.Right - rect.Left) - screen.Width) <= 2 &&
				Math.Abs((rect.Bottom - rect.Top) - screen.Height) <= 2;

            return fullscreen;
        }
		
		bool IsIgnoredClass(IntPtr hwnd)
		{
			var sb = new System.Text.StringBuilder(256);

			GetClassName(hwnd, sb, sb.Capacity);

			string cls = sb.ToString();

			foreach (var ignored in IgnoredWindowClasses)
			{
				if (cls.StartsWith(ignored, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
		
		string GetLayoutLanguage(string layout)
		{
			try
			{
				int id = Convert.ToInt32(layout, 16);

				var culture = new System.Globalization.CultureInfo(id);

				return culture.EnglishName;
			}
			catch
			{
				return "Unknown";
			}
		}
		
		string GetProcessPath(uint pid)
		{
			IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);

			if (hProcess == IntPtr.Zero)
				return "access denied";

			try
			{
				var buffer = new System.Text.StringBuilder(1024);
				int size = buffer.Capacity;

				if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
					return buffer.ToString();
			}
			catch
			{
			}
			finally
			{
				CloseHandle(hProcess);
			}

			return "unknown";
		}
		
		string GetProcessBitness(Process proc)
		{
			if (!Environment.Is64BitOperatingSystem)
				return "x86";

			bool isWow64;

			if (!IsWow64Process(proc.Handle, out isWow64))
				return "Unknown";

			if (isWow64)
				return "x86";

			return "x64";
		}
    }

    public class BoolEventArgs : EventArgs
    {
        public bool Bool { get; set; }

        public BoolEventArgs(bool b)
        {
            Bool = b;
        }
    }
}