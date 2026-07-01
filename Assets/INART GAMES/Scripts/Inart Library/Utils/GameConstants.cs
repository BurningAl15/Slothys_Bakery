namespace INART.Framework.Core
{
    /// <summary>
    /// Project-wide constants. Never use magic strings or magic numbers directly
    /// in game code — reference them here so changes propagate everywhere.
    /// </summary>
    public static class GameConstants
    {
        // ── Animation ──────────────────────────────────────────────────────────
        public const float DEFAULT_FADE_DURATION    = 0.4f;
        public const float DEFAULT_ANIM_DURATION    = 1.0f;
        public const float TYPEWRITER_LETTER_DELAY  = 0.05f;
        public const float DIALOGUE_PHRASE_DELAY    = 0.5f;

        // ── Audio ──────────────────────────────────────────────────────────────
        public const float DEFAULT_MUSIC_VOLUME     = 0.7f;
        public const float DEFAULT_SFX_VOLUME       = 0.9f;
        public const float DEFAULT_MASTER_VOLUME    = 1.0f;

        // ── Progression ────────────────────────────────────────────────────────
        public const int   TOTAL_STORY_LEVELS       = 18;
        public const int   MAX_STARS_PER_LEVEL      = 3;
        public const int   MINIGAME_COUNT           = 3;
        public const int   DAILY_CHALLENGES_PER_DAY = 3;

        // ── UI ─────────────────────────────────────────────────────────────────
        public const float HINT_DELAY               = 2.0f;
        public const float MESSAGE_DISPLAY_DURATION = 0.5f;
        public const float FRACTION_MENU_MAX_TIME   = 0.5f;

        // ── PlayerPrefs Keys ───────────────────────────────────────────────────
        // Game progress (SaveLoadState)
        public const string PREFS_PLAYER_PROGRESS   = "slothys_bakery_progress";

        // Daily challenge (DailyChallengeManager)
        public const string PREFS_DAILY_DATE        = "slothys_daily_date";
        public const string PREFS_DAILY_PROGRESS    = "slothys_daily_progress";
        public const string PREFS_DAILY_COMPLETED   = "slothys_daily_completed";

        // Analytics helpers
        public const string PREFS_SESSION_COUNT     = "analytics_session_count";
        public const string PREFS_COMPLETIONS_COUNT = "analytics_completions_count";

        // Rating / review gate
        public const string PREFS_RATING_DONE       = "rating_never_show";
        public const string PREFS_RATING_LAST_PROMPT= "rating_last_prompted_completion";
    }
}
