using System.Collections.Generic;
using System.Diagnostics;

namespace CursorWrangler
{
    public static class ProcessCache
    {
        private static Dictionary<int, ProcessInfo> cache = new Dictionary<int, ProcessInfo>();

        public static ProcessInfo Get(int pid)
        {
            if (cache.TryGetValue(pid, out var info))
                return info;

            try
            {
                var proc = Process.GetProcessById(pid);

                info = new ProcessInfo
                {
                    Name = proc.ProcessName,
                    Path = FullscreenDetector.GetProcessPathWMI(pid),
                    Bitness = FullscreenDetector.GetProcessBitness(proc)
                };

                cache[pid] = info;

                return info;
            }
            catch
            {
                return new ProcessInfo();
            }
        }
    }

    public class ProcessInfo
    {
        public string Name = "";
        public string Path = "";
        public string Bitness = "";
    }
}