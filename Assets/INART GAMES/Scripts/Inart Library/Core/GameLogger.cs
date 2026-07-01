using System;
using System.Diagnostics;

namespace INART.Framework.Core
{
    /// <summary>
    /// Centralized logging system. In production builds, only Warnings and Errors
    /// are emitted — everything else is stripped by [Conditional] at compile time.
    ///
    /// Usage:
    ///   GameLogger.Log("Player spawned", LogCategory.Gameplay);
    ///   GameLogger.Warning("Missing clip", LogCategory.Audio);
    ///   GameLogger.Error("Save failed",   LogCategory.Save);
    /// </summary>
    public static class GameLogger
    {
        // ── Configuration ──────────────────────────────────────────────────────
        private static LogLevel  _currentLevel      = LogLevel.Info;
        private static bool      _enableInProduction = false;
        private static bool      _showTimestamps     = false;
        private static bool      _showCategory       = true;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        private static readonly bool _isProductionBuild = true;
#else
        private static readonly bool _isProductionBuild = false;
#endif

        // ── Configuration API ──────────────────────────────────────────────────
        public static void SetLogLevel(LogLevel level)            => _currentLevel       = level;
        public static void EnableProductionLogs(bool enable)      => _enableInProduction = enable;
        public static void ShowTimestamps(bool show)              => _showTimestamps      = show;
        public static void ShowCategories(bool show)              => _showCategory        = show;

        // ── Public Logging API ─────────────────────────────────────────────────

        /// <summary>Detailed trace — only in Editor/DevBuild.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Verbose(string message, LogCategory category = LogCategory.System,
            UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Verbose)) return;
            UnityEngine.Debug.Log(Format(message, category, LogLevel.Verbose), context);
        }

        /// <summary>General info — only in Editor/DevBuild.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, LogCategory category = LogCategory.System,
            UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            UnityEngine.Debug.Log(Format(message, category, LogLevel.Info), context);
        }

        /// <summary>Non-critical warning — visible in production if EnableProductionLogs(true).</summary>
        public static void Warning(string message, LogCategory category = LogCategory.System,
            UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            UnityEngine.Debug.LogWarning(Format(message, category, LogLevel.Warning), context);
        }

        /// <summary>Critical error — always visible in production.</summary>
        public static void Error(string message, LogCategory category = LogCategory.System,
            UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            UnityEngine.Debug.LogError(Format(message, category, LogLevel.Error), context);
        }

        /// <summary>Logs an exception with full stack trace.</summary>
        public static void Exception(Exception exception, LogCategory category = LogCategory.System,
            UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            UnityEngine.Debug.LogException(exception, context);
        }

        // ── Internals ──────────────────────────────────────────────────────────
        private static bool ShouldLog(LogLevel level)
        {
            if (_isProductionBuild && !_enableInProduction && level < LogLevel.Error)
                return false;
            return level >= _currentLevel;
        }

        private static string Format(string message, LogCategory category, LogLevel level)
        {
            string ts  = _showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            string cat = _showCategory   ? $"[{category}] "              : "";
            string lvl = level switch
            {
                LogLevel.Verbose => "<color=grey>[V]</color> ",
                LogLevel.Info    => "<color=white>[I]</color> ",
                LogLevel.Warning => "<color=yellow>[W]</color> ",
                LogLevel.Error   => "<color=red>[E]</color> ",
                _                => ""
            };
            return $"{ts}{cat}{lvl}{message}";
        }
    }
}
