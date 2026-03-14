using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace CursorWrangler
{
    public partial class CursorWrangler : Form
    {
        private Checker c;

        NamedPipeClientStream pipe;
        StreamWriter writer;
        
        Process overlayProcess; // Оверлей-процесс

        private bool uiInitializing = true;

        public bool IsExiting = false;

        public event EventHandler ActiveStateToggled;
        public event Action<bool> TrayStateChanged;
        public event Action RequestAppExit;

        bool debugFrozen = false;
        bool scrollWasDown = false;

        [DllImport("user32.dll")]
        static extern short GetKeyState(int nVirtKey);

        const int VK_SCROLL = 0x91;

        public CursorWrangler(Checker checker)
        {
            InitializeComponent();

            c = checker;

            Application.Idle += ListenFreezeHotkey;

            Config.Load();

            StartPosition = FormStartPosition.Manual;

            if (Config.WindowX != -1 &&
                Config.WindowY != -1 &&
                IsPositionVisible(Config.WindowX, Config.WindowY))
            {
                Location = new Point(Config.WindowX, Config.WindowY);
            }
            else
            {
                MoveNearTray();
            }

            EnsureWindowVisible();

            this.ResizeEnd += CursorWrangler_MoveEnd;
			this.Move += CursorWrangler_MoveEnd;

            ApplyConfig();

            c.ActiveStateChanged += ActiveStateChanged;
            c.ForegroundFullscreenStateChanged += ForegroundFullscreenStateChanged;

            MinimizeOnCloseCheckBox.CheckedChanged += MinimizeOnCloseCheckBox_CheckedChanged;
            StartMinimizedCheckBox.CheckedChanged += StartMinimizedCheckBox_CheckedChanged;
            LaunchOnStartupCheckBox.CheckedChanged += LaunchOnStartupCheckBox_CheckedChanged;
            debugCheckbox.CheckedChanged += DebugCheckboxChanged;

            uiInitializing = false;

            if (Config.DebugLog)
                OpenDebugWindow();

            UpdateUI(true);
			
			// Обработчики фокуса
			this.Activated += (s, e) => UpdateOverlayText();
			this.Deactivate += (s, e) => UpdateOverlayText();
        }

        public void ApplyConfig()
        {
            uiInitializing = true;

            AlwaysOnTopCheckBox.Checked = Config.AlwaysOnTop;
            TopMost = Config.AlwaysOnTop;

            MinimizeOnCloseCheckBox.Checked = Config.MinimizeOnClose;
            StartMinimizedCheckBox.Checked = Config.StartMinimized;

            LaunchOnStartupCheckBox.Checked = Config.IsStartupEnabled();
            ForceEnglishCheckBox.Checked = Config.ForceEnglishFullscreen;

            debugCheckbox.Checked = Config.DebugLog;

            uiInitializing = false;
        }

		// Метод для обновления текста в оверлее
		private void UpdateOverlayText(string text)
		{
			if (pipe == null || !pipe.CanWrite || writer == null)
				return;

			try
			{
				writer.WriteLine(text.Replace("\n", "\\n"));
				writer.Flush();
			}
			catch
			{
				writer = null;
			}
		}
		
		// Обновление текста оверлея из текущего DebugInfo
		private void UpdateOverlayText()
		{
			if (writer == null)
				return;

			try
			{
				writer.WriteLine("UpdateOverlayText");
				writer.Flush();
			}
			catch
			{
				writer = null;
			}
		}

		// Открыть оверлей, если он не был открыт
		void OpenDebugWindow()
		{
			if (overlayProcess != null && !overlayProcess.HasExited)
				return;

			try
			{
				overlayProcess = Process.Start("CursorWranglerOverlay.exe");
				overlayProcess.EnableRaisingEvents = true;
				overlayProcess.Exited += OverlayProcess_Exited;

				// Попытка подключения к NamedPipe для передачи данных
				try
				{
					pipe = new NamedPipeClientStream(".", "CursorWranglerDebug", PipeDirection.Out);
					pipe.Connect(2000);  // Подключаемся с тайм-аутом 2 секунды

					writer = new StreamWriter(pipe);
					writer.AutoFlush = true;
				}
				catch
				{
					// В случае ошибки при подключении, обнуляем pipe и writer
					pipe = null;
					writer = null;
				}

				// После того как оверлей и pipe готовы, обновляем позицию
				UpdateOverlayPosition();
			}
			catch
			{
				return;
			}

			// Подключаем обработчик информации для обновлений
			c.DebugInfo += OnDebugInfo;
		}

		// Обработчик закрытия процесса оверлея
		private void OverlayProcess_Exited(object sender, EventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => OverlayProcess_Exited(sender, e)));
				return;
			}

			// Обновление состояния в UI
			uiInitializing = true;
			debugCheckbox.Checked = false;
			uiInitializing = false;

			// Закрываем pipe и writer, если они были использованы
			writer?.Close();
			writer = null;

			pipe?.Close();
			pipe = null;
		}

		// Закрытие оверлея, если он был открыт
		void CloseDebugWindow()
		{
			if (overlayProcess != null && !overlayProcess.HasExited)
			{
				try
				{
					overlayProcess.Kill();  // Останавливаем процесс оверлея
				}
				catch { }
			}
			overlayProcess = null;
		}

        public void SaveWindowPosition()
        {
            Config.WindowX = Left;
            Config.WindowY = Top;
            Config.Save();
        }

        private void CursorWrangler_MoveEnd(object sender, EventArgs e)
        {
            EnsureWindowVisible();
            SaveWindowPosition();
            
            // Обновляем позицию оверлея при изменении позиции основного окна
            UpdateOverlayPosition();
        }

		private void CursorWrangler_Move(object sender, EventArgs e)
		{
			// Обновляем позицию оверлея в процессе перемещения
			UpdateOverlayPosition();
		}

        private void CursorWrangler_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsExiting && Config.MinimizeOnClose)
            {
                e.Cancel = true;
                Hide();
                SetVisibility(false);
                return;
            }

            SaveWindowPosition();

            IsExiting = true;

            RequestAppExit?.Invoke();
            
            // Закрытие оверлея, если он открыт
            CloseDebugWindow();
        }

        public void SetVisibility(bool state)
        {
            if (IsExiting)
                return;

            if (state)
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
            else
            {
                Hide();
            }
        }

		// Обновление позиции оверлея
		private void UpdateOverlayPosition()
		{
			// Проверяем, открыт ли оверлей и подключен ли pipe
			if (overlayProcess != null && !overlayProcess.HasExited && pipe != null && pipe.CanWrite)
			{
				Point overlayLocation = new Point(Left + Width + 5, Top);  // Оверлей справа от основного окна

				// Передаем команду на обновление позиции оверлея через pipe
				writer.WriteLine($"MoveOverlay {overlayLocation.X} {overlayLocation.Y}");
				writer.Flush();  // Обеспечиваем немедленную отправку данных
			}
		}

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            ActiveStateToggled?.Invoke(this, e);
        }

        private void UpdateUI(bool active)
        {
            TrayStateChanged?.Invoke(active);

            if (active)
            {
                StatusLabel.Text = "Waiting for focus";
                StatusLabel.ForeColor = Color.DarkOrange;

                ToggleButton.Text = "Pause";
                ToggleButton.BackColor = Color.LightGreen;
            }
            else
            {
                StatusLabel.Text = "Paused";
                StatusLabel.ForeColor = Color.Gray;

                ToggleButton.Text = "Resume";
                ToggleButton.BackColor = Color.LightGray;
            }
        }

        public void ActiveStateChanged(object sender, BoolEventArgs e)
        {
            UpdateUI(e.Bool);
        }

        public void ForegroundFullscreenStateChanged(object sender, BoolEventArgs e)
        {
            if (e.Bool)
            {
                StatusLabel.Text = "Fullscreen app in focus";
                StatusLabel.ForeColor = Color.Green;
            }
            else
            {
                StatusLabel.Text = "Waiting for focus";
                StatusLabel.ForeColor = Color.DarkOrange;
            }
        }
		
        private void AlwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (uiInitializing) return;

            TopMost = AlwaysOnTopCheckBox.Checked;

            Config.AlwaysOnTop = AlwaysOnTopCheckBox.Checked;
            Config.Save();
        }

        private void MinimizeOnCloseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (uiInitializing) return;

            Config.MinimizeOnClose = MinimizeOnCloseCheckBox.Checked;
            Config.Save();
        }

        private void StartMinimizedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (uiInitializing) return;

            Config.StartMinimized = StartMinimizedCheckBox.Checked;
            Config.Save();
        }

        private void LaunchOnStartupCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (uiInitializing) return;

            Config.SetStartup(LaunchOnStartupCheckBox.Checked);
        }

        private void ForceEnglishCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (uiInitializing) return;

            Config.ForceEnglishFullscreen = ForceEnglishCheckBox.Checked;
            Config.Save();
        }

        public void DebugCheckboxChanged(object sender, EventArgs e)
        {
            if (uiInitializing)
                return;

            bool state = debugCheckbox.Checked;

            Config.DebugLog = state;
            Config.Save();

            if (state)
            {
                OpenDebugWindow();
            }
			else
			{
				c.DebugInfo -= OnDebugInfo;

				// Останавливаем процесс оверлея при снятии галочки
				CloseDebugWindow();
			}
        }

		void OnDebugInfo(object sender, string text)
		{
			if (IsExiting || debugFrozen)
				return;

			// Обновляем текст оверлея с полученной строки
			UpdateOverlayText(text);
		}

        void ListenFreezeHotkey(object sender, EventArgs e)
        {
            if (IsExiting)
                return;

            bool scrollOn = (GetKeyState(VK_SCROLL) & 1) != 0;

            if (scrollOn != scrollWasDown)
            {
                scrollWasDown = scrollOn;
                debugFrozen = scrollOn;
            }
        }

        private void EnsureWindowVisible()
        {
            Rectangle rect = Bounds;

            foreach (Screen screen in Screen.AllScreens)
                if (screen.WorkingArea.IntersectsWith(rect))
                    return;

            Screen nearest = Screen.FromPoint(new Point(rect.Left, rect.Top));
            Rectangle wa = nearest.WorkingArea;

            int newX = Math.Max(wa.Left, Math.Min(rect.Left, wa.Right - rect.Width));
            int newY = Math.Max(wa.Top, Math.Min(rect.Top, wa.Bottom - rect.Height));

            Location = new Point(newX, newY);
        }

        private bool IsPositionVisible(int x, int y)
        {
            Rectangle windowRect = new Rectangle(x, y, Width, Height);

            foreach (Screen screen in Screen.AllScreens)
                if (screen.Bounds.IntersectsWith(windowRect))
                    return true;

            return false;
        }

        private void MoveNearTray()
        {
            Screen screen = Screen.PrimaryScreen;
            Rectangle wa = screen.WorkingArea;

            int x = wa.Right - Width - 10;
            int y = wa.Bottom - Height - 10;

            Location = new Point(x, y);
        }

        public void ResetWindowPosition()
        {
            MoveNearTray();
        }

        public void ToggleActive()
        {
            ToggleButton.PerformClick();
        }
    }
}