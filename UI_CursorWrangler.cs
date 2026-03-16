using System;
using System.Drawing;
using System.Windows.Forms;

namespace CursorWrangler
{
	public class CursorWrangler : Form
	{
		private Button ToggleButton;
		private Label StatusLabel;
		private CheckBox AlwaysOnTopCheckBox;
		private CheckBox LaunchOnStartupCheckBox;
		private CheckBox StartMinimizedCheckBox;
		private CheckBox MinimizeOnCloseCheckBox;
		private CheckBox ForceEnglishCheckBox;
		private CheckBox debugCheckbox;
		private Panel BottomSeparator;

		Timer savePosTimer;

		public bool IsExiting = false;

		public CursorWrangler()
		{
			InitializeComponent();

			savePosTimer = new Timer();
			savePosTimer.Interval = 400;
			savePosTimer.Tick += SavePosTimer_Tick;

			LocationChanged += MainForm_LocationChanged;

			this.SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.UserPaint,
				true);

			this.UpdateStyles();

			if (ConfigManager.DebugLog && !ConfigManager.StartMinimized)
			{
				var dbg = DebugWindow.Get();

				dbg.Follow(this);

				if (!dbg.Visible)
					dbg.Show();
			}

			LoadWindowPosition();
			LoadConfigToUI();
			BindConfig();

			LaunchOnStartupCheckBox.Checked = ConfigManager.IsStartupEnabled();

			LaunchOnStartupCheckBox.CheckedChanged += (s, e) =>
			{
				ConfigManager.SetStartup(
					LaunchOnStartupCheckBox.Checked
				);
			};

			ConfigManager.ConfigReloaded += ApplyConfig;
		}

		public void ApplyConfig()
		{
			if (InvokeRequired)
			{
				Invoke(new Action(ApplyConfig));
				return;
			}

			AlwaysOnTopCheckBox.Checked = ConfigManager.AlwaysOnTop;
			ForceEnglishCheckBox.Checked = ConfigManager.ForceEnglishFullscreen;

			if (ConfigManager.DebugLog && !ConfigManager.StartMinimized)
			{
				var dbg = DebugWindow.Get();
				dbg.Follow(this);

				if (!dbg.Visible)
					dbg.Show();
			}

			TopMost = ConfigManager.AlwaysOnTop;

			if (DebugWindow.Get() != null)
			{
				DebugWindow.Get().TopMost = ConfigManager.AlwaysOnTop;
			}
		}

		void Bind(CheckBox box, Func<bool> getter, Action<bool> setter)
		{
			box.Checked = getter();

			box.CheckedChanged += (s, e) =>
			{
				setter(box.Checked);
				ConfigManager.Save();
			};
		}

		void MainForm_LocationChanged(object sender, EventArgs e)
		{
			savePosTimer.Stop();
			savePosTimer.Start();
		}

		void SavePosTimer_Tick(object sender, EventArgs e)
		{
			savePosTimer.Stop();

			if (WindowState != FormWindowState.Normal)
				return;

			ConfigManager.WindowX = Left;
			ConfigManager.WindowY = Top;

			ConfigManager.Save();
		}

