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
        private static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "RibbonLibDesign.log");

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
