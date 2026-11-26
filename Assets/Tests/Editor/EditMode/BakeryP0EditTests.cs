using System.Linq;
using Fatcat.Slothy.Bakery.Shell;
using FatCat.Localization.UI;
using FatCat.SettingsUI;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.SceneTransition;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Tests
{
    public class BakeryP0EditTests
    {
        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BuildSettings_ExcludesDevSceneAndOrdersReleaseFlow()
        {
            var devScene = EditorBuildSettings.scenes
                .FirstOrDefault(s => s.path.Contains("DEV_ModularMinigame"));
            Assert.IsNotNull(devScene, "DEV_ModularMinigame entry should remain disabled, not deleted unexpectedly.");
            Assert.IsFalse(devScene.enabled, "DEV_ModularMinigame must be disabled in EditorBuildSettings.");

            var enabledPaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Assets/FatCatStudio/Splash/FatCatSplash.unity",
                    "Assets/INART GAMES/Scenes/1. Main Screen.unity",
                    "Assets/INART GAMES/Scenes/2. Minigame1.unity",
                    "Assets/INART GAMES/Scenes/3. Minigame2.unity",
                    "Assets/INART GAMES/Scenes/4. Minigame3.unity",
                    "Assets/INART GAMES/Scenes/5. FinalCutscene.unity"
                },
                enabledPaths);
        }

        [Test]
        public void SceneRouting_ResolvesMenuContinueAndExitTargetsWithoutDev()
        {
            Assert.AreEqual(SceneUtils.FirstMinigameScene, UI_Transition_Button.ResolveSceneName("0"));
            Assert.AreEqual(SceneUtils.FirstMinigameScene, UI_Transition_Button.ResolveSceneName("1"));
            Assert.AreEqual(SceneUtils.SecondMinigameScene, UI_Transition_Button.ResolveSceneName("2"));
            Assert.AreEqual(SceneUtils.ThirdMinigameScene, UI_Transition_Button.ResolveSceneName("3"));
            Assert.AreEqual(SceneUtils.FinalScene, UI_Transition_Button.ResolveSceneName("4"));
            Assert.AreEqual(SceneUtils.FinalScene, UI_Transition_Button.ResolveSceneName("FinalCutscene"));
            Assert.AreEqual(SceneUtils.MainMenuScene, UI_Transition_Button.ResolveSceneName("StartScreen"));

            Assert.AreEqual(SceneUtils.FirstMinigameScene, SceneUtils.SceneForProgress(0));
            Assert.AreEqual(SceneUtils.SecondMinigameScene, SceneUtils.SceneForProgress(1));
            Assert.AreEqual(SceneUtils.ThirdMinigameScene, SceneUtils.SceneForProgress(2));
            Assert.AreEqual(SceneUtils.ThirdMinigameScene, SceneUtils.SceneForProgress(99));
        }

        [Test]
        public void MainScreen_HasShellPrefabControlsFontsAndSoundButtonWired()
        {
            var scene = EditorSceneManager.OpenScene("Assets/INART GAMES/Scenes/1. Main Screen.unity", OpenSceneMode.Single);
            var shellRoot = scene.GetRootGameObjects().FirstOrDefault(g =>
                g.name == "BakeryShell" || g.name == "BakeryShellCanvas"
                || g.GetComponentInChildren<SettingsScreenView>(true) != null);
            Assert.IsNotNull(shellRoot, "BakeryShell must be installed in 1. Main Screen.unity.");

            var settingsView = shellRoot.GetComponentInChildren<SettingsScreenView>(true);
            var backend = shellRoot.GetComponentInChildren<BakerySettingsBackend>(true);
            var picker = shellRoot.GetComponentInChildren<LocalePickerController>(true);
            var gate = shellRoot.GetComponent<BakeryLanguageGate>();
            var langLabel = shellRoot.GetComponentInChildren<BakeryLanguageLabel>(true);

            Assert.IsNotNull(settingsView);
            Assert.IsNotNull(backend);
            Assert.IsNotNull(picker);
            Assert.IsNotNull(gate);
            Assert.IsNotNull(langLabel);

            var soundBtn = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Button>(true))
                .FirstOrDefault(b => b.name == "SoundBtn");
            Assert.IsNotNull(soundBtn, "SoundBtn must exist in 1. Main Screen.unity.");
            Assert.GreaterOrEqual(soundBtn.onClick.GetPersistentEventCount(), 1);
            Assert.AreEqual(settingsView, soundBtn.onClick.GetPersistentTarget(0));
            Assert.AreEqual(nameof(SettingsScreenView.Open), soundBtn.onClick.GetPersistentMethodName(0));

            var vso = new SerializedObject(settingsView);
            Assert.AreEqual(2, vso.FindProperty("volumes").arraySize);
            Assert.AreEqual(2, vso.FindProperty("switches").arraySize);
            Assert.AreEqual(6, vso.FindProperty("shortcuts").arraySize);
            Assert.IsNotNull(vso.FindProperty("panel").objectReferenceValue);

            var pso = new SerializedObject(picker);
            Assert.AreEqual(2, pso.FindProperty("toggles").arraySize);
            Assert.AreEqual("ES", pso.FindProperty("languageKeys").GetArrayElementAtIndex(0).stringValue);
            Assert.AreEqual("EN", pso.FindProperty("languageKeys").GetArrayElementAtIndex(1).stringValue);

            foreach (var tmp in shellRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.IsNotNull(tmp.font, $"TMP font missing on {tmp.name}");
                foreach (char ch in new[] { 'á', 'é', 'í', 'ó', 'ú', 'ñ', '¿', '×' })
                    Assert.IsTrue(tmp.font.HasCharacter(ch, true),
                        $"Shell font '{tmp.font.name}' on '{tmp.name}' is missing Spanish character '{ch}'.");
            }
        }
    }
}
