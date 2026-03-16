using System;

namespace CursorWrangler
{
    public struct WindowInfo
    {
        public IntPtr Hwnd;

        public string ProcessName;
        public string ProcessPath;
        public int ProcessId;

        public string Title;
        public string ClassName;

        public RECT Rect;

        public bool IsToolWindow;
        public bool IgnoredClass;

        public bool IsFullscreen;
		
		public string ProcessBitness;
    }
}