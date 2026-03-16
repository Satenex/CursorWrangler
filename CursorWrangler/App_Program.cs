using System;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;

namespace CursorWrangler
{
    internal static class Program
    {
        static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool created;

            mutex = new Mutex(true, "CursorWranglerSingleton", out created);

            if (!created)
            {
                try
                {
                    using (var client = new NamedPipeClientStream(".", "CursorWranglerPipe", PipeDirection.Out))
                    {
                        client.Connect(200);
                    }
                }
                catch { }

                return;
            }

            ConfigManager.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new CursorWranglerContext());
        }
    }
}