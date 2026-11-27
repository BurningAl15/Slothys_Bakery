using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fatcat.Slothy.Bakery.Shell;
using FatCat.Localization.UI;
using FatCat.SettingsUI;
using INART.SlothyBakery.Base;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fatcat.Slothy.Bakery.Tests
{
    public class BakeryP0PlayTests
    {
        private static readonly string MainScreenPath = "Assets/INART GAMES/Scenes/1. Main Screen.unity";
        private static readonly string FinalCutscenePath = "Assets/INART GAMES/Scenes/5. FinalCutscene.unity";

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying) return;
            BakeryTestPrefsScope.RestoreProfile();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private static IEnumerator WaitForMenuButtonsSettled(float timeout = 8f, params string[] buttonNames)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            bool settled = false;
            while (Time.realtimeSinceStartup < deadline && !settled)
            {
                yield return null;
                var snd = FindInActiveScene("SoundBtn");
                if (snd == null || snd.transform.localScale.x < 0.95f)
                    continue;

                settled = true;
                foreach (var name in buttonNames)
                {
                    var go = FindInActiveScene(name);
                    var img = go != null ? go.GetComponent<Image>() : null;
                    if (go == null || !go.activeInHierarchy || go.transform.localScale.x < 0.95f || (img != null && img.color.a < 0.95f))
                    {
                        settled = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        private static IEnumerator WaitForScene(string expectedScene, float timeout = 20f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != expectedScene)
                yield return null;
            Assert.AreEqual(expectedScene, SceneManager.GetActiveScene().name,
                $"Expected active scene '{expectedScene}' before timeout.");
        }

        private static GameObject FindInActiveScene(string exactName)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                    if (tr.name == exactName)
                        return tr.gameObject;
            }
            return null;
        }

        private static void CaptureAtAspect(string dir, string baseName)
        {
            if (string.IsNullOrEmpty(dir))
                dir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);

            var cam = Camera.main;
            Assert.IsNotNull(cam, "Camera.main must exist before capturing screenshots.");

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var prevModes = new RenderMode[canvases.Length];
            var prevCameras = new Camera[canvases.Length];
            var prevPlanes = new float[canvases.Length];
            var prevLayers = new int[canvases.Length];
            var prevOrders = new int[canvases.Length];

            int topLayer = 0;
            int topOrder = 100;
            foreach (var c in canvases)
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay && c.sortingOrder >= topOrder)
                {
                    topOrder = c.sortingOrder;
                    topLayer = c.sortingLayerID;
                }
            }

            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                prevModes[i] = c.renderMode;
                prevCameras[i] = c.worldCamera;
                prevPlanes[i] = c.planeDistance;
                prevLayers[i] = c.sortingLayerID;
                prevOrders[i] = c.sortingOrder;
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1f;
                    c.sortingLayerID = topLayer;
                    c.sortingOrder = topOrder + 100 + prevOrders[i];
                }
            }

            try
            {
                foreach (var (w, h, tag) in new[] { (1920, 1080, "16x9"), (2340, 1080, "20x9") })
                {
                    var rt = new RenderTexture(w, h, 24);
                    cam.targetTexture = rt;
                    foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
                    {
                        scaler.enabled = false;
                        scaler.enabled = true;
                    }
                    for (int pass = 0; pass < 3; pass++)
                    {
                        Canvas.ForceUpdateCanvases();
                        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    }

                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    cam.targetTexture = null;
                    Object.DestroyImmediate(rt);

                    File.WriteAllBytes(Path.Combine(dir, $"{baseName}_{tag}.png"), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] == null) continue;
                    canvases[i].renderMode = prevModes[i];
                    canvases[i].worldCamera = prevCameras[i];
                    canvases[i].planeDistance = prevPlanes[i];
                    canvases[i].sortingLayerID = prevLayers[i];
                    canvases[i].sortingOrder = prevOrders[i];
                }
            }
        }

        [UnityTest]
        public IEnumerator MainMenu_NewGameStartsAtMinigame1_AndHidesContinueWithoutSave()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.SetString("selected-locale", "es");
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            yield return WaitForMenuButtonsSettled(8f, "StartBtn");

            var startBtnGo = FindInActiveScene("StartBtn");
            var continueBtnGo = FindInActiveScene("ContinueBtn");
            Assert.IsNotNull(startBtnGo, "StartBtn must exist.");
            Assert.IsNotNull(continueBtnGo, "ContinueBtn must exist.");
            Assert.IsTrue(startBtnGo.activeSelf, "StartBtn must be visible.");
            Assert.Greater(startBtnGo.transform.localScale.x, 0.9f, "StartBtn must finish intro scale.");
            Assert.Greater(startBtnGo.GetComponent<Image>().color.a, 0.9f, "StartBtn must finish fade-in alpha.");
            Assert.IsFalse(continueBtnGo.activeSelf, "ContinueBtn must stay hidden when no save exists.");

            string captureDir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");
            CaptureAtAspect(captureDir, "bakery_home_nosave");

            var startBtn = startBtnGo.GetComponent<Button>();
            startBtn.onClick.Invoke();
            yield return WaitForScene("2. Minigame1");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator MainMenu_ContinueRoutesToSavedMinigameWithoutOverwritingProgress()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.SetString("selected-locale", "es");
            PlayerPrefs.SetString("slothys_bakery_progress", "{\"unlockedState\":1,\"completions\":0,\"lastPlayedUtc\":\"\"}");
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            yield return WaitForMenuButtonsSettled(8f, "StartBtn", "ContinueBtn");

            var continueBtnGo = FindInActiveScene("ContinueBtn");
            Assert.IsNotNull(continueBtnGo);
            Assert.IsTrue(continueBtnGo.activeSelf, "ContinueBtn must be visible when progress exists.");
            Assert.Greater(continueBtnGo.transform.localScale.x, 0.9f, "ContinueBtn must finish intro scale.");
            Assert.Greater(continueBtnGo.GetComponent<Image>().color.a, 0.9f, "ContinueBtn must finish fade-in alpha.");
            Assert.IsNotNull(SaveLoadState.Instance);
            Assert.AreEqual(1, SaveLoadState.Instance.GetActualState());

            string captureDir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");
            CaptureAtAspect(captureDir, "bakery_home_continue");

            continueBtnGo.GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("3. Minigame2");
            Assert.AreEqual(1, SaveLoadState.Instance.GetActualState(),
                "Continuing to Minigame2 must preserve unlockedState == 1.");

            yield return new ExitPlayMode();

            // Verify unlockedState == 2 -> Minigame3
            BakeryTestPrefsScope.ClearProfileKeys();
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.SetString("slothys_bakery_progress", "{\"unlockedState\":2,\"completions\":0,\"lastPlayedUtc\":\"\"}");
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            yield return WaitSeconds(2.2f);

            continueBtnGo = FindInActiveScene("ContinueBtn");
            Assert.IsTrue(continueBtnGo.activeSelf);
            continueBtnGo.GetComponent<Button>().onClick.Invoke();
            yield return WaitForScene("4. Minigame3");
            Assert.AreEqual(2, SaveLoadState.Instance.GetActualState(),
                "Continuing to Minigame3 must preserve unlockedState == 2.");

            yield return new ExitPlayMode();

            // Verify FinalCutscene renders cleanly without Missing/UnassignedReferenceException and returns to 1. Main Screen
            EditorSceneManager.OpenScene(FinalCutscenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = false;
            yield return WaitSeconds(2.8f);

            var finalCutscene = Object.FindAnyObjectByType<FinalCutscene>(FindObjectsInactive.Include);
            Assert.IsNotNull(finalCutscene, "FinalCutscene component must exist in 5. FinalCutscene.unity.");
            var diplomaBtn = GameObject.Find("DiplomaButton");
            Assert.IsNotNull(diplomaBtn, "DiplomaButton must be installed in 5. FinalCutscene.unity.");
            Assert.IsTrue(diplomaBtn.activeInHierarchy, "DiplomaButton must be visible in 5. FinalCutscene.unity.");

            CaptureAtAspect(captureDir, "bakery_final_cutscene");

            var transitionBtn = Object.FindObjectsByType<UI_Transition_Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "ResetButton" && b.gameObject.activeInHierarchy)
                ?? Object.FindObjectsByType<UI_Transition_Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.gameObject.activeInHierarchy);
            Assert.IsNotNull(transitionBtn, "Active UI_Transition_Button must exist in 5. FinalCutscene.unity.");
            Debug.Log($"[BakeryP0Test] FinalCutscene btn={transitionBtn.name} active={transitionBtn.gameObject.activeInHierarchy} fade={INART.SlothyBakery.SceneTransition.SceneTransitionController.Instance}");
            transitionBtn.BtnToIntroScene();
            yield return WaitForScene("1. Main Screen");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator Shell_LanguagePickerAndSettingsPersistIsolatedPreferencesAndCapture()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            yield return LocalizationSettings.InitializationOperation;
            yield return WaitSeconds(0.8f);

            string captureDir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");

            var picker = Object.FindAnyObjectByType<LocalePickerController>(FindObjectsInactive.Include);
            var gate = Object.FindAnyObjectByType<BakeryLanguageGate>(FindObjectsInactive.Include);
            Assert.IsNotNull(picker);
            Assert.IsNotNull(gate);
            Assert.IsTrue(picker.gameObject.activeSelf, "LanguagePicker should auto-open on first launch.");

            CaptureAtAspect(captureDir, "bakery_language_picker");

            var toggles = picker.GetComponentsInChildren<Toggle>(true);
            Assert.AreEqual(6, toggles.Length);
            toggles[1].isOn = true; // Select English
            yield return null;
            Assert.IsTrue(LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase),
                $"Expected English locale, got '{LocalizationSettings.SelectedLocale.Identifier.Code}'.");

            toggles[0].isOn = true; // Switch back to Spanish before confirming
            yield return null;
            Assert.IsTrue(LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase),
                $"Expected Spanish locale, got '{LocalizationSettings.SelectedLocale.Identifier.Code}'.");

            var continuePickerBtn = picker.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "ContinueButton");
            Assert.IsNotNull(continuePickerBtn);
            continuePickerBtn.onClick.Invoke();
            yield return null;

            Assert.IsFalse(picker.gameObject.activeSelf);
            Assert.AreEqual(1, PlayerPrefs.GetInt(BakeryLanguageGate.ChosenKey, 0));

            yield return WaitSeconds(1.8f);

            var soundBtn = FindInActiveScene("SoundBtn")?.GetComponent<Button>();
            Assert.IsNotNull(soundBtn);
            soundBtn.onClick.Invoke();
            yield return WaitSeconds(0.5f);

            var settingsView = Object.FindAnyObjectByType<SettingsScreenView>(FindObjectsInactive.Include);
            Assert.IsNotNull(settingsView);
            Assert.IsTrue(settingsView.IsOpen, "SoundBtn click must open SettingsScreenView.");

            CaptureAtAspect(captureDir, "bakery_settings_es");

            // Test volume + switch persistence via UI controls
            var sliders = settingsView.GetComponentsInChildren<Slider>(true);
            Assert.AreEqual(2, sliders.Length);
            sliders[0].value = 0.25f;
            sliders[1].value = 0.55f;
            var switches = settingsView.GetComponentsInChildren<Toggle>(true);
            Assert.AreEqual(2, switches.Length);
            switches[0].isOn = false;
            switches[1].isOn = false;
            yield return null;

            Assert.AreEqual(0.25f, PlayerPrefs.GetFloat("cfg_audio_music", -1f), 0.01f);
            Assert.AreEqual(0.55f, PlayerPrefs.GetFloat("cfg_audio_sfx", -1f), 0.01f);
            Assert.AreEqual(0, PlayerPrefs.GetInt("cfg_gameplay_vib", -1));
            Assert.AreEqual(0, PlayerPrefs.GetInt("cfg_notif_enabled", -1));

            // Switch to English via Language shortcut and verify UI localization updates cleanly
            var langShortcut = settingsView.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "LanguageRow");
            Assert.IsNotNull(langShortcut);
            langShortcut.onClick.Invoke();
            yield return WaitSeconds(0.2f);
            Assert.IsTrue(picker.gameObject.activeSelf);

            toggles[1].isOn = true; // English
            yield return null;
            continuePickerBtn.onClick.Invoke();
            yield return WaitSeconds(0.3f);

            CaptureAtAspect(captureDir, "bakery_settings_en");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }
    }
}
