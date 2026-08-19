using System.Collections.Generic;
using UnityEngine;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Master configuration asset for the full game flow.
    ///
    /// Create ONE asset per game (e.g. "SlothysBakery_FlowConfig") and assign it
    /// to the GameFlowManager component in your persistent Loader scene.
    ///
    ///   Right-click in Project → Create → INART → Modular → Game Flow Config
    ///
    /// The order of <see cref="minigames"/> is the order they are played.
    /// Add, remove, or reorder entries freely — no code changes needed.
    /// </summary>
    [CreateAssetMenu(
        menuName = "INART/Modular/Game Flow Config",
        fileName = "GameFlowConfig",
        order    = 9)]
    public class GameFlowConfigSO : ScriptableObject
    {
        // ── Minigame sequence ──────────────────────────────────────────────────

        [Tooltip(
            "Ordered list of minigames. Index 0 = first minigame after the main menu.\n" +
            "GameFlowManager maps each entry's sceneName to a scene in Build Settings.")]
        public List<MinigameDefinitionSO> minigames = new List<MinigameDefinitionSO>();

        // ── Special scenes ─────────────────────────────────────────────────────

        [Header("Special Scenes")]
        [Tooltip("Scene loaded when the player starts a new game from the main menu.")]
        public string mainMenuSceneName = "1. Main Screen";

        [Tooltip("Scene loaded after all minigames are completed.")]
        public string finalCutsceneSceneName = "5. FinalCutscene";

        // ── Progression visuals ────────────────────────────────────────────────

        [Header("Progression Bar Prefabs")]
        [Tooltip(
            "Prefab used by ModularProgressionCanvas to instantiate each bar cell.\n" +
            "Must have a ProgressionBarCell component at its root.")]
        public GameObject barCellPrefab;

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Total number of minigames in the flow.</summary>
        public int MinigameCount => minigames?.Count ?? 0;

        /// <summary>
        /// Returns the MinigameDefinitionSO at <paramref name="index"/>,
        /// or null if the index is out of range.
        /// </summary>
        public MinigameDefinitionSO GetMinigame(int index)
        {
            if (minigames == null || index < 0 || index >= minigames.Count)
                return null;
            return minigames[index];
        }

        /// <summary>
        /// Returns the 0-based index of the minigame whose sceneName matches
        /// <paramref name="sceneName"/>, or -1 if not found.
        /// </summary>
        public int IndexOf(string sceneName)
        {
            if (minigames == null) return -1;
            for (int i = 0; i < minigames.Count; i++)
                if (minigames[i] != null && minigames[i].sceneName == sceneName)
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns the total round count across all minigames.
        /// Useful for analytics (e.g. "completed X of Y total rounds").
        /// </summary>
        public int TotalRoundCount()
        {
            int total = 0;
            if (minigames == null) return total;
            foreach (var def in minigames)
                if (def != null) total += def.roundCount;
            return total;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn if any entry is null or has an empty sceneName
            if (minigames == null) return;
            for (int i = 0; i < minigames.Count; i++)
            {
                if (minigames[i] == null)
                {
                    Debug.LogWarning(
                        $"[GameFlowConfigSO] '{name}': entry at index {i} is null.", this);
                    continue;
                }
                if (string.IsNullOrEmpty(minigames[i].sceneName))
                    Debug.LogWarning(
                        $"[GameFlowConfigSO] '{name}': minigame at index {i} " +
                        $"('{minigames[i].name}') has no sceneName set.", this);
            }
        }
#endif
    }
}
