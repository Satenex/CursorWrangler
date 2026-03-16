using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CursorWrangler
{
    public static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_TOOLWINDOW = 0x00000080;

        public const long WS_BORDER = 0x00800000;
        public const long WS_THICKFRAME = 0x00040000;
        public const long WS_CAPTION = 0x00C00000;
		
		public const int VK_SCROLL = 0x91;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);   // ← ДОБАВИТЬ

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadKeyboardLayout(string id, uint flags);

        [DllImport("user32.dll")]
        public static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
		
		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);
		
		[DllImport("user32.dll")]
		public static extern bool HideCaret(IntPtr hWnd);
    }
}