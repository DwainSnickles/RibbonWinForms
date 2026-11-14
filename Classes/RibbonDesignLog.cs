using System;
using System.IO;

namespace RibbonLib
{
    /// <summary>
    /// Temporary diagnostics for design-time failures that the out-of-process designer
    /// cannot surface (its error dialog itself crashes without IUIService). Writes to
    /// %TEMP%\RibbonLibDesign.log so the real exception can be captured.
    /// </summary>
    internal static class RibbonDesignLog
    {
        // RIBBONLIB_DESIGN_LOG overrides the destination. On a machine that has the
        // repository checked out at the baked-in path the log lands there; everywhere
        // else (end users, CI) it falls back to %TEMP%.
        private const string RepoDir = @"E:\repos\RibbonLib";

        private static string LogPath =>
            Environment.GetEnvironmentVariable("RIBBONLIB_DESIGN_LOG")
            ?? (Directory.Exists(RepoDir)
                ? Path.Combine(RepoDir, "RibbonLibDesign.log")
                : Path.Combine(Path.GetTempPath(), "RibbonLibDesign.log"));

        internal static void Write(string message)
        {
            try
            {
                File.AppendAllText(
                    LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never break the designer.
            }
        }

        internal static void Exception(string context, Exception ex)
        {
            Write($"{context}: {ex}");
        }
    }
}
