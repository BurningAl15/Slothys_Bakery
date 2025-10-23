using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// using LoLSDK;

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
            //Do something or run a shader
            yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
            if (_sceneName == "0")
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            else if (_sceneName == "1" || _sceneName == "2" || _sceneName == "3")
                SceneManager.LoadScene("Minigame" + _sceneName);
            else
                SceneManager.LoadScene(_sceneName);
            currentCoroutine = null;
        }

        public void ToScene()
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("0"));
        }

        public void ToScene(string _sceneName)
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene(_sceneName));
        }

        public void BtnIntroToScene()
        {
            // print(SaveLoadState.instance.GetActualState());
            switch (SaveLoadState.instance.GetActualState())
            {
                case 0:
                    if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("1"));
                    break;
                case 1:
                    if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("2"));
                    break;
                case 2:
                    if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("3"));
                    break;
                default:
                    break;
            }
        }

        public void BtnToIntroScene()
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene("StartScreen"));
        }
    }
}
