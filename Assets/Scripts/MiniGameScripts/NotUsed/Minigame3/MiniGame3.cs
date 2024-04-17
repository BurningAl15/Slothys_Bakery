using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;

    [Serializable]
    public class Minigame3_QuestionUI
    {
        public Image image;
        public Button button;
        public TextMeshProUGUI text;
    }

    [Serializable]
    public class QuestionOptions
    {
        public Transform bread;
        public string[] options = new string[4];
        public int correctAnswer;
        public int currentAnswer;
    }

    public class MiniGame3 : MiniGameParent
    {
        [Header("Canvas Groups")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField] private CanvasGroup buttonsGroup;

        [Header("Needed Properties")]
        [SerializeField]
        private List<Minigame3_QuestionUI> buttons = new List<Minigame3_QuestionUI>();

        [SerializeField] private List<QuestionOptions> optionsGroup = new List<QuestionOptions>();

        [Header("Waypoint Positions")]
        [SerializeField]
        private Transform startPosition;

        [SerializeField] private Transform midPosition;
        [SerializeField] private Transform endPosition;

        [Header("Animation Properties")]
        [Range(1f, 2f)]
        [SerializeField]
        private float timeScaler;

        [SerializeField] private AnimationCurve _animationCurve;

        [Header("Button Colors")]
        [SerializeField]
        private Color goodColor;

        [SerializeField] private Color wrongColor;
        [SerializeField] private Color neutralColor;

        //Others
        private Coroutine currentCoroutine = null;
        private int index = 0;
        private bool next = false;

        [SerializeField] private List<string> paragraphs = new List<string>();
        [SerializeField] private TextMeshProUGUI questionText;

        private void Update()
        {
            if (GameStates._instance.GetGameState() == GameStates.GameState.PLAY)
                if (next)
                    next = false;
        }


        public void Option(int _answer)
        {
            if (_answer == optionsGroup[index].correctAnswer)
            {
                buttons[_answer].image.color = goodColor;
                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
                // #if UNITY_WEBGL
                //                 //LOLSDK.Instance.SpeakText("Minigame3_Correct");
                // #endif
            }
            else
            {
                buttons[_answer].image.color = wrongColor;
                buttons[_answer].button.interactable = false;
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                // #if UNITY_WEBGL
                //                 //LOLSDK.Instance.SpeakText("Minigame3_Incorrect");
                // #endif
            }
        }

        public override void Init()
        {
            CanvasGroupAlpha(0);
            CanvasGroupEnabler(false);

            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].button.interactable = false;
                buttons[i].text.text = optionsGroup[index].options[i];
                buttons[i].image.color = neutralColor;
            }

            for (var i = 0; i < optionsGroup.Count; i++)
                optionsGroup[i].bread.localPosition = startPosition.localPosition;
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            yield return new WaitForSeconds(.2f);

            var scaledTime = Time.fixedDeltaTime * timeScaler;

            float animationTime = 0;
            var moveDelay = 1f;

            while (animationTime <= moveDelay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / moveDelay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                optionsGroup[index].bread.localPosition =
                    Vector3.Lerp(startPosition.localPosition, midPosition.localPosition, animationCurveValue);

                yield return null;
            }

            questionText.text = paragraphs[optionsGroup[index].currentAnswer];

            animationTime = 0;
            while (animationTime <= _delay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _delay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                CanvasGroupAlpha(animationCurveValue);

                yield return null;
            }

            CanvasGroupEnabler(true);
            for (var i = 0; i < buttons.Count; i++)
                buttons[i].button.interactable = true;

            currentCoroutine = null;

            // if (index == 0)
            // {
            //     LOLSDK.Instance.SpeakText("Q" + (index + 1));
            // }
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return new WaitForSeconds(.25f);
            var scaledTime = Time.fixedDeltaTime * timeScaler;

            yield return StartCoroutine(
                ProgressionCanvas._instance.FillBar(index, ProgressionCanvas.ProgressionType.One));
            yield return StartCoroutine(ProgressionCanvas._instance.UnlockStuff(index));

            //Slide old bread from center to end
            float animationTime = 0;

            while (animationTime <= _delay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _delay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                optionsGroup[index].bread.localPosition =
                    Vector3.Lerp(midPosition.localPosition, endPosition.localPosition, animationCurveValue);

                yield return null;
            }

            animationTime = 0;

            while (animationTime <= _delay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _delay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                buttonsGroup.alpha = 1f - animationCurveValue;

                yield return null;
            }

            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;

            //Warning(Aldhair): LolSDK
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SubmitProgress(0, index + 13, 18);
            // #endif
            //Here we change the whole stuff
            //Main Loop
            if (index < optionsGroup.Count - 1)
            {
                index++;

                if (index == 1)
                {
                    CanvasGroupEnabler(false);

                    animationTime = 0;

                    while (animationTime <= _delay)
                    {
                        animationTime += scaledTime;
                        var percent = Mathf.Clamp01(animationTime / _delay);
                        var animationCurveValue = _animationCurve.Evaluate(percent);

                        CanvasGroupAlpha(1f - animationCurveValue);

                        yield return null;
                    }

                    yield return StartCoroutine(
                        CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));

                    questionText.text = paragraphs[optionsGroup[index].currentAnswer];
                    animationTime = 0;
                    while (animationTime <= _delay)
                    {
                        animationTime += scaledTime;
                        var percent = Mathf.Clamp01(animationTime / _delay);
                        var animationCurveValue = _animationCurve.Evaluate(percent);

                        CanvasGroupAlpha(animationCurveValue);

                        yield return null;
                    }

                    CanvasGroupEnabler(true);
                }

                questionText.text = paragraphs[optionsGroup[index].currentAnswer];
                //Slide new bread from start to center
                animationTime = 0;

                while (animationTime <= _delay)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _delay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    optionsGroup[index].bread.localPosition =
                        Vector3.Lerp(startPosition.localPosition, midPosition.localPosition, animationCurveValue);

                    yield return null;
                }

                //Reset button colors and setting the options
                // #if UNITY_WEBGL
                //                 LOLSDK.Instance.SpeakText("Q" + (index + 1));
                // #endif
                for (var i = 0; i < buttons.Count; i++)
                {
                    buttons[i].image.color = neutralColor;
                    buttons[i].text.text = optionsGroup[index].options[i];
                }


                //Button group appears (alpha 0 - 1)
                animationTime = 0;

                while (animationTime <= _delay)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _delay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    buttonsGroup.alpha = animationCurveValue;

                    yield return null;
                }

                buttonsGroup.interactable = true;
                buttonsGroup.blocksRaycasts = true;

                next = true;
                for (var i = 0; i < buttons.Count; i++)
                    buttons[i].button.interactable = true;
            }
            //The last one
            else
            {
                animationTime = 0;

                //Deactivate canvas group and dissapears
                CanvasGroupEnabler(false);
                while (animationTime <= _delay)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _delay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);
                    CanvasGroupAlpha(1f - animationCurveValue);
                    yield return null;
                }

                yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                //LOLSDK.Instance.CompleteGame();
            }


            currentCoroutine = null;
        }

        #region Utils

        private void CanvasGroupAlpha(float _alpha)
        {
            canvasGroup.alpha = _alpha;
        }

        private void CanvasGroupEnabler(bool _enable)
        {
            canvasGroup.interactable = _enable;
            canvasGroup.blocksRaycasts = _enable;
        }

        #endregion
    }
}