using System;
using System.Collections;
using System.IO;
using System.Reflection;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using CanvasAnimatorNs = INART.SlothyBakery.CanvasAnimator;
using Object = UnityEngine.Object;

namespace Fatcat.Slothy.Bakery.Tests
{
    public class BakeryMinigamePlayTests
    {
        private const string Mg1Path = "Assets/INART GAMES/Scenes/2. Minigame1.unity";
        private const string Mg2Path = "Assets/INART GAMES/Scenes/3. Minigame2.unity";
        private const string Mg3Path = "Assets/INART GAMES/Scenes/4. Minigame3.unity";

        private static readonly BindingFlags PrivateInst = BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying) return;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = false;
            BakeryTestPrefsScope.RestoreProfile();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static IEnumerator WaitUntilPlaying(float timeout = 20f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline &&
                   (GameStates.Instance == null || !GameStates.Instance.IsGamePlaying()))
                yield return null;

            Assert.IsNotNull(GameStates.Instance, "GameStates.Instance must exist in minigame scene.");
            Assert.IsTrue(GameStates.Instance.IsGamePlaying(), "Scene must enter GameStates.States.Play.");
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
        public IEnumerator Minigame1_ContentValid_WrongAndRightSteps_AdvanceRoundWithoutDoubleAccept()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            EditorSceneManager.OpenScene(Mg1Path, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = true;

            yield return WaitUntilPlaying();
            yield return new WaitForSecondsRealtime(0.4f);

            var mg = Object.FindAnyObjectByType<Minigame1_B>();
            Assert.IsNotNull(mg);
            Assert.IsTrue((bool)typeof(Minigame1_B).GetField("contentReady", PrivateInst).GetValue(mg),
                "MG1 authored content must pass ValidateContent().");

            var logic = (Minigame1_Logic)typeof(Minigame1_B).GetField("_logic", PrivateInst).GetValue(mg);
            var rounds = (IList)typeof(Minigame1_B).GetField("compareFractions", PrivateInst).GetValue(mg);
            Assert.IsNotNull(logic);
            Assert.Greater(rounds.Count, 1);

            CaptureAtAspect(Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR"), "bakery_mg1_round0");

            int d1 = (int)typeof(Minigame1_Logic).GetField("denominator1", PrivateInst).GetValue(logic);
            int d2 = (int)typeof(Minigame1_Logic).GetField("denominator2", PrivateInst).GetValue(logic);
            bool sameDenom = d1 == d2;
            var expectedSign = (int)(Sign)rounds[0].GetType().GetField("_value").GetValue(rounds[0]);

            // 1. Wrong denominator check & correct denominator step
            if (sameDenom)
            {
                logic.TurnOn_IfDenominatorsEquals_InputFieldMenu();
                Assert.IsFalse(logic.IsCorrect);
                logic.CheckDenominators();
            }
            else
            {
                logic.CheckDenominators();
                Assert.IsFalse(logic.IsCorrect);
                logic.TurnOn_IfDenominatorsEquals_InputFieldMenu();
                var input = (TMP_InputField)typeof(Minigame1_Logic).GetField("inputField", PrivateInst).GetValue(logic);
                // Wrong / empty multiplier rejected cleanly
                input.text = "";
                logic.CheckInputValue();
                Assert.AreEqual("", input.text);
                // Compute LCM multiplier for denominator1
                int mult = 1;
                while ((d1 * mult) % d2 != 0 && mult <= 20) mult++;
                input.text = mult.ToString();
                logic.CheckInputValue();
            }

            // 3. Wrong sign keeps IsCorrect false; right sign accepts once and blocks double call
            logic.CheckSignInput((expectedSign + 1) % 3);
            Assert.IsFalse(logic.IsCorrect);
            logic.CheckSignInput(expectedSign);
            Assert.IsTrue(logic.IsCorrect);
            logic.CheckSignInput(expectedSign);

            // Wait for Minigame1_B to advance to round index 1
            float deadline = Time.realtimeSinceStartup + 15f;
            var idxField = typeof(Minigame1_B).GetField("minigame1_Index", PrivateInst);
            while (Time.realtimeSinceStartup < deadline && (int)idxField.GetValue(mg) == 0)
                yield return null;
            Assert.AreEqual(1, (int)idxField.GetValue(mg), "MG1 should advance to round 1 after right sign.");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator Minigame2_ContentValid_EmptyAndWrongSignRejected_CorrectAdvancesOnce()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            EditorSceneManager.OpenScene(Mg2Path, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = true;

            yield return WaitUntilPlaying();
            yield return new WaitForSecondsRealtime(0.4f);

            var mg = Object.FindAnyObjectByType<Minigame2>();
            Assert.IsNotNull(mg);
            Assert.IsTrue((bool)typeof(Minigame2).GetField("contentReady", PrivateInst).GetValue(mg),
                "MG2 authored content must pass ValidateContent().");

            var idxField = typeof(Minigame2).GetField("index", PrivateInst);
            var acceptedField = typeof(Minigame2).GetField("answerAccepted", PrivateInst);
            var rounds = (IList)typeof(Minigame2).GetField("rounds", PrivateInst).GetValue(mg);
            var btn = (Button)typeof(Minigame2).GetField("button", PrivateInst).GetValue(mg);
            Assert.Greater(rounds.Count, 1);

            float waitBtnDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < waitBtnDeadline && !btn.interactable)
                yield return null;
            Assert.IsTrue(btn.interactable, "MG2 submit button should become interactable in round 0.");

            CaptureAtAspect(Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR"), "bakery_mg2_round0");

            // 1. Empty selection does not advance
            ValueManager._instance.ResetValue();
            mg.CheckValue();
            Assert.IsFalse((bool)acceptedField.GetValue(mg));
            Assert.AreEqual(0, (int)idxField.GetValue(mg));

            // 2. Wrong answer rejected
            int correct = (int)rounds[0].GetType().GetField("correctAnswer").GetValue(rounds[0]);
            Sign ToSign(int code) => code == 0 ? Sign.Less : code == 1 ? Sign.Equals : Sign.Greater;
            ValueManager._instance.SetValue(ToSign((correct + 1) % 3));
            mg.CheckValue();
            Assert.IsFalse((bool)acceptedField.GetValue(mg));

            // 3. Correct answer accepted once, double-click ignored, transitions to index 1
            ValueManager._instance.SetValue(ToSign(correct));
            mg.CheckValue();
            Assert.IsTrue((bool)acceptedField.GetValue(mg));
            mg.CheckValue();

            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && (int)idxField.GetValue(mg) == 0)
                yield return null;
            Assert.AreEqual(1, (int)idxField.GetValue(mg), "MG2 must advance cleanly to round 1.");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }

        [UnityTest]
        public IEnumerator Minigame3_ContentValid_InvalidZeroCloseRejected_EquivalentAdvancesOnce()
        {
            BakeryTestPrefsScope.BeginIsolatedProfile();
            EditorSceneManager.OpenScene(Mg3Path, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = true;
            CanvasAnimatorNs.CanvasAnimator.SkipForTests = true;

            yield return WaitUntilPlaying();
            yield return new WaitForSecondsRealtime(0.4f);

            var mg = Object.FindAnyObjectByType<Minigame3_B>();
            Assert.IsNotNull(mg);
            Assert.IsTrue((bool)typeof(Minigame3_B).GetField("contentReady", PrivateInst).GetValue(mg),
                "MG3 authored content must pass ValidateContent().");

            var input = (TMP_InputField)typeof(Minigame3_B).GetField("inputField", PrivateInst).GetValue(mg);
            var idxField = typeof(Minigame3_B).GetField("index", PrivateInst);
            var acceptedField = typeof(Minigame3_B).GetField("answerAccepted", PrivateInst);

            float waitInputDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < waitInputDeadline && !input.interactable)
                yield return null;
            Assert.IsTrue(input.interactable, "MG3 inputField must become interactable in round 0.");

            CaptureAtAspect(Environment.GetEnvironmentVariable("FATCAT_CAPTURE_DIR"), "bakery_mg3_round0");

            // Invalid / zero / close wrong inputs all clear & fail cleanly
            foreach (var bad in new[] { "", "abc", "1/2/3", "1/0", "49/99" })
            {
                input.text = bad;
                mg.Option();
                Assert.IsFalse((bool)acceptedField.GetValue(mg), $"Expected '{bad}' to be rejected.");
                Assert.AreEqual("", input.text, $"Rejected input '{bad}' should clear.");
            }

            // Round 0 expects == 0.5 -> equivalent fraction "2/4" succeeds once
            input.text = "2/4";
            mg.Option();
            Assert.IsTrue((bool)acceptedField.GetValue(mg), "Equivalent fraction 2/4 should succeed.");
            Assert.IsFalse(input.interactable, "Input must lock during round transition.");
            mg.Option();

            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && (int)idxField.GetValue(mg) == 0)
                yield return null;
            Assert.AreEqual(1, (int)idxField.GetValue(mg), "MG3 must advance cleanly to round 1.");

            yield return new ExitPlayMode();
            BakeryTestPrefsScope.RestoreProfile();
        }
    }
}
