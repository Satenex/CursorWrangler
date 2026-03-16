using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Threading;
using System.IO;

namespace CursorWrangler
{
    class CursorWranglerContext : ApplicationContext
    {
		public static CursorWranglerContext Instance;
		
        volatile bool IsShuttingDown = false;

        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;

        private ToolStripMenuItem PauseMenuItem;
        private ToolStripMenuItem ShowMenuItem;
        private ToolStripMenuItem ResetPositionMenuItem;
        private ToolStripMenuItem QuitMenuItem;

        private readonly CursorWrangler fsl;
        private readonly CheckerLoop c;

        FileSystemWatcher configWatcher;

        public CursorWranglerContext()
        {
			Instance = this;
			
            Application.ApplicationExit += OnApplicationExit;

            InitializeComponent();

            ConfigManager.Load();

            c = new CheckerLoop();
            fsl = new CursorWrangler();

            var handle = fsl.Handle;

            if (!ConfigManager.StartMinimized)
                fsl.Show();

            StartPipeServer();
            StartConfigWatcher();

			c.FullscreenStateChanged += OnFullscreenChanged;

            TrayIcon.Visible = true;
			
			c.Start();
        }

		void OnFullscreenChanged(bool fullscreen)
		{
			if (fsl == null || fsl.IsDisposed)
				return;

			fsl.BeginInvoke(new Action(() =>
			{
				if (fullscreen)
					fsl.SetFullscreenState();
				else
					fsl.SetWaitingState();
			}));
		}

        void StartConfigWatcher()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;

            configWatcher = new FileSystemWatcher(dir, "config.ini");

            configWatcher.NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.Size;

            configWatcher.Changed += (s, e) =>
            {
                if (IsShuttingDown)
                    return;

                try
                {
                    Thread.Sleep(120);

                    ConfigManager.Load();

                    if (!IsShuttingDown && fsl != null && !fsl.IsDisposed)
                    {
                        fsl.BeginInvoke(new Action(() =>
                        {
                            if (!IsShuttingDown)
                                fsl.ApplyConfig();
                        }));
                    }
                }
                catch { }
            };

            configWatcher.EnableRaisingEvents = true;
        }

        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon();
            TrayIconContextMenu = new ContextMenuStrip();

            PauseMenuItem = new ToolStripMenuItem("Pause");
            ShowMenuItem = new ToolStripMenuItem();
            ResetPositionMenuItem = new ToolStripMenuItem();
            QuitMenuItem = new ToolStripMenuItem();

            TrayIconContextMenu.SuspendLayout();

            TrayIconContextMenu.Opening += TrayIconContextMenu_Opening;

            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
            TrayIcon.Icon = Icons.TrayActive;
            TrayIcon.Text = "CursorWrangler";
            TrayIcon.Visible = true;
            TrayIcon.MouseClick += TrayIcon_MouseClick;

            PauseMenuItem.Text = "Pause";
            PauseMenuItem.Click += PauseMenuItem_Click;

            ShowMenuItem.Text = "Show";
            ShowMenuItem.Click += ShowMenuItem_Click;

            ResetPositionMenuItem.Text = "Reset window position";
            ResetPositionMenuItem.Click += ResetPositionMenuItem_Click;

            QuitMenuItem.Text = "Quit";
            QuitMenuItem.Click += QuitMenuItem_Click;

            TrayIconContextMenu.Items.AddRange(new ToolStripItem[]
            {
                PauseMenuItem,
                ShowMenuItem,
                ResetPositionMenuItem,
                new ToolStripSeparator(),
                QuitMenuItem
            });

            TrayIconContextMenu.ResumeLayout(false);
        }

        private void PauseMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown) return;
            AppState.Paused = !AppState.Paused;
			fsl.UpdatePauseVisual();
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ToggleWindow();
			}
		}

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown) return;
            ToggleWindow();
        }

        private void ResetPositionMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown) return;

            ConfigManager.WindowX = -1;
            ConfigManager.WindowY = -1;

            ConfigManager.Save();

            fsl.ResetWindowPosition();
			fsl.Show();
			fsl.Activate();
        }

		private void ToggleWindow()
		{
			if (fsl == null || fsl.IsDisposed)
				return;

			if (!fsl.Visible)
			{
				fsl.Show();
				fsl.WindowState = FormWindowState.Normal;
				fsl.Activate();

				if (ConfigManager.DebugLog)
				{
					var dbg = DebugWindow.Get();

					if (!dbg.Visible)
						dbg.Show();

					dbg.Follow(fsl);
				}
			}
			else
			{
				fsl.Hide();

				if (ConfigManager.DebugLog)
				{
					var dbg = DebugWindow.Get();

					if (dbg.Visible)
						dbg.Hide();
				}
			}
		}

        private void TrayIconContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsShuttingDown) return;

            if (fsl.Visible)
                ShowMenuItem.Text = "Hide";
            else
                ShowMenuItem.Text = "Show";
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown)
                return;

            IsShuttingDown = true;

            Shutdown();

            ExitThread();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (TrayIcon != null)
                TrayIcon.Visible = false;
        }

        private void StartPipeServer()
        {
            Thread t = new Thread(() =>
            {
                while (!IsShuttingDown)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream("CursorWranglerPipe"))
                        {
                            server.WaitForConnection();

                            if (IsShuttingDown)
                                return;

                            if (fsl != null && !fsl.IsDisposed)
                            {
                                fsl.BeginInvoke(new Action(() =>
                                {
                                    if (!IsShuttingDown)
                                    {
                                        fsl.Show();
                                        fsl.Activate();
                                    }
                                }));
                            }
                        }
                    }
                    catch { }
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        public void UpdateTrayState(bool active)
        {
            if (IsShuttingDown)
                return;

            if (active)
            {
                TrayIcon.Icon = Icons.TrayActive;
                PauseMenuItem.Text = "Pause";
            }
            else
            {
                TrayIcon.Icon = Icons.TrayPaused;
                PauseMenuItem.Text = "Resume";
            }
        }

		public void OnFormExitRequested()
		{
			if (IsShuttingDown)
				return;

			IsShuttingDown = true;

			Shutdown();

			ExitThread();
		}

		public void SetPaused(bool paused)
		{
			if (c != null)
				c.SetPaused(paused);

			UpdateTrayState(!paused);
		}

        public void Shutdown()
        {
            try
            {
                if (configWatcher != null)
                {
                    configWatcher.EnableRaisingEvents = false;
                    configWatcher.Dispose();
                    configWatcher = null;
                }
            }
            catch { }

            try
            {
                if (TrayIcon != null)
                {
                    TrayIcon.Visible = false;
                    TrayIcon.Dispose();
                    TrayIcon = null;
                }
            }
            catch { }

            try
            {
                if (fsl != null && !fsl.IsDisposed)
                    fsl.Close();
            }
            catch { }
        }
    }
}