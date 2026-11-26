using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Fatcat.Slothy.Bakery.Modes;
using Fatcat.Slothy.Bakery.Shell;
using FatCat.SettingsUI;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.Core;
using INART.SlothyBakery.SceneTransition;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using CanvasAnimatorNs = INART.SlothyBakery.CanvasAnimator;
using Object = UnityEngine.Object;

namespace Fatcat.Slothy.Bakery.Tests
{
    public class BakeryModesAndCheckpointsPlayTests
    {
        private const string MainScreenPath = "Assets/INART GAMES/Scenes/1. Main Screen.unity";
        private const string Mg2Path = "Assets/INART GAMES/Scenes/3. Minigame2.unity";
        private const string PauseStorySnapshot = "{\"unlockedState\":1,\"completions\":2,\"lastPlayedUtc\":\"2026-11-24T00:00:00Z\",\"currentRoundIndex\":3,\"currentSceneName\":\"3. Minigame2\"}";
        private const string ModesStorySnapshot = "{\"unlockedState\":1,\"completions\":1,\"lastPlayedUtc\":\"2026-11-24T00:00:00Z\",\"currentRoundIndex\":2,\"currentSceneName\":\"3. Minigame2\"}";
        private static readonly BindingFlags PrivateInst = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly System.Collections.Generic.List<string> StaticNavigated = new();
        private static bool StaticGateCallbackInvoked;

        private static bool CaptureNav(string scene)
        {
            StaticNavigated.Add(scene);
            return true;
        }

        private static void OnGateVerified() => StaticGateCallbackInvoked = true;

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying) return;
            Time.timeScale = 1f;
            BakerySession.NavigateOverride = null;
            BakerySession.SetStoryMode();
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = false;
            BakeryTestPrefsScope.RestoreProfile();
            ClearBakeryModePrefs();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void ClearBakeryModePrefs()
        {
            foreach (var k in new[]
            {
                "bakery.stat.story.finished", "bakery.stat.afternoon.finished", "bakery.stat.chef.finished",
                "bakery.stat.daily.finished", "bakery.stat.minigame.perfect", "bakery.stat.days.played",
                "bakery.stat.minigame.done.2. Minigame1", "bakery.stat.minigame.done.3. Minigame2",
                "bakery.stat.minigame.done.4. Minigame3",
                "bakery.stat.skill.stars.2. Minigame1", "bakery.stat.skill.stars.3. Minigame2",
                "bakery.stat.skill.stars.4. Minigame3",
                "bakery.practice.mg1.count", "bakery.practice.mg2.count", "bakery.practice.mg3.count",
                "bakery.play_days",
                "bakery.daily.done." + DateTime.Now.Date.ToString("yyyyMMdd"),
                "bakery.daily.done.20261124"
            })
                PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }

