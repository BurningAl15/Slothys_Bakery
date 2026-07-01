using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using INART.Framework.Core;
using INART.Framework.Core.Services;

// These namespaces require com.unity.services.analytics and com.unity.services.core
// Install via Package Manager → Add by name → com.unity.services.analytics
#if UNITY_SERVICES_ANALYTICS_ENABLED || UNITY_ANALYTICS
using Unity.Services.Analytics;
using Unity.Services.Core;
#endif

namespace INART.SlothyBakery.Analytics
{
    using Core;

    /// <summary>
    /// Centralized analytics manager using Unity Gaming Services Analytics SDK 6.x.
    ///
    /// SETUP (one-time):
    ///   1. https://cloud.unity.com → project → Analytics → Enable
    ///   2. Add this component to the same persistent GameObject as SettingsManager
    ///      (Loader scene with DontDestroyOnLoad).
    ///   3. Link it as IAnalyticsService in GameManager if you want ServiceLocator access.
    ///
    /// Generic events (all Slothy games):
    ///   session_start       — app opened
    ///   game_started        — minigame scene loaded
    ///   answer_correct      — correct answer selected
    ///   answer_wrong        — wrong answer selected
    ///   round_completed     — round finished
    ///   game_completed      — all rounds finished
    ///   settings_changed    — player changed a setting
    ///   daily_challenge_completed
    ///
    /// Bakery-specific events (added via TrackCustom):
    ///   fraction_compared   — player compared two fractions
    ///   sign_placed         — player placed a comparison sign
    /// </summary>
    public class AnalyticsManager : Singleton<AnalyticsManager>, IAnalyticsService
    {
        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── State ──────────────────────────────────────────────────────────────
        private bool _isInitialized;

        // ── PlayerPrefs Keys (shared with RatingManager / DailyChallengeManager) ──
        public static class Keys
        {
            public const string SESSION_COUNT     = "analytics_session_count";
            public const string COMPLETIONS_COUNT = "analytics_completions_count";
        }

        // ── Event name constants ───────────────────────────────────────────────
        private static class E
        {
            public const string SESSION_START              = "session_start";
            public const string GAME_STARTED               = "game_started";
            public const string ANSWER_CORRECT             = "answer_correct";
            public const string ANSWER_WRONG               = "answer_wrong";
            public const string ROUND_COMPLETED            = "round_completed";
            public const string GAME_COMPLETED             = "game_completed";
            public const string SETTINGS_CHANGED           = "settings_changed";
            public const string DAILY_CHALLENGE_COMPLETED  = "daily_challenge_completed";
        }

        // ── IGameService ───────────────────────────────────────────────────────

        public void Initialize()
        {
            // Async init is kicked off in OnAwake — this satisfies the interface.
        }

        public void Shutdown()
        {
            GameLogger.Log("AnalyticsManager shutdown.", LogCategory.Analytics);
        }

        // ── Singleton lifecycle ────────────────────────────────────────────────

        protected override void OnAwake()
        {
            // Register with ServiceLocator so game code can resolve IAnalyticsService
            if (!ServiceLocator.Has<IAnalyticsService>())
                ServiceLocator.Register<IAnalyticsService>(this);

            _ = InitUGSAsync();
        }

        // ── UGS initialization ─────────────────────────────────────────────────

        private async Task InitUGSAsync()
        {
#if UNITY_SERVICES_ANALYTICS_ENABLED || UNITY_ANALYTICS
            try
            {
                await UnityServices.InitializeAsync();
                _isInitialized = true;
                GameLogger.Log("UGS Analytics initialized.", LogCategory.Analytics);

                int sessions = PlayerPrefs.GetInt(Keys.SESSION_COUNT, 0) + 1;
                PlayerPrefs.SetInt(Keys.SESSION_COUNT, sessions);
                PlayerPrefs.Save();

                SendEvent(E.SESSION_START, new Dictionary<string, object>
                {
                    { "session_number", sessions },
                    { "first_scene",    SceneManager.GetActiveScene().name },
                    { "platform",       Application.platform.ToString() }
                });
            }
            catch (Exception ex)
            {
                GameLogger.Warning($"UGS Analytics init failed: {ex.Message}", LogCategory.Analytics);
            }
#else
            GameLogger.Warning(
                "UGS Analytics package not installed. Install com.unity.services.analytics via Package Manager.",
                LogCategory.Analytics);
            await Task.CompletedTask;
#endif
        }

