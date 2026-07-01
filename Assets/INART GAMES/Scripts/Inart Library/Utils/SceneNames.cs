namespace INART.SlothyBakery.Base
{
    /// <summary>
    /// Centralized scene name constants to avoid magic strings and typos.
    /// Update this file when adding new scenes to the project.
    /// 
    /// Usage:
    /// <code>
    /// if (SceneManager.GetActiveScene().name == SceneNames.MINIGAME_1)
    /// {
    ///     // Do something
    /// }
    /// </code>
    /// </summary>
    public static class SceneNames
    {
        public const string RESOURCES_LOADER = "0. ResourcesLoader";
        
        /// <summary>Initial menu / main menu scene</summary>
        public const string INITIAL_MENU = "1. Main Screen";
        
        /// <summary>First minigame - Fraction comparison</summary>
        public const string MINIGAME_1 = "2. Minigame1";
        
        /// <summary>Second minigame - Sign selection</summary>
        public const string MINIGAME_2 = "3. Minigame2";
        
        /// <summary>Third minigame - Fraction equivalence</summary>
        public const string MINIGAME_3 = "4. Minigame3";
        
        /// <summary>Final cutscene / ending</summary>
        public const string FINAL_CUTSCENE = "5. FinalCutscene";

        // Add new scenes here as the project grows
        // Example:
        // /// <summary>Daily challenge mode</summary>
        // public const string DAILY_CHALLENGE = "DailyChallenge";
    }
}
