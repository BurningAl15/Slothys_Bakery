using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using INART.SlothyBakery.SceneTransition;

namespace INART.SlothyBakery.Base
{
    using SceneTransition;

    public class UI_Transition_Button : MonoBehaviour
    {
        Coroutine currentCoroutine = null;

        void Awake()
        {
            // GetComponent<Button>().interactable = false;
        }

        private IEnumerator ToNextScene(string _sceneName)
        {
            var fade = SceneTransitionController.Instance;
            if (fade != null && fade.gameObject.activeInHierarchy)
                yield return StartCoroutine(fade.FadeEffect_OUT());
            var destination = ResolveSceneName(_sceneName);
            if (string.IsNullOrEmpty(destination))
            {
                Debug.LogError($"[Bakery] Unknown menu scene route '{_sceneName}'.", this);
                currentCoroutine = null;
                yield break;
            }

            SceneUtils.LoadScene(destination);
            currentCoroutine = null;
        }

        public static string ResolveSceneName(string requested)
        {
            switch (requested)
            {
                case "0": return SceneUtils.FirstMinigameScene;
                case "1": return SceneUtils.FirstMinigameScene;
                case "2": return SceneUtils.SecondMinigameScene;
                case "3": return SceneUtils.ThirdMinigameScene;
                case "4":
                case "FinalCutscene": return SceneUtils.FinalScene;
                case "StartScreen":
                case "MainScreen": return SceneUtils.MainMenuScene;
                default: return requested;
            }
        }

        public static string ResolveContinueSceneName()
            => SceneUtils.SceneForProgress(SaveLoadState.Instance != null
                ? SaveLoadState.Instance.GetActualState() : 0);

        public void ToScene()
        {
            Fatcat.Slothy.Bakery.Modes.BakerySession.SetStoryMode();
            SaveLoadState.Instance?.ResetForNewStoryGame();
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("0"));
        }

        public void ToScene(string _sceneName)
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene(_sceneName));
        }

        public void BtnIntroToScene()
        {
            Fatcat.Slothy.Bakery.Modes.BakerySession.SetStoryMode();
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(ToNextScene(ResolveContinueSceneName()));
        }

        public void BtnToIntroScene()
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("StartScreen"));
        }
    }
}