        private static IEnumerator WaitForScene(string expectedScene, float timeout = 20f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != expectedScene)
                yield return null;
            Assert.AreEqual(expectedScene, SceneManager.GetActiveScene().name,
                $"Expected active scene '{expectedScene}'.");
        }

        private static IEnumerator WaitUntilPlaying(float timeout = 20f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline &&
                   (GameStates.Instance == null || !GameStates.Instance.IsGamePlaying()))
                yield return null;
            Assert.IsTrue(GameStates.Instance != null && GameStates.Instance.IsGamePlaying(),
                "Expected scene to enter GameStates.States.Play.");
        }

        private static GameObject FindInAllLoaded(string exactName)
        {
            foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tr.name == exactName)
                    return tr.gameObject;
            return null;
        }

        private static void CaptureAtAspect(string dir, string baseName)
        {
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var cam = Camera.main;
            Assert.IsNotNull(cam, "Camera.main must exist before capture.");

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var prevModes = new RenderMode[canvases.Length];
            var prevCameras = new Camera[canvases.Length];
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
                    canvases[i].sortingLayerID = prevLayers[i];
                    canvases[i].sortingOrder = prevOrders[i];
                }
            }
        }

        [UnityTest]
        public IEnumerator StoryCheckpoint_ResumesAtSavedRound_AndLegacyJsonMigratesCleanly()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            ClearBakeryModePrefs();

            // 1. Verify legacy JSON without currentRoundIndex defaults round to 0 safely
            PlayerPrefs.SetString("slothys_bakery_progress", "{\"unlockedState\":1,\"completions\":0,\"lastPlayedUtc\":\"\"}");
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            Assert.AreEqual(1, SaveLoadState.Instance.GetActualState());
            Assert.AreEqual(0, SaveLoadState.Instance.GetRoundCheckpoint(1));

            // 2. Save Story round checkpoint 2 inside chapter 1 (Minigame2)
            SaveLoadState.Instance.SaveRoundCheckpoint(1, SceneUtils.SecondMinigameScene, 2);
            Assert.AreEqual(2, SaveLoadState.Instance.GetRoundCheckpoint(1));

            yield return new ExitPlayMode();

            // Re-enter PlayMode and Continue from Main Screen -> must resume at MG2 index == 2
            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = true;

            yield return null;
            var continueBtn = FindInAllLoaded("ContinueBtn")?.GetComponent<Button>();
            Assert.IsNotNull(continueBtn);
            Assert.IsTrue(continueBtn.gameObject.activeSelf);
            continueBtn.onClick.Invoke();

            yield return WaitForScene("3. Minigame2");
            yield return WaitUntilPlaying();

            var mg2 = Object.FindAnyObjectByType<Minigame2>();
            Assert.IsNotNull(mg2);
            int resumedIdx = (int)typeof(Minigame2).GetField("index", PrivateInst).GetValue(mg2);
            Assert.AreEqual(2, resumedIdx, "MG2 must resume at checkpoint round 2.");

            // Pause -> Restart inside Story must keep round checkpoint 2 (not reset to 0)
            BakeryPauseOverlay.Instance.OpenPause();
            Assert.IsTrue(BakeryPauseOverlay.Instance.IsPaused);
            BakeryPauseOverlay.Instance.Restart();
            yield return WaitForScene("3. Minigame2");
            yield return WaitUntilPlaying();
            mg2 = Object.FindAnyObjectByType<Minigame2>();
            Assert.AreEqual(2, (int)typeof(Minigame2).GetField("index", PrivateInst).GetValue(mg2),
                "Story Restart must resume at saved round checkpoint 2.");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator PauseOverlay_PausesResumesRestartsAndExitsCleanly_WhilePreservingStoryInOffStory()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            ClearBakeryModePrefs();
            PlayerPrefs.SetString("slothys_bakery_progress", PauseStorySnapshot);
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = true;

            yield return null;

            // Launch Practice MG2 from ModesView -> must ignore Story round 3 and start at index 0
            BakerySession.BeginPractice("mg2", SceneUtils.SecondMinigameScene);
            yield return WaitForScene("3. Minigame2");
            yield return WaitUntilPlaying();
            yield return new WaitForSecondsRealtime(0.4f);

            var mg2 = Object.FindAnyObjectByType<Minigame2>();
            Assert.AreEqual(0, (int)typeof(Minigame2).GetField("index", PrivateInst).GetValue(mg2),
                "Practice must start at round 0 without reading Story checkpoint.");
            Assert.IsTrue(BakeryPauseOverlay.Instance.PauseButton.gameObject.activeInHierarchy);

            // Pause -> Verify TimeScale == 0 & modal shown -> Capture
            BakeryPauseOverlay.Instance.OpenPause();
            yield return null;
            Assert.IsTrue(BakeryPauseOverlay.Instance.IsPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.001f);
            Assert.IsFalse(GameStates.Instance.IsGamePlaying());

            string captureDir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");
            CaptureAtAspect(captureDir, "bakery_pause_modal");

            // Resume -> TimeScale restored & Play resumed
            BakeryPauseOverlay.Instance.Resume();
            yield return null;
            Assert.IsFalse(BakeryPauseOverlay.Instance.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.IsTrue(GameStates.Instance.IsGamePlaying());

            // Wrong answer then Abandon via Pause -> must NOT record completion and must keep Story save untouched
            ValueManager._instance.SetValue(Sign.Greater); // 1/5 > 1/3 is wrong
            mg2.CheckValue();
            Assert.AreEqual(1, BakerySession.Mistakes);

            BakeryPauseOverlay.Instance.OpenPause();
            BakeryPauseOverlay.Instance.RequestExitToMenu();
            BakeryPauseOverlay.Instance.ConfirmExitToMenu();
            yield return WaitForScene("1. Main Screen");

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.IsFalse(BakerySession.OffStory);
            Assert.AreEqual(0, BakeryStats.PracticeCount("mg2"), "Abandoning Practice must not count as finished.");
            Assert.AreEqual(PauseStorySnapshot, PlayerPrefs.GetString("slothys_bakery_progress", ""),
                "Practice session and abandonment must leave Story save byte-identical.");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator Modes_DailyAfternoonChef_SummaryAndParentalGate_WorkAndCapture()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            ClearBakeryModePrefs();
            PlayerPrefs.SetString("slothys_bakery_progress", ModesStorySnapshot);
            PlayerPrefs.SetInt(BakeryLanguageGate.ChosenKey, 1);
            PlayerPrefs.SetString("selected-locale", "es");
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(MainScreenPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;

            yield return new WaitForSecondsRealtime(0.8f);
            string captureDir = Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR");

            // 1. Open Modes modal (Afternoon & Chef initially locked if story.finished == 0)
            var modesView = Object.FindAnyObjectByType<BakeryModesView>(FindObjectsInactive.Include);
            Assert.IsNotNull(modesView);
            Assert.IsFalse(BakerySession.AfternoonUnlocked);
            modesView.OpenModes();
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAtAspect(captureDir, "bakery_modes");
            modesView.CloseModes();
            yield return new WaitForSecondsRealtime(0.2f);

            // 2. Verify Daily deterministic seed & run completion without loading external scenes
            var day = new DateTime(2026, 11, 24);
            CollectionAssert.AreEqual(BakerySession.DailyScenes(day), BakerySession.DailyScenes(day));
            StaticNavigated.Clear();
            BakerySession.NavigateOverride = CaptureNav;

            BakerySession.BeginDaily(day);
            Assert.IsTrue(BakerySession.OffStory);
            Assert.AreEqual(BakerySession.DailySeed(day), BakerySession.Seed);
            BakerySession.RecordAnswer(true);
            BakerySession.CompleteCurrentChapter(); // chapter 0 -> chapter 1
            Assert.IsTrue(BakerySession.OffStory);
            BakerySession.RecordAnswer(false);
            BakerySession.RecordAnswer(true);
            BakerySession.CompleteCurrentChapter(); // chapter 1 -> complete
            Assert.IsFalse(BakerySession.OffStory);
            Assert.IsTrue(BakeryStats.IsDailyCompleted(day));
            Assert.IsTrue(BakerySession.ConsumePendingSummary(out var dailySummary));
            Assert.AreEqual(5, dailySummary.totalStars); // 3★ (0 err) + 2★ (1 err)

            // 3. Verify Afternoon unlocks Chef when finished
            BakeryStats.Add("story.finished");
            Assert.IsTrue(BakerySession.AfternoonUnlocked);
            Assert.IsFalse(BakerySession.ChefUnlocked);
            BakerySession.BeginAfternoon();
            BakerySession.RecordAnswer(true);
            BakerySession.CompleteCurrentChapter();
            BakerySession.RecordAnswer(true);
            BakerySession.CompleteCurrentChapter();
            Assert.IsTrue(BakerySession.ChefUnlocked);
            Assert.IsTrue(BakerySession.ConsumePendingSummary(out var aftSummary));
            Assert.IsTrue(aftSummary.newlyUnlockedChef);

            BakerySession.NavigateOverride = null;
            modesView.ShowSummary(aftSummary);
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAtAspect(captureDir, "bakery_run_summary");
            modesView.CloseSummary();
            yield return new WaitForSecondsRealtime(0.2f);

            // 4. Story progress stayed byte-identical across Daily & Afternoon runs
            Assert.AreEqual(ModesStorySnapshot, PlayerPrefs.GetString("slothys_bakery_progress", ""),
                "Daily and Afternoon runs must never touch Story SaveLoadState.");

            // 5. ParentalGate verification (wrong option keeps gate open; correct invokes callback)
            StaticGateCallbackInvoked = false;
            BakeryParentalGate.Ask(OnGateVerified);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(BakeryParentalGate.IsOpen);
            CaptureAtAspect(captureDir, "bakery_parental_gate");

            var gate = Object.FindAnyObjectByType<BakeryParentalGate>(FindObjectsInactive.Include);
            var questionField = (TMPro.TMP_Text)typeof(BakeryParentalGate).GetField("questionLabel", PrivateInst).GetValue(gate);
            Assert.IsFalse(questionField.text.Contains("{0}"), $"ParentalGate question was not formatted: '{questionField.text}'");
            int correctIdx = (int)typeof(BakeryParentalGate).GetField("_correctIndex", PrivateInst).GetValue(gate);
            gate.PickOption((correctIdx + 1) % 3);
            Assert.IsFalse(StaticGateCallbackInvoked);
            Assert.IsTrue(BakeryParentalGate.IsOpen);

            correctIdx = (int)typeof(BakeryParentalGate).GetField("_correctIndex", PrivateInst).GetValue(gate);
            gate.PickOption(correctIdx);
            Assert.IsTrue(StaticGateCallbackInvoked);
            Assert.IsFalse(BakeryParentalGate.IsOpen);

            // 6. Achievements, Progress Dashboard & More Games captures
            modesView.OpenAchievements();
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAtAspect(captureDir, "bakery_achievements");
            modesView.CloseAchievements();
            yield return new WaitForSecondsRealtime(0.2f);

            modesView.OpenProgress();
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAtAspect(captureDir, "bakery_progress");
            modesView.CloseProgress();
            yield return new WaitForSecondsRealtime(0.2f);

            modesView.OpenMoreGames();
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAtAspect(captureDir, "bakery_more_games");
            modesView.CloseMoreGames();
            yield return new WaitForSecondsRealtime(0.2f);

            BakeryDiploma.Open();
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsTrue(BakeryDiploma.IsOpen, "Diploma modal must open via BakeryDiploma.Open().");
            CaptureAtAspect(captureDir, "bakery_diploma");
            BakeryDiploma.Close();
            yield return new WaitForSecondsRealtime(0.2f);

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
            ClearBakeryModePrefs();
        }
    }
}
