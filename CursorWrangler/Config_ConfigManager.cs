using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CursorWrangler
{
	public static class ConfigManager
	{
		static readonly string ConfigPath =
		Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

		static Timer saveTimer;

		static readonly List<string> DefaultWindowsClassNotDetected = new List<string>()
		{
			"MediaPlayerClassicW",
			"Chrome_WidgetWin_1",
			"PotPlayer64"
		};

		public static bool DebugLog = false;
		public static bool AlwaysOnTop = true;
		public static bool StartMinimized = false;
		public static bool MinimizeOnClose = true;
		public static bool ForceEnglishFullscreen = true;

		public static int WindowX = -1;
		public static int WindowY = -1;

		public static event Action ConfigReloaded;

		public static List<string> WindowsClassNotDetected =
			new List<string>(DefaultWindowsClassNotDetected);

		static ConfigManager()
		{
			saveTimer = new Timer();
			saveTimer.Interval = 500;
			saveTimer.Tick += (s, e) =>
			{
				saveTimer.Stop();
				SaveImmediate();
			};
		}

		public static void Load()
		{
			if (!File.Exists(ConfigPath))
			{
				SaveImmediate();
				return;
			}

			var lines = File.ReadAllLines(ConfigPath);

			foreach (var raw in lines)
			{
				string line = raw.Trim();

				if (line.Length == 0 || line.StartsWith("#"))
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
							.Split(new char[] { ',', ';' },
								StringSplitOptions.RemoveEmptyEntries)
							.Select(x => x.Trim())
							.Where(x => x.Length > 0)
							.ToList();

						break;
				}
			}

			EnsureDefaults();
			ConfigReloaded?.Invoke();
		}

		static void EnsureDefaults()
		{
			if (WindowsClassNotDetected == null ||
				WindowsClassNotDetected.Count == 0)
			{
				WindowsClassNotDetected =
					new List<string>(DefaultWindowsClassNotDetected);
			}
		}

		public static void Save()
		{
			saveTimer.Stop();
			saveTimer.Start();
		}

		static void SaveImmediate()
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
							Application.ExecutablePath);
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