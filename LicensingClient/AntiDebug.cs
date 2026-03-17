using System;
using System.Diagnostics;
using System.Linq;

namespace LicensingClient
{
    public static class AntiDebug
    {
        public static void Check()
        {
            if (Debugger.IsAttached)
                Kill();

            if (IsDebuggerProcessRunning())
                Kill();
        }

        private static bool IsDebuggerProcessRunning()
        {
            string[] badProcesses =
            {
                "x64dbg",
                "dnspy",
                "cheatengine",
                "ollydbg",
                "ida"
            };

            var processes = Process.GetProcesses();

            foreach (var p in processes)
            {
                try
                {
                    string name = p.ProcessName.ToLower();

                    if (badProcesses.Any(b => name.Contains(b)))
                        return true;
                }
                catch { }
            }

            return false;
        }

        private static void Kill()
        {
#if DEBUG
            MessageBox.Show("Security violation (DEBUG MODE)", "Debug");
#else
    Environment.FailFast("Security violation");
#endif
        }
    }
}