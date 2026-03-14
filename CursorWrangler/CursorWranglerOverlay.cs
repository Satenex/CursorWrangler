using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

class DebugWindow : Form
{
    RichTextBox logBox = new RichTextBox();

    public DebugWindow()
    {
        Text = "Debug";
        TopMost = true;

        Width = 400;
        Height = 250;

        StartPosition = FormStartPosition.Manual;
        Location = new Point(50, 50); // Начальная позиция, можно будет изменить позже

        logBox.Dock = DockStyle.Fill;
        logBox.ReadOnly = true;
        logBox.Font = new Font("Consolas", 10);
        logBox.BackColor = Color.Black;
        logBox.ForeColor = Color.White;
        logBox.BorderStyle = BorderStyle.None;

        Controls.Add(logBox);
    }

	public void UpdateText(string text)
	{
		logBox.AppendText(text + Environment.NewLine);
		logBox.SelectionStart = logBox.Text.Length;
		logBox.ScrollToCaret();
	}

    public void UpdatePosition(int x, int y)
    {
        // Обновляем позицию окна оверлея
        Location = new Point(x, y);
    }
}

class OverlayServer
{
    DebugWindow window;

    public OverlayServer(DebugWindow w)
    {
        window = w;
    }

    public void Start()
    {
        Task.Run(ServerLoop);
    }

    async Task ServerLoop()
    {
        while (true)
        {
            using (var pipe = new NamedPipeServerStream("CursorWranglerDebug", PipeDirection.In))
            {
                await pipe.WaitForConnectionAsync();

                using (var reader = new StreamReader(pipe))
                {
                    while (!reader.EndOfStream)
                    {
						string text = await reader.ReadLineAsync();

						if (text.StartsWith("MoveOverlay"))
						{
							string[] parts = text.Split(' ');
							if (parts.Length == 3)
							{
								int x = int.Parse(parts[1]);
								int y = int.Parse(parts[2]);

								window.BeginInvoke((Action)(() =>
								{
									window.Location = new Point(x, y);
								}));
							}
						}
						else
						{
							window.BeginInvoke((Action)(() =>
							{
								window.UpdateText(text.Replace("\\n", "\n"));
							}));
						}
                    }
                }
            }
        }
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        DebugWindow win = new DebugWindow();

        OverlayServer server = new OverlayServer(win);
        server.Start();

        Application.Run(win);
    }
}