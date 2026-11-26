using Fatcat.Slothy.Bakery.Modes;
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
        /// <summary>Saved round index within the active Story chapter (0-based).</summary>
        public int currentRoundIndex = 0;
        /// <summary>Last active Story scene name.</summary>
        public string currentSceneName = "";
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
            if (BakerySession.OffStory) return;
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
                : JsonUtility.FromJson<BakeryProgressData>(json) ?? new BakeryProgressData();
        }

        /// <summary>Wipes all saved progress (use for "New Game" option).</summary>
        public void DeleteProgress()
        {
            PlayerPrefs.DeleteKey(GameConstants.PREFS_PLAYER_PROGRESS);
            PlayerPrefs.Save();
            _progress = new BakeryProgressData();
            GameLogger.Log("Progress deleted.", LogCategory.Save);
        }

        /// <summary>Resets active chapter & round checkpoint for a fresh Story run while preserving completions.</summary>
        public void ResetForNewStoryGame()
        {
            if (BakerySession.OffStory) return;
            _progress ??= new BakeryProgressData();
            _progress.unlockedState = 0;
            _progress.currentRoundIndex = 0;
            _progress.currentSceneName = INART.SlothyBakery.SceneTransition.SceneUtils.FirstMinigameScene;
            Save();
        }

        /// <summary>Records chapter entry on Story without clobbering an existing round checkpoint for the same chapter.</summary>
        public void EnterMinigame(int chapterState, string sceneName)
        {
            if (BakerySession.OffStory) return;
            _progress ??= new BakeryProgressData();
            bool sameChapter = HasSavedProgress() && _progress.unlockedState == chapterState;
            _progress.unlockedState = chapterState;
            _progress.currentSceneName = sceneName;
            if (!sameChapter)
                _progress.currentRoundIndex = 0;
            Save();
        }

        /// <summary>Persists round checkpoint inside the active Story chapter.</summary>
        public void SaveRoundCheckpoint(int chapterState, string sceneName, int nextRoundIndex)
        {
            if (BakerySession.OffStory) return;
            _progress ??= new BakeryProgressData();
            _progress.unlockedState = chapterState;
            _progress.currentSceneName = sceneName;
            _progress.currentRoundIndex = Mathf.Max(0, nextRoundIndex);
            Save();
        }

        /// <summary>Returns saved round index if playing Story in the matching chapter; 0 in OffStory modes.</summary>
        public int GetRoundCheckpoint(int expectedChapterState)
        {
            if (BakerySession.OffStory || _progress == null || !HasSavedProgress())
                return 0;
            return _progress.unlockedState == expectedChapterState ? Mathf.Max(0, _progress.currentRoundIndex) : 0;
        }

        /// <summary>Advances Story progress when the last round of a chapter completes.</summary>
        public void CompleteStoryChapter(int nextChapterState, string nextSceneName, bool finishedStory)
        {
            if (BakerySession.OffStory) return;
            _progress ??= new BakeryProgressData();
            if (finishedStory)
            {
                _progress.completions = Mathf.Max(0, _progress.completions) + 1;
                _progress.unlockedState = 0;
                _progress.currentRoundIndex = 0;
                _progress.currentSceneName = INART.SlothyBakery.SceneTransition.SceneUtils.FirstMinigameScene;
                BakeryStats.Add("story.finished");
            }
            else
            {
                _progress.unlockedState = nextChapterState;
                _progress.currentRoundIndex = 0;
                _progress.currentSceneName = nextSceneName;
            }
            Save();
        }

        /// <summary>True if an in-progress Story run exists beyond the very first unplayed exercise.</summary>
        public bool HasSavedProgress()
            => _progress != null
               && !string.IsNullOrEmpty(PlayerPrefs.GetString(GameConstants.PREFS_PLAYER_PROGRESS, ""))
               && (_progress.unlockedState > 0 || _progress.currentRoundIndex > 0);

        // ── Legacy compatibility API ───────────────────────────────────────────
        // Keep these so existing minigame scripts don't need to change yet.

        /// <summary>Returns the current unlocked state. Use Progress.unlockedState for new code.</summary>
        public int GetActualState() => _progress.unlockedState;

        /// <summary>Sets the current unlocked state. Use Progress.unlockedState for new code.</summary>
        public void SetActualState(int state)
        {
            if (BakerySession.OffStory) return;
            if (_progress.unlockedState != state)
                _progress.currentRoundIndex = 0;
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
            if (newGameButton != null) newGameButton.gameObject.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
        }
    }
}