		public void SetDebugCheckbox(bool value)
		{
			if (InvokeRequired)
			{
				Invoke(new Action<bool>(SetDebugCheckbox), value);
				return;
			}

			debugCheckbox.Checked = value;
		}

		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);

			if (ConfigManager.DebugLog)
				DebugWindow.Get()?.Follow(this);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (ConfigManager.DebugLog)
				DebugWindow.Get()?.Follow(this);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			var dbg = DebugWindow.Get();

			if (WindowState == FormWindowState.Minimized)
			{
				if (dbg.Visible)
					dbg.Hide();

				return;
			}

			if (ConfigManager.DebugLog && dbg.Visible)
				dbg.Follow(this);
		}

		private void LoadWindowPosition()
		{
			int x = ConfigManager.WindowX;
			int y = ConfigManager.WindowY;

			if (x != 0 || y != 0)
			{
				StartPosition = FormStartPosition.Manual;
				Location = new Point(x, y);
			}
		}

		public void SetFullscreenState()
		{
			if (InvokeRequired)
			{
				Invoke(new Action(SetFullscreenState));
				return;
			}

			StatusLabel.Text = "Fullscreen detected";
			StatusLabel.ForeColor = Color.LimeGreen;
		}

		public void SetWaitingState()
		{
			if (InvokeRequired)
			{
				Invoke(new Action(SetWaitingState));
				return;
			}

			StatusLabel.Text = "Waiting for focus";
			StatusLabel.ForeColor = Color.DarkOrange;
		}

		public bool IsPaused { get; private set; }

		public void UpdatePauseState(bool paused)
		{
			IsPaused = paused;

			ToggleButton.Text = paused ? "Resume" : "Pause";
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (ConfigManager.MinimizeOnClose)
			{
				e.Cancel = true;

				Hide();

				var dbg = DebugWindow.Get();
				if (dbg.Visible)
					dbg.Hide();

				return;
			}

			CursorWranglerContext.Instance?.OnFormExitRequested();
		}

		public void ResetWindowPosition()
		{
			StartPosition = FormStartPosition.CenterScreen;

			Location = new Point(
				(Screen.PrimaryScreen.WorkingArea.Width - Width) / 2,
				(Screen.PrimaryScreen.WorkingArea.Height - Height) / 2
			);
		}

		public void SetVisibility(bool visible)
		{
			if (InvokeRequired)
			{
				Invoke(new Action<bool>(SetVisibility), visible);
				return;
			}

			if (visible)
			{
				Show();
				WindowState = FormWindowState.Normal;
				Activate();

				if (ConfigManager.DebugLog && !ConfigManager.StartMinimized)
				{
					var dbg = DebugWindow.Get();

					dbg.Follow(this);

					if (!dbg.Visible)
						dbg.Show();
				}
			}
			else
			{
				Hide();

				var dbg = DebugWindow.Get();

				if (dbg.Visible)
					dbg.Hide();
			}
		}

		private void LoadConfigToUI()
		{
			AlwaysOnTopCheckBox.Checked = ConfigManager.AlwaysOnTop;
			StartMinimizedCheckBox.Checked = ConfigManager.StartMinimized;
			MinimizeOnCloseCheckBox.Checked = ConfigManager.MinimizeOnClose;
			ForceEnglishCheckBox.Checked = ConfigManager.ForceEnglishFullscreen;
			debugCheckbox.Checked = ConfigManager.DebugLog;

			TopMost = ConfigManager.AlwaysOnTop;
		}

		private void BindConfig()
		{
			Bind(AlwaysOnTopCheckBox,
				() => ConfigManager.AlwaysOnTop,
				v =>
				{
					ConfigManager.AlwaysOnTop = v;
					TopMost = v;
				});

			Bind(StartMinimizedCheckBox,
				() => ConfigManager.StartMinimized,
				v => ConfigManager.StartMinimized = v);

			Bind(MinimizeOnCloseCheckBox,
				() => ConfigManager.MinimizeOnClose,
				v => ConfigManager.MinimizeOnClose = v);

			Bind(ForceEnglishCheckBox,
				() => ConfigManager.ForceEnglishFullscreen,
				v => ConfigManager.ForceEnglishFullscreen = v);

			Bind(debugCheckbox,
				() => ConfigManager.DebugLog,
				v =>
				{
					ConfigManager.DebugLog = v;

					var dbg = DebugWindow.Get();

					if (v)
					{
						if (!dbg.Visible)
							dbg.Show();

						dbg.Follow(this);
					}
					else
					{
						if (dbg.Visible)
							dbg.Hide();
					}
				});
		}

		private void InitializeComponent()
		{
			ToggleButton = new Button();
			StatusLabel = new Label();
			AlwaysOnTopCheckBox = new CheckBox();
			LaunchOnStartupCheckBox = new CheckBox();
			StartMinimizedCheckBox = new CheckBox();
			MinimizeOnCloseCheckBox = new CheckBox();
			ForceEnglishCheckBox = new CheckBox();
			debugCheckbox = new CheckBox();
			BottomSeparator = new Panel();

			SuspendLayout();

			ToggleButton.BackColor = Color.LightGreen;
			ToggleButton.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
			ToggleButton.Location = new Point(30, 15);
			ToggleButton.Size = new Size(200, 45);
			ToggleButton.Text = "Pause";
			ToggleButton.Click += ToggleButton_Click;

			StatusLabel.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
			StatusLabel.ForeColor = Color.DarkOrange;
			StatusLabel.Location = new Point(10, 65);
			StatusLabel.Size = new Size(240, 28);
			StatusLabel.Text = "Waiting for focus";
			StatusLabel.TextAlign = ContentAlignment.MiddleCenter;

			AlwaysOnTopCheckBox.AutoSize = true;
			AlwaysOnTopCheckBox.Font = new Font("Microsoft Sans Serif", 10F);
			AlwaysOnTopCheckBox.Location = new Point(30, 105);
			AlwaysOnTopCheckBox.Text = "Always on top";

			LaunchOnStartupCheckBox.AutoSize = true;
			LaunchOnStartupCheckBox.Font = new Font("Microsoft Sans Serif", 10F);
			LaunchOnStartupCheckBox.Location = new Point(30, 132);
			LaunchOnStartupCheckBox.Text = "Launch at startup";

			StartMinimizedCheckBox.AutoSize = true;
			StartMinimizedCheckBox.Font = new Font("Microsoft Sans Serif", 10F);
			StartMinimizedCheckBox.Location = new Point(30, 159);
			StartMinimizedCheckBox.Text = "Start minimized";

			MinimizeOnCloseCheckBox.AutoSize = true;
			MinimizeOnCloseCheckBox.Font = new Font("Microsoft Sans Serif", 10F);
			MinimizeOnCloseCheckBox.Location = new Point(30, 186);
			MinimizeOnCloseCheckBox.Text = "Minimize to tray on close";

			BottomSeparator.BackColor = Color.Gray;
			BottomSeparator.Location = new Point(25, 215);
			BottomSeparator.Size = new Size(210, 1);

			ForceEnglishCheckBox.AutoSize = true;
			ForceEnglishCheckBox.Font = new Font("Microsoft Sans Serif", 10F);
			ForceEnglishCheckBox.Location = new Point(30, 226);
			ForceEnglishCheckBox.Text = "Force English in fullscreen";

			debugCheckbox.AutoSize = true;
			debugCheckbox.Location = new Point(242, 5);

			ClientSize = new Size(260, 256);

			Controls.Add(ToggleButton);
			Controls.Add(StatusLabel);
			Controls.Add(AlwaysOnTopCheckBox);
			Controls.Add(LaunchOnStartupCheckBox);
			Controls.Add(StartMinimizedCheckBox);
			Controls.Add(MinimizeOnCloseCheckBox);
			Controls.Add(BottomSeparator);
			Controls.Add(ForceEnglishCheckBox);
			Controls.Add(debugCheckbox);

			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterScreen;
			Text = "CursorWrangler";

			ResumeLayout(false);
			PerformLayout();
		}

		private void ToggleButton_Click(object sender, EventArgs e)
		{
			AppState.Paused = !AppState.Paused;

			CursorWranglerContext.Instance?.SetPaused(AppState.Paused);

			UpdatePauseVisual();
		}

		public void UpdatePauseVisual()
		{
			if (AppState.Paused)
			{
				ToggleButton.Text = "Resume";
				ToggleButton.BackColor = Color.LightGray;
				StatusLabel.Text = "Paused";
				StatusLabel.ForeColor = Color.Gray;
			}
			else
			{
				ToggleButton.Text = "Pause";
				ToggleButton.BackColor = Color.LightGreen;
				StatusLabel.Text = "Waiting for focus";
				StatusLabel.ForeColor = Color.DarkOrange;
			}
		}
	}
}