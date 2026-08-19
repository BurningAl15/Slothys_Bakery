using UnityEngine;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Data descriptor for a single minigame in the flow.
    /// 
    /// Create one asset per minigame:
    ///   Right-click in Project → Create → INART → Modular → Minigame Definition
    /// 
    /// The GameFlowConfigSO holds an ordered list of these.
    /// GameFlowManager reads the list at runtime — no code changes needed
    /// when you add, remove, or reorder minigames.
    /// </summary>
    [CreateAssetMenu(
        menuName = "INART/Modular/Minigame Definition",
        fileName = "MinigameDef_New",
        order    = 10)]
    public class MinigameDefinitionSO : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────────

        [Tooltip("Must match EXACTLY the scene name in Build Settings.")]
        public string sceneName = "";

        [Tooltip("Human-readable name shown in the menu / debug UI.")]
        public string displayName = "New Minigame";

        [Tooltip("Optional thumbnail shown in the level-select screen.")]
        public Sprite thumbnail;

        // ── Progression ────────────────────────────────────────────────────────

        [Tooltip(
            "How many rounds this minigame has.\n" +
            "ModularProgressionCanvas uses this to generate bars/locks procedurally — " +
            "no manual Inspector wiring needed.")]
        [Min(1)]
        public int roundCount = 1;

        // ── Analytics / Daily Challenge ────────────────────────────────────────

        [Tooltip(
            "Key passed to DailyChallengeManager.AddProgress() after each round.\n" +
            "Leave empty to skip daily challenge tracking for this minigame.")]
        public string dailyChallengeKey = "complete_rounds";

        [Tooltip(
            "Key passed to AnalyticsManager.TrackLevelCompleted().\n" +
            "Defaults to sceneName if left empty.")]
        public string analyticsKey = "";

        // ── Unlock ─────────────────────────────────────────────────────────────

        [Tooltip(
            "If true this minigame is always unlocked regardless of save state.\n" +
            "Useful for the first minigame or for testing.")]
        public bool alwaysUnlocked = false;

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the analytics key to use, falling back to sceneName if none is set.
        /// </summary>
        public string ResolvedAnalyticsKey =>
            string.IsNullOrEmpty(analyticsKey) ? sceneName : analyticsKey;
    }
}
