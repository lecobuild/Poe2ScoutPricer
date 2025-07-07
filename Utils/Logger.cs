// Utils/Logger.cs
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Poe2ScoutPricer.Utils
{
    public static class Logger
    {
        public static Action<string>? LogAction { get; set; }
        public static bool DebugEnabled { get; set; } = false;

        public static void LogInfo(string message, [CallerMemberName] string memberName = "")
        {
            Log($"[INFO] {memberName}: {message}");
        }

        public static void LogError(string message, [CallerMemberName] string memberName = "")
        {
            Log($"[ERROR] {memberName}: {message}");
        }

        public static void LogWarning(string message, [CallerMemberName] string memberName = "")
        {
            Log($"[WARNING] {memberName}: {message}");
        }

        public static void LogDebug(string message, [CallerMemberName] string memberName = "")
        {
            if (DebugEnabled)
            {
                Log($"[DEBUG] {memberName}: {message}");
            }
        }

        private static void Log(string message)
        {
            LogAction?.Invoke(message);
            Debug.WriteLine(message);
        }
    }
}