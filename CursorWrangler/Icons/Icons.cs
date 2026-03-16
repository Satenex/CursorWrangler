using System.Drawing;
using System.IO;
using System.Reflection;

namespace CursorWrangler
{
    public static class Icons
    {
        private static Icon Load(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string resource = "CursorWrangler.icons." + name;

            using (Stream s = asm.GetManifestResourceStream(resource))
            {
                if (s == null)
                    return SystemIcons.Application;

                return new Icon(s);
            }
        }

        public static Icon TrayActive
        {
            get { return Load("TrayIconActive.ico"); }
        }

        public static Icon TrayPaused
        {
            get { return Load("TrayIconPaused.ico"); }
        }
    }
}