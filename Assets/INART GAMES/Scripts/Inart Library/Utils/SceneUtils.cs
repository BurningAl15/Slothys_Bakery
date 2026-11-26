using UnityEngine;
using UnityEngine.SceneManagement;

namespace INART.SlothyBakery.SceneTransition
{
    /// <summary>
    /// Utility methods for scene navigation. All logic goes through here —
    /// never call SceneManager directly from game scripts.
    /// </summary>
    public static class SceneUtils
    {
        public const string MainMenuScene = "1. Main Screen";
        public const string FirstMinigameScene = "2. Minigame1";
        public const string SecondMinigameScene = "3. Minigame2";
        public const string ThirdMinigameScene = "4. Minigame3";
        public const string FinalScene = "5. FinalCutscene";

        // ── Navigation ─────────────────────────────────────────────────────────

        /// <summary>Loads scene 0 (usually the splash / main menu).</summary>
        public static void ToInitialScene()
            => SceneManager.LoadScene(0);

        /// <summary>Completes the current minigame chapter (handling Story and OffStory modes cleanly).</summary>
        public static void ToNextScene()
        {
            string current = CurrentSceneName;
            if (Fatcat.Slothy.Bakery.Modes.BakerySession.OffStory
                || current == FirstMinigameScene
                || current == SecondMinigameScene
                || current == ThirdMinigameScene)
            {
                Fatcat.Slothy.Bakery.Modes.BakerySession.CompleteCurrentChapter();
                return;
            }

            string next = current == MainMenuScene || current == "DEV_ModularMinigame" ? FirstMinigameScene : null;
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError($"[Bakery] No authored next-scene route from '{current}'.");
                return;
            }

            LoadScene(next);
        }

        /// <summary>Maps legacy menu progress values to stable scene names without changing save data.</summary>
        public static string SceneForProgress(int unlockedState) => unlockedState switch
        {
            <= 0 => FirstMinigameScene,
            1 => SecondMinigameScene,
            _ => ThirdMinigameScene
        };

        /// <summary>Loads a scene by its exact build name.</summary>
        public static void LoadScene(string sceneName)
            => SceneManager.LoadScene(sceneName);

        /// <summary>Loads a scene by its build index.</summary>
        public static void LoadScene(int buildIndex)
            => SceneManager.LoadScene(buildIndex);

        /// <summary>Reloads the currently active scene.</summary>
        public static void ReloadCurrentScene()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // ── Queries ────────────────────────────────────────────────────────────

        /// <summary>Returns the name of the currently active scene.</summary>
        public static string CurrentSceneName
            => SceneManager.GetActiveScene().name;

        /// <summary>Returns the build index of the currently active scene.</summary>
        public static int CurrentSceneIndex
            => SceneManager.GetActiveScene().buildIndex;

        /// <summary>True if the current scene name contains "Minigame1".</summary>
        public static bool IsCurrentSceneFirstLevel()
            => CurrentSceneName.Contains("Minigame1");

        /// <summary>True if the current scene name contains "FinalCutscene".</summary>
        public static bool IsCinematicScene()
            => CurrentSceneName.Contains("FinalCutscene");

        /// <summary>True if the current scene name contains any of the provided substrings.</summary>
        public static bool IsScene(params string[] nameContains)
        {
            foreach (var n in nameContains)
                if (CurrentSceneName.Contains(n)) return true;
            return false;
        }

        /// <summary>True if there is a next scene in the build settings.</summary>
        public static bool HasNextScene()
            => CurrentSceneIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }
}

