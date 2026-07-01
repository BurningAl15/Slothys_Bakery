using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;
    using Analytics;
    using DailyChallenge;

    [Serializable]
    public class OptionsPerQuestion
    {
        public Transform bread;
        public int currentQuestion;
        public double currentAnswer;
    }

    public class Minigame3_B : MiniGameParent
    {
        [Header("Canvas Groups")]
        [SerializeField]
        private CanvasGroup canvasGroup;
        [SerializeField] private CanvasGroup buttonsGroup;

        [Header("Needed Properties")]
        [SerializeField] private List<OptionsPerQuestion> optionsGroup = new List<OptionsPerQuestion>();

        [SerializeField] TMP_InputField inputField;

        [Header("Waypoint Positions")]
        [SerializeField] private Transform startPosition;
        [SerializeField] private Transform midPosition;
        [SerializeField] private Transform endPosition;

        [Header("Animation Properties")]
        [Range(1f, 2f)]
        [SerializeField]
        private float timeScaler;

        [SerializeField] private AnimationCurve _animationCurve;

        [Header("Button Colors")]
        [SerializeField] private Color goodColor;
        [SerializeField] private Color wrongColor;
        [SerializeField] private Color neutralColor;

        //Others
        private Coroutine currentCoroutine = null;
        [SerializeField] private int index = 0;
        private bool next = false;

        [SerializeField] private List<string> paragraphs = new List<string>();
        [SerializeField] private TextMeshProUGUI questionText;


        private void Update()
        {
            if (GameStates.Instance.GetGameState() == GameStates.States.Play)
                if (next)
                    next = false;
        }


        public void Option()
        {
            EvaluateInputField();
        }

        double GetInputValue()
        {
            string content = inputField.text;
            string[] split = content.Split(new char[] { ' ', '/' });

            if (split.Length >= 2)
            {
                int a, b;

                if (int.TryParse(split[0], out a) && int.TryParse(split[1], out b))
                {
                    if (split.Length >= 2)
                    {
                        return (double)a / b;
                    }
                }
            }
            return 0;
        }

        void ClearInputField()
        {
            inputField.text = "";
        }

        void CheckIfGreater(double _value)
        {
            if (_value > optionsGroup[index].currentAnswer)
            {
                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                MessageManager.Instance.Animate_Message(true);
                AnalyticsManager.Instance?.TrackAnswerCorrect(index);
            }
            else
            {
                ClearInputField();
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                MessageManager.Instance.Animate_Message(false);
                AnalyticsManager.Instance?.TrackAnswerWrong(index);
            }
        }

        void CheckIfEquals(double _value)
        {
            if (Math.Abs(_value - optionsGroup[index].currentAnswer) < .01f && Math.Abs(_value - optionsGroup[index].currentAnswer) > -.01f)
            {
                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                MessageManager.Instance.Animate_Message(true);
                AnalyticsManager.Instance?.TrackAnswerCorrect(index);
            }
            else
            {
                ClearInputField();
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                MessageManager.Instance.Animate_Message(false);
                AnalyticsManager.Instance?.TrackAnswerWrong(index);
            }
        }

        void CheckIfLess(double _value)
        {
            if (_value < optionsGroup[index].currentAnswer)
            {
                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                MessageManager.Instance.Animate_Message(true);
                AnalyticsManager.Instance?.TrackAnswerCorrect(index);
            }
            else
            {
                ClearInputField();
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                MessageManager.Instance.Animate_Message(false);
                AnalyticsManager.Instance?.TrackAnswerWrong(index);
            }
        }

        void EvaluateInputField()
        {
            double decimalValue_Fraction = GetInputValue();
            // float decimalValue = Convert.ToSingle(decimalValue_Fraction);
            // print("Decimal Value: "+decimalValue_Fraction);
            switch (optionsGroup[index].currentQuestion)
            {
                //Equals
                case 0:
                    CheckIfEquals(decimalValue_Fraction);
                    break;
                //Greater
                case 1:
                    CheckIfGreater(decimalValue_Fraction);
                    break;
                //Less
                case 2:
                    CheckIfLess(decimalValue_Fraction);
                    break;
            }
        }


        public override void Init()
        {
            SaveLoadState.Instance.SetActualState(2);
            SaveLoadState.Instance.Save();
            CanvasGroupAlpha(0);
            CanvasGroupEnabler(false);

            inputField.interactable = false;

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

            questionText.text = paragraphs[optionsGroup[index].currentQuestion];

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
            inputField.interactable = true;

            currentCoroutine = null;

            // if (index == 0)
            // {
            // }
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return new WaitForSeconds(.25f);
            var scaledTime = Time.fixedDeltaTime * timeScaler;

            yield return StartCoroutine(
                ProgressionCanvas.Instance.FillBar(index, ProgressionCanvas.ProgressionType.One));
            yield return StartCoroutine(ProgressionCanvas.Instance.UnlockStuff(index));

            // Analytics + Daily Challenge
            AnalyticsManager.Instance?.TrackLevelCompleted($"minigame3_round_{index}", true);
            DailyChallengeManager.Instance?.AddProgress("complete_rounds", 1);

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
                        CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));

                    questionText.text = paragraphs[optionsGroup[index].currentQuestion];
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

                questionText.text = paragraphs[optionsGroup[index].currentQuestion];
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
                ClearInputField();

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
                inputField.interactable = true;
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

                yield return StartCoroutine(CanvasAnimator.Instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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

        void NotUsed()
        {
            //             if (_answer == optionsGroup[index].correctAnswer)
            //             {
            //                 if (currentCoroutine == null)
            //                     currentCoroutine = StartCoroutine(Change(1f));
            //                 SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
            //             }
            //             else
            //             {
            //                 buttons[_answer].button.interactable = false;
            //                 SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
            //             }
        }
    }
}
