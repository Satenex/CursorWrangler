using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CursorWrangler
{
    public class DebugWindow : Form
    {
        static DebugWindow instance;

        Form followOwner;

        enum DockSide
        {
            Right,
            Left,
            Bottom,
            Top
        }

        DockSide dockSide = DockSide.Right;

        RichTextBox logBox = new RichTextBox();

        string lastText = "";

        bool frozen = false;
        bool lastScrollDown = false;

        Timer keyTimer;

        int baseWidth = 300;
        int baseHeight = 200;

        Font font = new Font("Consolas", 10);

        const int PAD_Y = 12;
        const int MaxLogLines = 1000;

        public Action DebugClosed;

        public static DebugWindow Get()
        {
            if (instance == null || instance.IsDisposed)
                instance = new DebugWindow();

            return instance;
        }

        public DebugWindow()
        {
            Text = "Debug / Press [Scroll Lock] => freeze";

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;

            BackColor = Color.Black;
            Width = baseWidth;
            Height = baseHeight;

            logBox.ReadOnly = true;
            logBox.Font = font;
            logBox.BackColor = Color.Black;
            logBox.ForeColor = Color.White;
            logBox.BorderStyle = BorderStyle.None;
            logBox.DetectUrls = false;

            logBox.Location = new Point(10, 10);
            logBox.Size = new Size(Width - 20, Height - 20);

            logBox.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            logBox.WordWrap = false;
            logBox.ScrollBars = RichTextBoxScrollBars.None;
            logBox.HideSelection = false;

            logBox.GotFocus += (s, e) =>
            {
                NativeMethods.HideCaret(logBox.Handle);
            };

            logBox.MouseDown += (s, e) =>
            {
                NativeMethods.HideCaret(logBox.Handle);
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Copy", null, (s, e) => logBox.Copy());
            menu.Items.Add("Select All", null, (s, e) => logBox.SelectAll());

            logBox.ContextMenuStrip = menu;

            Controls.Add(logBox);

            FormClosed += (s, e) => DebugClosed?.Invoke();

            keyTimer = new Timer();
            keyTimer.Interval = 16;
            keyTimer.Tick += KeyTimer_Tick;
            keyTimer.Start();

            this.Deactivate += DebugWindow_Deactivate;
        }

        void KeyTimer_Tick(object sender, EventArgs e)
        {
            bool scrollDown =
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SCROLL) & 0x8000) != 0;

            if (scrollDown && !lastScrollDown)
            {
                frozen = !frozen;

                Color c = frozen ? Color.FromArgb(71, 0, 0) : Color.Black;

                BackColor = c;
                logBox.BackColor = c;

                Text = frozen
                    ? "Debug [FROZEN] / Press [Scroll Lock] => UNfreeze"
                    : "Debug / Press [Scroll Lock] => freeze";
            }

            lastScrollDown = scrollDown;
        }

        void DebugWindow_Deactivate(object sender, EventArgs e)
        {
            logBox.DeselectAll();
        }

        public void Follow(Form owner)
        {
            if (owner == null || owner.IsDisposed)
                return;

            followOwner = owner;

            Screen screen = Screen.FromControl(owner);
            Rectangle bounds = screen.WorkingArea;

            int margin = 10;

            int xRight = owner.Right + margin;
            int xLeft = owner.Left - Width - margin;

            int yTop = owner.Top;
            int yBottom = owner.Bottom + margin;
            int yAbove = owner.Top - Height - margin;

            int x = xRight;
            int y = yTop;

            if (xRight + Width <= bounds.Right)
            {
                dockSide = DockSide.Right;
                x = xRight;
                y = yTop;
            }
            else if (xLeft >= bounds.Left)
            {
                dockSide = DockSide.Left;
                x = xLeft;
                y = yTop;
            }
            else if (yBottom + Height <= bounds.Bottom)
            {
                dockSide = DockSide.Bottom;
                x = owner.Left;
                y = yBottom;
            }
            else if (yAbove >= bounds.Top)
            {
                dockSide = DockSide.Top;
                x = owner.Left;
                y = yAbove;
            }

            if (x + Width > bounds.Right)
                x = bounds.Right - Width;

            if (x < bounds.Left)
                x = bounds.Left;

            if (y + Height > bounds.Bottom)
                y = bounds.Bottom - Height;

            if (y < bounds.Top)
                y = bounds.Top;

            Location = new Point(x, y);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (followOwner == null || followOwner.IsDisposed)
                return;

            int margin = 10;

            if (dockSide == DockSide.Left)
                Location = new Point(followOwner.Left - Width - margin, Location.Y);

            else if (dockSide == DockSide.Right)
                Location = new Point(followOwner.Right + margin, Location.Y);

            else if (dockSide == DockSide.Top)
                Location = new Point(Location.X, followOwner.Top - Height - margin);

            else if (dockSide == DockSide.Bottom)
                Location = new Point(Location.X, followOwner.Bottom + margin);
        }

        public static void Log(string text)
        {
            var dbg = Get();

            if (dbg == null || dbg.IsDisposed)
                return;

            if (dbg.InvokeRequired)
            {
                dbg.BeginInvoke(new Action<string>(Log), text);
                return;
            }

            dbg.AppendLine(text);
        }

        public void AppendLine(string text)
        {
            if (frozen)
                return;

            bool hadSelection = logBox.SelectionLength > 0;

            int selStart = logBox.SelectionStart;
            int selLength = logBox.SelectionLength;

            if (hadSelection)
            {
                logBox.SelectionLength = 0;
                logBox.SelectionStart = logBox.TextLength;
            }

            logBox.AppendText(text + Environment.NewLine);

            if (logBox.Lines.Length > MaxLogLines)
            {
                var lines = logBox.Lines;

                logBox.Lines =
                    lines.Skip(lines.Length - MaxLogLines)
                         .ToArray();
            }

            if (hadSelection)
            {
                logBox.SelectionStart = selStart;
                logBox.SelectionLength = selLength;
            }
        }

        public void UpdateText(string text)
        {
            if (IsDisposed)
                return;

            if (frozen)
                return;

            if (logBox.SelectionLength > 0)
                return;

            text = text.Replace("\r", "");

            if (text == lastText)
                return;

            lastText = text;

            logBox.Clear();

            string[] lines = text.Split('\n');

            foreach (var raw in lines)
            {
                string line = raw.TrimEnd('\r');

                if (line.Length == 0)
                {
                    logBox.AppendText("\n");
                    continue;
                }

                int sep = line.IndexOf(':');

                if (sep < 0)
                {
                    logBox.SelectionColor = Color.White;
                    logBox.AppendText(line + "\n");
                    continue;
                }

                string key = line.Substring(0, sep);
                string value = line.Substring(sep + 1).Trim();

                logBox.SelectionColor = Color.LightGray;
                logBox.AppendText(key);

                logBox.SelectionColor = Color.Gray;
                logBox.AppendText(": ");

                logBox.SelectionColor = GetValueColor(key, value);
                logBox.AppendText(value + "\n");
            }

            AutoResize(lines);
        }

        Color GetValueColor(string key, string value)
        {
            string k = key.ToLower();
            string v = value.ToLower();

            if (v == "true") return Color.LimeGreen;
            if (v == "false") return Color.Red;

            if (k.Contains("process")) return Color.Yellow;
            if (k.Contains("path")) return Color.LightSkyBlue;
            if (k.Contains("hwnd")) return Color.Cyan;
            if (k.Contains("rect") || k.Contains("size")) return Color.Orange;
            if (k.Contains("layout")) return Color.Violet;

            return Color.White;
        }

        void AutoResize(string[] lines)
        {
            using (Graphics g = logBox.CreateGraphics())
            {
                int maxW = baseWidth;
                int totalH = PAD_Y;

                foreach (var line in lines)
                {
                    SizeF s = g.MeasureString(line, font);

                    maxW = Math.Max(maxW, (int)s.Width + 10);
                    totalH += font.Height;
                }

                totalH += PAD_Y + font.Height;

                Width = maxW;
                Height = Math.Max(baseHeight, totalH);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;

                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;

                return cp;
            }
        }
    }
}