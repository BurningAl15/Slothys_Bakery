using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace INART.SlothyBakery.Base
{
    using SceneTransition;
    public class InitialMenuManager : MonoBehaviour
    {
        [Header("Base UI")] [SerializeField] private RectTransform slothImg;
        [SerializeField] private RectTransform titleImg;
        [FormerlySerializedAs("startButton")] [SerializeField] private List<Button> startButtons=new List<Button>();
        [SerializeField] private Button continueButton;
        [SerializeField] private Button soundButton;

        [Header("Properties for Base UI")] [SerializeField]
        private float slothImg_InitialY;

        [SerializeField] private float slothImg_EndY;

        private Vector3 initialPosition_Sloth;
        private Vector3 endPosition_Sloth;

        [SerializeField] private float titleImg_InitialX;
        [SerializeField] private float titleImg_EndX;

        private Vector3 initialPosition_Title;
        private Vector3 endPosition_Title;

        [Range(1f, 2f)] [SerializeField] private float timeScaler;

        private Coroutine currentCoroutine = null;
        [SerializeField] private AnimationCurve _animationCurve;

        [Header("Sound Menu")] [SerializeField]
        private CanvasGroup soundSettings;

        [SerializeField] private AudioSource clickSource;

        private void Awake()
        {
            Initialize();

            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(InitialAnimation(.75f));

            // SaveLoadState.StateButtonInitialize<TrueBakeryData>(startButtons[0], continueButton, SaveLoadState.instance.OnLoad);
        }

        private void Initialize()
        {
            var slothPosition = slothImg.localPosition;
            slothImg.localPosition = new Vector3(slothPosition.x, slothImg_InitialY, slothPosition.z);

            initialPosition_Sloth = slothImg.localPosition;
            endPosition_Sloth = new Vector3(slothPosition.x, slothImg_EndY, slothPosition.z);

            var titlePosition = titleImg.localPosition;
            titleImg.localPosition = new Vector3(titleImg_InitialX, titlePosition.y, titlePosition.z);

            initialPosition_Title = titleImg.localPosition;
            endPosition_Title = new Vector3(titleImg_EndX, titlePosition.y, titlePosition.z);

            for (int i = 0; i < startButtons.Count; i++)
                startButtons[i].GetComponent<RectTransform>().localScale = Vector3.zero;

            soundButton.GetComponent<RectTransform>().localScale = Vector3.zero;

            soundSettings.alpha = 0;
            soundSettings.interactable = false;
            soundSettings.blocksRaycasts = false;
        }

        public void Call_SoundSettings()
        {
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(SoundSettings(.75f));
        }

        public void Call_InitialAnimation()
        {
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(InitialAnimation(.75f, false));
        }

        private IEnumerator InitialAnimation(float _animationDuration, bool isInitial = true)
        {
            var scaledTime = Time.fixedDeltaTime * timeScaler;
            float animationTime = 0;

            if (!isInitial)
            {
                animationTime = _animationDuration;
                while (animationTime > 0)
                {
                    animationTime -= scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _animationDuration);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    soundSettings.alpha = animationCurveValue;
                    yield return null;
                }

                soundSettings.interactable = false;
                soundSettings.blocksRaycasts = false;
            }

            animationTime = 0;

            while (animationTime <= _animationDuration)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                slothImg.localPosition = Vector3.Lerp(initialPosition_Sloth, endPosition_Sloth, animationCurveValue);
                yield return null;
            }

            yield return new WaitForSeconds(.25f);

            animationTime = 0;
            while (animationTime <= _animationDuration)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                titleImg.localPosition = Vector3.Lerp(initialPosition_Title, endPosition_Title, animationCurveValue);
                yield return null;
            }
            // yield return new WaitForSeconds(.25f);

            animationTime = 0;
            while (animationTime <= _animationDuration)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                for (int i = 0; i < startButtons.Count; i++)
                    startButtons[i].GetComponent<RectTransform>().localScale = Vector3.one * animationCurveValue;
                
                yield return null;
            }

            animationTime = 0;
            while (animationTime <= _animationDuration)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                soundButton.GetComponent<RectTransform>().localScale = Vector3.one * animationCurveValue;

                yield return null;
            }

            for (int i = 0; i < startButtons.Count; i++)
                startButtons[i].GetComponent<Animator>().enabled = true;

            currentCoroutine = null;
        }

        private IEnumerator SoundSettings(float _animationDuration)
        {
            var scaledTime = Time.fixedDeltaTime * timeScaler;
            var animationTime = _animationDuration;

            for (int i = 0; i < startButtons.Count; i++)
                startButtons[i].GetComponent<Animator>().enabled = false;

            while (animationTime > 0)
            {
                animationTime -= scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                slothImg.localPosition = Vector3.Lerp(initialPosition_Sloth, endPosition_Sloth, animationCurveValue);
                yield return null;
            }

            yield return new WaitForSeconds(.25f);

            animationTime = _animationDuration;
            while (animationTime > 0)
            {
                animationTime -= scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                titleImg.localPosition = Vector3.Lerp(initialPosition_Title, endPosition_Title, animationCurveValue);
                yield return null;
            }

            animationTime = _animationDuration;
            while (animationTime > 0)
            {
                animationTime -= scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                for (int i = 0; i < startButtons.Count; i++)
                    startButtons[i].GetComponent<RectTransform>().localScale = Vector3.one * animationCurveValue;
                
                soundButton.GetComponent<RectTransform>().localScale = Vector3.one * animationTime;

                yield return null;  
            }

            animationTime = 0;

            while (animationTime <= _animationDuration)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _animationDuration);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                soundSettings.alpha = animationCurveValue;
                yield return null;
            }

            soundSettings.interactable = true;
            soundSettings.blocksRaycasts = true;

            currentCoroutine = null;
        }

        private IEnumerator ToNextScene()
        {
            //Do something or run a shader
            if (clickSource != null)
            {
                clickSource.Play();
                yield return !clickSource.isPlaying;
            }

            yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            currentCoroutine = null;
        }

        private IEnumerator ToNextScene(string _sceneName)
        {
            //Do something or run a shader
            yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
            SceneManager.LoadScene(_sceneName);
            currentCoroutine = null;
        }

        public void CallNextScene()
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene());
        }
    }
}
