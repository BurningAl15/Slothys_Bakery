using UnityEngine;
using UnityEditor;
using System.IO;
using INART.SlothyBakery.Base;

namespace INART.SlothyBakery.Editor
{
    /// <summary>
    /// Editor utilities for Slothy's Bakery development.
    /// Provides menu items for common development tasks.
    /// </summary>
    public static class EditorUtilities
    {
        #region PlayerPrefs Management
        [MenuItem("INART/Development/Clear PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog(
                "Clear PlayerPrefs",
                "Are you sure you want to delete all PlayerPrefs data?",
                "Yes", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[EditorUtilities] PlayerPrefs cleared successfully!");
            }
        }

        [MenuItem("INART/Development/Show PlayerPrefs Location")]
        public static void ShowPlayerPrefsLocation()
        {
            string path = "";
            
            #if UNITY_EDITOR_WIN
            path = @"HKEY_CURRENT_USER\Software\Unity\UnityEditor\" + Application.companyName + @"\" + Application.productName;
            #elif UNITY_EDITOR_OSX
            path = "~/Library/Preferences/com." + Application.companyName + "." + Application.productName + ".plist";
            #endif
            
            Debug.Log($"[EditorUtilities] PlayerPrefs location: {path}");
            EditorUtility.DisplayDialog("PlayerPrefs Location", path, "OK");
        }
        #endregion

        #region Screenshot Tools
        [MenuItem("INART/Development/Take Screenshot _F12")]
        public static void TakeScreenshot()
        {
            string directory = "Screenshots";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filename = $"screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string path = Path.Combine(directory, filename);
            
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[EditorUtilities] Screenshot saved: {path}");
        }

        [MenuItem("INART/Development/Take High-Res Screenshot (2x)")]
        public static void TakeHighResScreenshot()
        {
            string directory = "Screenshots";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filename = $"screenshot_2x_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string path = Path.Combine(directory, filename);
            
            ScreenCapture.CaptureScreenshot(path, 2);
            Debug.Log($"[EditorUtilities] High-res screenshot saved: {path}");
        }
        #endregion

        #region Scene Management
        [MenuItem("INART/Scenes/Open Initial Menu")]
        public static void OpenInitialMenu()
        {
            OpenScene(SceneNames.INITIAL_MENU);
        }

        [MenuItem("INART/Scenes/Open Minigame 1 _F5")]
        public static void OpenMinigame1()
        {
            OpenScene(SceneNames.MINIGAME_1);
        }

        [MenuItem("INART/Scenes/Open Minigame 2 _F6")]
        public static void OpenMinigame2()
        {
            OpenScene(SceneNames.MINIGAME_2);
        }

        [MenuItem("INART/Scenes/Open Minigame 3 _F7")]
        public static void OpenMinigame3()
        {
            OpenScene(SceneNames.MINIGAME_3);
        }

        [MenuItem("INART/Scenes/Open Final Cutscene _F8")]
        public static void OpenFinalCutscene()
        {
            OpenScene(SceneNames.FINAL_CUTSCENE);
        }

        [MenuItem("INART/Scenes/▶ Load Next Scene in Play Mode %]")]
        public static void LoadNextScenePlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EditorUtilities] 'Load Next Scene' only works in Play Mode.");
                return;
            }
            int next = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
            if (next < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                UnityEngine.SceneManagement.SceneManager.LoadScene(next);
            else
                Debug.LogWarning("[EditorUtilities] Already on the last scene.");
        }

        [MenuItem("INART/Scenes/◀ Load Previous Scene in Play Mode %[")]
        public static void LoadPrevScenePlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EditorUtilities] 'Load Previous Scene' only works in Play Mode.");
                return;
            }
            int prev = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex - 1;
            if (prev >= 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(prev);
            else
                Debug.LogWarning("[EditorUtilities] Already on the first scene.");
        }

        [MenuItem("INART/Scenes/↺ Reload Current Scene in Play Mode %r")]
        public static void ReloadCurrentScenePlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EditorUtilities] 'Reload Scene' only works in Play Mode.");
                return;
            }
            int current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            UnityEngine.SceneManagement.SceneManager.LoadScene(current);
        }

        private static void OpenScene(string sceneName)
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                }
                else
                {
                    Debug.LogWarning($"[EditorUtilities] Scene '{sceneName}' not found!");
                }
            }
        }
        #endregion

        #region Build Tools
        [MenuItem("INART/Build/Show Build Settings")]
        public static void ShowBuildSettings()
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        [MenuItem("INART/Build/Clean Build Folder")]
        public static void CleanBuildFolder()
        {
            string buildPath = "Builds";
            if (Directory.Exists(buildPath))
            {
                if (EditorUtility.DisplayDialog(
                    "Clean Build Folder",
                    $"Delete all contents of '{buildPath}' folder?",
                    "Yes", "Cancel"))
                {
                    Directory.Delete(buildPath, true);
                    Directory.CreateDirectory(buildPath);
                    AssetDatabase.Refresh();
                    Debug.Log("[EditorUtilities] Build folder cleaned!");
                }
            }
            else
            {
                Debug.LogWarning($"[EditorUtilities] Build folder '{buildPath}' does not exist.");
            }
        }
        #endregion

        #region Asset Management
        [MenuItem("INART/Assets/Find Missing Scripts")]
        public static void FindMissingScripts()
        {
            int missingCount = 0;
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            
            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                foreach (Component c in components)
                {
                    if (c == null)
                    {
                        missingCount++;
                        Debug.LogWarning($"Missing script on: {go.name}", go);
                    }
                }
            }

            if (missingCount == 0)
            {
                Debug.Log("[EditorUtilities] No missing scripts found!");
            }
            else
            {
                Debug.LogWarning($"[EditorUtilities] Found {missingCount} missing scripts!");
            }
            
            EditorUtility.DisplayDialog(
                "Missing Scripts Check",
                $"Found {missingCount} missing script(s). Check console for details.",
                "OK");
        }

        [MenuItem("INART/Assets/Refresh AssetDatabase")]
        public static void RefreshAssetDatabase()
        {
            AssetDatabase.Refresh();
            Debug.Log("[EditorUtilities] AssetDatabase refreshed!");
        }
        #endregion

        #region Debug Tools
        [MenuItem("INART/Debug/Log System Info")]
        public static void LogSystemInfo()
        {
            Debug.Log("=== SYSTEM INFO ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Platform: {Application.platform}");
            Debug.Log($"Device Model: {SystemInfo.deviceModel}");
            Debug.Log($"OS: {SystemInfo.operatingSystem}");
            Debug.Log($"Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            Debug.Log($"Memory: {SystemInfo.systemMemorySize} MB");
            Debug.Log($"Graphics: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            Debug.Log("==================");
        }

        [MenuItem("INART/Debug/Clear Console _F1")]
        public static void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
        #endregion
    }
}