        // ── IAnalyticsService — generic events ────────────────────────────────

        public void TrackEvent(string eventName)
            => SendEvent(eventName);

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            => SendEvent(eventName, parameters);

        public void TrackLevelStarted(string levelName)
            => SendEvent(E.GAME_STARTED, new Dictionary<string, object>
            {
                { "level_name",     levelName },
                { "session_number", PlayerPrefs.GetInt(Keys.SESSION_COUNT, 1) }
            });

        public void TrackLevelCompleted(string levelName, bool success)
            => SendEvent(E.ROUND_COMPLETED, new Dictionary<string, object>
            {
                { "level_name", levelName },
                { "success",    success }
            });

        public void TrackAnswerCorrect(int roundIndex)
            => SendEvent(E.ANSWER_CORRECT, new Dictionary<string, object>
            {
                { "round_index", roundIndex }
            });

        public void TrackAnswerWrong(int roundIndex)
            => SendEvent(E.ANSWER_WRONG, new Dictionary<string, object>
            {
                { "round_index", roundIndex }
            });

        /// <summary>
        /// Call when the player finishes all rounds.
        /// Increments the completions counter used by RatingManager and DailyChallengeManager.
        /// </summary>
        public void TrackGameCompleted()
        {
            int completions = PlayerPrefs.GetInt(Keys.COMPLETIONS_COUNT, 0) + 1;
            PlayerPrefs.SetInt(Keys.COMPLETIONS_COUNT, completions);
            PlayerPrefs.Save();

            SendEvent(E.GAME_COMPLETED, new Dictionary<string, object>
            {
                { "completions_total", completions },
                { "session_number",    PlayerPrefs.GetInt(Keys.SESSION_COUNT, 1) }
            });
        }

        // ── Extra events shared across all Slothy games ────────────────────────

        public void TrackSettingsChanged(string settingKey, object newValue)
            => SendEvent(E.SETTINGS_CHANGED, new Dictionary<string, object>
            {
                { "setting", settingKey },
                { "value",   newValue?.ToString() ?? "null" }
            });

        public void TrackDailyChallengeCompleted(string challengeType, int progress)
            => SendEvent(E.DAILY_CHALLENGE_COMPLETED, new Dictionary<string, object>
            {
                { "challenge_type", challengeType },
                { "progress",       progress }
            });

        /// <summary>
        /// Open-ended hook for game-specific events.
        /// Example (Bakery):
        ///   AnalyticsManager.Instance.TrackCustom("fraction_compared",
        ///       new() { {"numerator_left", 1}, {"denominator_left", 2} });
        /// </summary>
        public void TrackCustom(string eventName, Dictionary<string, object> parameters = null)
            => SendEvent(eventName, parameters);

        // ── Helpers ────────────────────────────────────────────────────────────

        public int SessionCount     => PlayerPrefs.GetInt(Keys.SESSION_COUNT, 0);
        public int CompletionsCount => PlayerPrefs.GetInt(Keys.COMPLETIONS_COUNT, 0);

        // ── Internal send ──────────────────────────────────────────────────────

        private void SendEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
            {
                GameLogger.Verbose($"Skipping '{eventName}' — UGS not initialized yet.",
                    LogCategory.Analytics);
                return;
            }

            GameLogger.Log($"Event: {eventName}", LogCategory.Analytics);

#if UNITY_SERVICES_ANALYTICS_ENABLED || UNITY_ANALYTICS
            try
            {
                var ev = new CustomEvent(eventName);
                if (parameters != null)
                    foreach (var kv in parameters)
                        ev.Add(kv.Key, kv.Value);

                AnalyticsService.Instance.RecordEvent(ev);
            }
            catch (Exception ex)
            {
                GameLogger.Warning($"Failed to send '{eventName}': {ex.Message}",
                    LogCategory.Analytics);
            }
#endif
        }
    }
}
