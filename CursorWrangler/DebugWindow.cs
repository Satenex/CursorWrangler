using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CursorWrangler
{
    public class DebugWindow : Form
    {
        RichTextBox logBox = new RichTextBox();

        string lastText = "";

        int baseWidth = 300;
        int baseHeight = 200;

        Font font = new Font("Consolas", 10);

        const int PAD_X = 16;
        const int PAD_Y = 12;

        const int WM_MOUSEACTIVATE = 0x21;
        const int MA_NOACTIVATE = 3;

        public Action DebugClosed;

        public DebugWindow(Form owner)
        {
            Text = "Debug / Press [Scroll Lock] => freeze";

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            TopMost = true;

            BackColor = Color.Black;
            DoubleBuffered = true;

            Width = baseWidth;
            Height = baseHeight;

            logBox.ReadOnly = true;
            logBox.Font = font;

            logBox.BackColor = Color.Black;
            logBox.ForeColor = Color.White;

            logBox.BorderStyle = BorderStyle.None;

            logBox.Location = new Point(10, 10);
            logBox.Size = new Size(Width - 20, Height - 20);

            logBox.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            logBox.WordWrap = false;
            logBox.ScrollBars = RichTextBoxScrollBars.None;

            // ключевая строка — выделение не исчезает
            logBox.HideSelection = false;

            Controls.Add(logBox);

            FormClosed += (s, e) => DebugClosed?.Invoke();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            logBox.Focus();
        }

        public void SetBaseSize(int w, int h)
        {
            baseWidth = w;
            baseHeight = h;

            Width = w;
            Height = h;
        }

        public void UpdateText(string text)
        {
            if (IsDisposed)
                return;

            text = text.Replace("\r", "");

            if (text == lastText)
                return;

            lastText = text;

            string[] lines = text.Split('\n');

            logBox.SuspendLayout();
            logBox.Rtf = BuildRtf(lines);
            logBox.ResumeLayout();

            AutoResize(lines);
        }

        string BuildRtf(string[] lines)
        {
            StringBuilder rtf = new StringBuilder();

            rtf.Append(@"{\rtf1\ansi\deff0");

            rtf.Append(@"{\colortbl;");
            rtf.Append(ColorToRtf(Color.LightGray));
            rtf.Append(ColorToRtf(Color.Gray));
            rtf.Append(ColorToRtf(Color.White));
            rtf.Append(ColorToRtf(Color.LimeGreen));
            rtf.Append(ColorToRtf(Color.Red));
            rtf.Append(ColorToRtf(Color.Yellow));
            rtf.Append(ColorToRtf(Color.LightSkyBlue));
            rtf.Append(ColorToRtf(Color.Cyan));
            rtf.Append(ColorToRtf(Color.Orange));
            rtf.Append(ColorToRtf(Color.Violet));
            rtf.Append("}");

            foreach (var raw in lines)
            {
                string line = raw.TrimEnd('\r');

                if (line.Length == 0)
                {
                    rtf.Append(@"\par ");
                    continue;
                }

                int sep = line.IndexOf(':');

                if (sep < 0)
                {
                    rtf.Append(@"\cf3 ");
                    rtf.Append(Escape(line));
                    rtf.Append(@"\par ");
                    continue;
                }

                string key = line.Substring(0, sep);
                string value = line.Substring(sep + 1).Trim();

                rtf.Append(@"\cf1 ");
                rtf.Append(Escape(key));

                rtf.Append(@"\cf2 : ");

                rtf.Append(GetValueColor(key, value));
                rtf.Append(Escape(value));

                rtf.Append(@"\par ");
            }

            rtf.Append("}");

            return rtf.ToString();
        }

        string GetValueColor(string key, string value)
        {
            string k = key.ToLower();
            string v = value.ToLower();

            if (v == "true") return @"\cf4 ";
            if (v == "false") return @"\cf5 ";

            if (k.Contains("process")) return @"\cf6 ";
            if (k.Contains("path")) return @"\cf7 ";
            if (k.Contains("hwnd")) return @"\cf8 ";
            if (k.Contains("rect") || k.Contains("size")) return @"\cf9 ";
            if (k.Contains("layout")) return @"\cf10 ";

            return @"\cf3 ";
        }

        string ColorToRtf(Color c)
        {
            return $@"\red{c.R}\green{c.G}\blue{c.B};";
        }

        string Escape(string text)
        {
            return text
                .Replace(@"\", @"\\")
                .Replace("{", @"\{")
                .Replace("}", @"\}");
        }

        void AutoResize(string[] lines)
        {
            using (Graphics g = CreateGraphics())
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

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;

                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                cp.ExStyle |= WS_EX_TOOLWINDOW;

                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }
    }
}