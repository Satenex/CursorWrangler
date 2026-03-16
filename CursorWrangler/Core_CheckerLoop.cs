using System;
using System.Windows.Forms;

namespace CursorWrangler
{
    public class CheckerLoop
    {
        private Timer timer;

        private FullscreenDetector detector;
        private CursorManager cursorManager;

        private bool paused = false;

        private IntPtr lastHwnd = IntPtr.Zero;
        private bool cursorLocked = false;

        public event Action<bool> FullscreenStateChanged;

		private string cachedProcess = "";
		private string cachedProcessPath = "";
		private string cachedBitness = "";
		private int cachedPid = 0;
		
		private long lastDebugUpdate = 0;
		
		private WindowInfo lastInfo;

        public CheckerLoop()
        {
            detector = new FullscreenDetector();
            cursorManager = new CursorManager();

            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += Tick;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public void SetPaused(bool value)
        {
            paused = value;
        }

		private void Tick(object sender, EventArgs e)
		{
			if (paused)
				return;

			IntPtr hwnd = detector.GetActiveHwnd();

			bool hwndChanged = hwnd != lastHwnd;

			WindowInfo info;

			if (hwndChanged)
			{
				lastHwnd = hwnd;
				lastInfo = detector.GetWindowInfo(hwnd);

				var proc = ProcessCache.Get(lastInfo.ProcessId);

				cachedProcess = proc.Name;
				cachedProcessPath = proc.Path;
				cachedBitness = proc.Bitness;
				cachedPid = lastInfo.ProcessId;
			}

			info = lastInfo;

			if (info.ProcessId == 0)
				return;

			if (info.IsFullscreen)
			{
				if (!cursorLocked)
				{
					cursorManager.LockCursor(info);
					cursorLocked = true;

					FullscreenStateChanged?.Invoke(true);
				}
			}
			else
			{
				if (cursorLocked)
				{
					cursorManager.UnlockCursor();
					cursorLocked = false;

					FullscreenStateChanged?.Invoke(false);
				}
			}

            // DEBUG WINDOW
			long now = Environment.TickCount;

			if (now - lastDebugUpdate > 500)
			{
				lastDebugUpdate = now;

				if (ConfigManager.DebugLog)
				{
					int width = info.Rect.Right - info.Rect.Left;
					int height = info.Rect.Bottom - info.Rect.Top;

					Screen screen = Screen.FromHandle(hwnd);
					var bounds = screen.Bounds;

					int monitorIndex = Array.IndexOf(Screen.AllScreens, screen);
					string monitorName = screen.DeviceName;

					uint thread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);

					var txt =
						"Process: " + cachedProcess + " [" + cachedBitness + "]\r\n" +
						"Path: " + cachedProcessPath + "\r\n" +
						"PID: " + cachedPid + "\r\n" +
						"Thread: " + thread + "\r\n" +
						"HWND: 0x" + hwnd.ToInt64().ToString("X") + "\r\n\r\n" +

						"Title: " + info.Title + "\r\n" +
						"Class: " + info.ClassName + "\r\n\r\n" +

						"Rect: " + info.Rect.Left + "," + info.Rect.Top + " " + info.Rect.Right + "," + info.Rect.Bottom + "\r\n" +
						"Size: " + width + "x" + height + "\r\n\r\n" +

						"Monitor: " + bounds.Width + "x" + bounds.Height +
						" [" + monitorIndex + "] [" + monitorName + "]\r\n\r\n" +

						"ToolWindow: " + info.IsToolWindow + "\r\n" +
						"IgnoredClass: " + info.IgnoredClass + "\r\n\r\n" +

						"FullscreenDetected: " + info.IsFullscreen + "\r\n\r\n" +

						"CursorClip: " + Cursor.Clip + "\r\n" +
						"CursorLocked: " + cursorLocked + "\r\n\r\n" +

						"IgnoreClasses: " + string.Join(",", ConfigManager.WindowsClassNotDetected) + "\r\n" +
						"KeyboardLayout: " + InputLanguage.CurrentInputLanguage.LayoutName;

					var dbg = DebugWindow.Get();

					if (dbg.Visible)
						dbg.UpdateText(txt);
				}
			}
        }
    }
}