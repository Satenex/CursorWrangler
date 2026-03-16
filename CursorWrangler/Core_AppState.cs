using System;

namespace CursorWrangler
{
    public static class AppState
    {
        public static bool Paused { get; set; } = false;

        public static bool CursorLocked { get; set; } = false;

        public static bool FullscreenActive { get; set; } = false;

        public static bool LayoutForced { get; set; } = false;

        public static IntPtr ActiveWindow = IntPtr.Zero;

        public static string ActiveProcess = "";
    }
}