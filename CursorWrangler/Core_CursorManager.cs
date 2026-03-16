using System.Drawing;
using System.Windows.Forms;

namespace CursorWrangler
{
    public class CursorManager
    {
        public void Update(WindowInfo info)
        {
            if (AppState.Paused)
                return;

            if (info.IsFullscreen && !AppState.CursorLocked)
            {
                LockCursor(info);
            }
            else if (!info.IsFullscreen && AppState.CursorLocked)
            {
                UnlockCursor();
            }
        }

        public void LockCursor(WindowInfo info)
        {
            Screen screen = Screen.FromHandle(info.Hwnd);

            Cursor.Clip = new Rectangle(
				info.Rect.Left,
				info.Rect.Top,
				info.Rect.Right - info.Rect.Left,
				info.Rect.Bottom - info.Rect.Top);

            AppState.CursorLocked = true;
        }

        public void UnlockCursor()
        {
            Cursor.Clip = Rectangle.Empty;

            AppState.CursorLocked = false;
        }
    }
}