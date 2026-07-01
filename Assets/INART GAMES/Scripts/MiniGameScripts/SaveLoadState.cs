using UnityEngine;
using UnityEngine.UI;
using INART.Framework.Core;

namespace INART.SlothyBakery.Base
{
    using Core;

    // ── Save data ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Player progress data for SlothysBakery.
    /// Add fields here as the game grows; the JSON serialization handles them automatically.
    /// </summary>
    [System.Serializable]
    public class BakeryProgressData
    {
        /// <summary>0 = fresh start, 1 = Minigame1 cleared, 2 = Minigame2 cleared, etc.</summary>
        public int  unlockedState  = 0;
        /// <summary>Total number of successful game completions.</summary>
        public int  completions    = 0;
        /// <summary>ISO-8601 timestamp of the last session (UTC).</summary>
        public string lastPlayedUtc = "";
    }

    // ── Manager ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Persistent save / load manager for SlothysBakery.
    /// Stores progress as JSON in PlayerPrefs (easy to swap for file-based save later).
    ///
    /// Usage:
    ///   SaveLoadState.Instance.Progress.unlockedState = 1;
    ///   SaveLoadState.Instance.Save();
    ///
    ///   var data = SaveLoadState.Instance.Progress;
    /// </summary>
    public class SaveLoadState : Singleton<SaveLoadState>
    {
        protected override bool PersistAcrossScenes => true;

        // ── State ──────────────────────────────────────────────────────────────

        private BakeryProgressData _progress;

        /// <summary>Live progress object. Modify directly then call Save().</summary>
        public BakeryProgressData Progress => _progress;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            Load();
            GameLogger.Log($"SaveLoadState loaded. UnlockedState={_progress.unlockedState}", LogCategory.Save);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Persists the current progress to PlayerPrefs.</summary>
        public void Save()
        {
            _progress.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(_progress);
            PlayerPrefs.SetString(GameConstants.PREFS_PLAYER_PROGRESS, json);
            PlayerPrefs.Save();
            GameLogger.Log("Progress saved.", LogCategory.Save);
        }

        /// <summary>Loads progress from PlayerPrefs. Creates a fresh save if none exists.</summary>
        public void Load()
        {
            string json = PlayerPrefs.GetString(GameConstants.PREFS_PLAYER_PROGRESS, "");
            _progress = string.IsNullOrEmpty(json)
                ? new BakeryProgressData()
                : JsonUtility.FromJson<BakeryProgressData>(json);
        }

        /// <summary>Wipes all saved progress (use for "New Game" option).</summary>
        public void DeleteProgress()
        {
            PlayerPrefs.DeleteKey(GameConstants.PREFS_PLAYER_PROGRESS);
            PlayerPrefs.Save();
            _progress = new BakeryProgressData();
            GameLogger.Log("Progress deleted.", LogCategory.Save);
        }

        /// <summary>True if a saved game exists.</summary>
        public bool HasSavedProgress()
            => !string.IsNullOrEmpty(PlayerPrefs.GetString(GameConstants.PREFS_PLAYER_PROGRESS, ""));

        // ── Legacy compatibility API ───────────────────────────────────────────
        // Keep these so existing minigame scripts don't need to change yet.

        /// <summary>Returns the current unlocked state. Use Progress.unlockedState for new code.</summary>
        public int GetActualState() => _progress.unlockedState;

        /// <summary>Sets the current unlocked state. Use Progress.unlockedState for new code.</summary>
        public void SetActualState(int state)
        {
            _progress.unlockedState = state;
        }

        // ── UI Helper ──────────────────────────────────────────────────────────

        /// <summary>
        /// Configures NewGame / Continue buttons based on whether a save exists.
        /// Call from your main menu's Start().
        /// </summary>
        public static void InitMenuButtons(Button newGameButton, Button continueButton)
        {
            bool hasSave = Instance != null && Instance.HasSavedProgress();
            newGameButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(hasSave);
        }
    }
}