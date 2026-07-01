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
        // ── Navigation ─────────────────────────────────────────────────────────

        /// <summary>Loads scene 0 (usually the splash / main menu).</summary>
        public static void ToInitialScene()
            => SceneManager.LoadScene(0);

        /// <summary>Loads the next scene by build index.</summary>
        public static void ToNextScene()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

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

