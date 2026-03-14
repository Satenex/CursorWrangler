using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CursorWrangler
{
    public static class Config
    {
        static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

		public static bool DebugLog = false;
        public static bool AlwaysOnTop = true;
        public static bool StartMinimized = false;
        public static bool MinimizeOnClose = true;
        public static bool ForceEnglishFullscreen = true;

        public static int WindowX = -1;
        public static int WindowY = -1;

        public static List<string> WindowsClassNotDetected =
            new List<string>()
            {
                "MediaPlayerClassicW",
                "Chrome_WidgetWin_1",
                "PotPlayer64"
            };

        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Save();
                return;
            }

            var lines = File.ReadAllLines(ConfigPath);

            foreach (var raw in lines)
            {
                string line = raw.Trim();

                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                int eq = line.IndexOf('=');

                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "DebugLog":
                        bool.TryParse(value, out DebugLog);
                        break;
						
                    case "AlwaysOnTop":
                        bool.TryParse(value, out AlwaysOnTop);
                        break;

                    case "StartMinimized":
                        bool.TryParse(value, out StartMinimized);
                        break;

                    case "MinimizeOnClose":
                        bool.TryParse(value, out MinimizeOnClose);
                        break;

                    case "ForceEnglishFullscreen":
                        bool.TryParse(value, out ForceEnglishFullscreen);
                        break;

                    case "WindowX":
                        int.TryParse(value, out WindowX);
                        break;

                    case "WindowY":
                        int.TryParse(value, out WindowY);
                        break;

                    case "WindowsClassNotDetected":

                        WindowsClassNotDetected = value
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .ToList();

                        break;
                }
            }

            EnsureDefaults();
        }

        static void EnsureDefaults()
        {
            if (WindowsClassNotDetected == null || WindowsClassNotDetected.Count == 0)
            {
                WindowsClassNotDetected = new List<string>()
                {
                    "MediaPlayerClassicW",
                    "Chrome_WidgetWin_1",
                    "PotPlayer64"
                };
            }
        }

        public static void Save()
        {
            try
            {
                string temp = ConfigPath + ".tmp";

                var lines = new List<string>()
                {
                    "# CursorWrangler configuration",
                    "# Lines starting with # are comments",
                    "",

                    "# Show debug information panel",
                    "DebugLog=" + DebugLog,
                    "",

                    "# Keep window above other windows",
                    "AlwaysOnTop=" + AlwaysOnTop,
                    "",

                    "# Start minimized to tray",
                    "StartMinimized=" + StartMinimized,
                    "",

                    "# Closing window minimizes to tray instead of exiting",
                    "MinimizeOnClose=" + MinimizeOnClose,
                    "",

                    "# Force English keyboard layout when fullscreen app is detected",
                    "ForceEnglishFullscreen=" + ForceEnglishFullscreen,
                    "",

                    "# Window classes that should NOT trigger fullscreen detection",
                    "# Separate multiple classes with commas",
                    "WindowsClassNotDetected=" +
                        string.Join(",", WindowsClassNotDetected),
                    "",

                    "# Last window position",
                    "WindowX=" + WindowX,
                    "WindowY=" + WindowY
                };

                File.WriteAllLines(temp, lines);

                if (File.Exists(ConfigPath))
                    File.Replace(temp, ConfigPath, null);
                else
                    File.Move(temp, ConfigPath);
            }
            catch
            {
                // silent fail
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        false))
                {
                    return key.GetValue("CursorWrangler") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        true))
                {
                    if (enable)
                        key.SetValue(
                            "CursorWrangler",
                            System.Windows.Forms.Application.ExecutablePath);
                    else
                        key.DeleteValue("CursorWrangler", false);
                }
            }
            catch
            {
            }
        }
    }
}