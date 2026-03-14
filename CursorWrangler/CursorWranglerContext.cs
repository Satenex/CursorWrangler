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
        volatile bool IsShuttingDown = false;

        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;

        private ToolStripMenuItem PauseMenuItem;
        private ToolStripMenuItem ShowMenuItem;
        private ToolStripMenuItem ResetPositionMenuItem;
        private ToolStripMenuItem QuitMenuItem;

        private readonly CursorWrangler fsl;
        private readonly Checker c;

        FileSystemWatcher configWatcher;

        public CursorWranglerContext()
        {
            Application.ApplicationExit += OnApplicationExit;

            InitializeComponent();

            Config.Load();

            c = new Checker();
            fsl = new CursorWrangler(c);

			fsl.RequestAppExit += OnFormExitRequested;
            fsl.TrayStateChanged += UpdateTrayState;

            var handle = fsl.Handle;

            if (!Config.StartMinimized)
                fsl.SetVisibility(true);

            StartPipeServer();
            StartConfigWatcher();

            c.ActiveStateChanged += fsl.ActiveStateChanged;
            c.ForegroundFullscreenStateChanged += fsl.ForegroundFullscreenStateChanged;
            fsl.ActiveStateToggled += c.ActiveStateToggled;

            TrayIcon.Visible = true;
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

                    Config.Load();

                    c.ReloadConfig();

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
            TrayIcon.Icon = Properties.Resources.TrayIconActive;
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
            fsl.ToggleActive();
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (IsShuttingDown) return;

            if (e.Button == MouseButtons.Left)
                ToggleWindow();
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown) return;
            ToggleWindow();
        }

        private void ResetPositionMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown) return;

            Config.WindowX = -1;
            Config.WindowY = -1;

            Config.Save();

            fsl.ResetWindowPosition();
            fsl.SetVisibility(true);
        }

        private void ToggleWindow()
        {
            if (IsShuttingDown) return;

            if (fsl.Visible && fsl.WindowState == FormWindowState.Normal)
                fsl.SetVisibility(false);
            else
                fsl.SetVisibility(true);
        }

        private void TrayIconContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsShuttingDown) return;

            if (fsl.Visible && fsl.WindowState == FormWindowState.Normal)
                ShowMenuItem.Text = "Hide";
            else
                ShowMenuItem.Text = "Show";
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            if (IsShuttingDown)
                return;

            IsShuttingDown = true;

            if (fsl != null)
            {
                fsl.IsExiting = true;
                fsl.SaveWindowPosition();
            }

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
                                        fsl.SetVisibility(true);
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
                TrayIcon.Icon = Properties.Resources.TrayIconActive;
                PauseMenuItem.Text = "Pause";
            }
            else
            {
                TrayIcon.Icon = Properties.Resources.TrayIconPaused;
                PauseMenuItem.Text = "Resume";
            }
        }

		void OnFormExitRequested()
		{
			if (IsShuttingDown)
				return;

			IsShuttingDown = true;

			Shutdown();

			ExitThread();
		}

        void Shutdown()
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