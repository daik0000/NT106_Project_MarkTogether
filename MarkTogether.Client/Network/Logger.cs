using System;
using System.Diagnostics;
using System.IO;

namespace MarkTogether.Client.Network
{
    /// <summary>
    /// Helper class for safe logging that supports multiple client instances.
    /// Writes to a per-process log file to avoid file lock issues.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;

        static Logger()
        {
            try
            {
                int pid = Process.GetCurrentProcess().Id;
                LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"client_debug_{pid}.log");
            }
            catch
            {
                LogFilePath = "client_debug_fallback.log";
            }
        }

        public static void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
                Debug.Write(line);
                File.AppendAllText(LogFilePath, line);
            }
            catch
            {
                // Never crash the UI due to logging failure
            }
        }
    }
}
