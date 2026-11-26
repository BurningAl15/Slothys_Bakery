using System;
using System.Collections;
using System.Collections.Generic;
using INART.SlothyBakery.Base;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fatcat.Slothy.Bakery.Shell;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;
    using Analytics;

    [Serializable]
    public class MenuPositionPoints
    {
        [SerializeField] private Vector3 initialPos;
        public Vector3 InitialPos => initialPos;

        [SerializeField] private Vector3 endPos;
        public Vector3 EndPos => endPos;
    }

    public class Minigame1_Logic : MonoBehaviour
    {
        [Header("Logic")][SerializeField] private List<string> questions = new List<string>();
        [SerializeField] private TextMeshProUGUI questionText;

        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup _buttonMenu;

        [SerializeField] private CanvasGroup _inputFieldMenu;
        [SerializeField] TMP_InputField inputField;

        [SerializeField] private CanvasGroup _signButtonMenu;


        [Header("Fraction")][SerializeField] private MenuPositionPoints positionPoints;

        [Header("WholeContainer")]
        [SerializeField] private RectTransform _rectTransform;

        [SerializeField] private Button yesButton, noButton;

        [Header("Colors")]
        public Color normal;
        public Color wrong;
        public Color success;

        private CompareFraction fraction;

        private int denominator1;
        private int denominator2;
        private Sign value;

        private AnimationCurve _animationCurve;
        private int activeQuestionIndex;

        [SerializeField] private bool isCorrect = false;
        public bool IsCorrect => isCorrect;
        public bool HasRequiredQuestionPrompts => questions != null && questions.Count >= 3 &&
            questionText != null && _buttonMenu != null && _inputFieldMenu != null &&
            _signButtonMenu != null && inputField != null && _rectTransform != null && positionPoints != null;


        public void Init(AnimationCurve animationCurve)
        {
            _rectTransform.localPosition = positionPoints.InitialPos;
            _animationCurve = animationCurve;
            isCorrect = false;

            SetQuestionPrompt(0);
            StartCoroutine(LoadLocalizedQuestionPrompts());

            Interact(_buttonMenu, false);
            Interact(_inputFieldMenu, false);
            Interact(_signButtonMenu, false);
            SaveLoadState.Instance?.EnterMinigame(0, SceneTransition.SceneUtils.FirstMinigameScene);
        }

        public IEnumerator MoveMenu(bool run)
        {
            Vector3 init = run ? positionPoints.InitialPos : positionPoints.EndPos;
            Vector3 end = run ? positionPoints.EndPos : positionPoints.InitialPos;


            float movement = 0;
            float evaluate;
            float maxTime = Utils_Minigame1_Settings.maxTime;
            while (movement < maxTime)
            {
                evaluate = _animationCurve.Evaluate(movement / maxTime);
                FractionExtensionMethods.Lerp(_rectTransform, init, end, evaluate);
                movement += Time.deltaTime;
                yield return null;
            }

            evaluate = _animationCurve.Evaluate(maxTime / maxTime);
            FractionExtensionMethods.Lerp(_rectTransform, init, end, evaluate);
        }

        public void InitializeDenominator(int _denominator1, int _denominator2, Sign _value)
        {
            TurnOffButtonMenu(_inputFieldMenu);
            TurnOffButtonMenu(_signButtonMenu);
            TurnOnButtonMenu(_buttonMenu);
            denominator1 = _denominator1;
            denominator2 = _denominator2;
            value = _value;
        }

        public void InitializeDenominator(CompareFraction _fraction)
        {
            TurnOffButtonMenu(_inputFieldMenu);
            TurnOffButtonMenu(_signButtonMenu);
            TurnOnButtonMenu(_buttonMenu);
            CanvasAnimator.Instance.TurnButton(true);
            SetQuestionPrompt(0);

            fraction = _fraction;

            denominator1 = fraction._left.denominator;
            denominator2 = fraction._right.denominator;

            value = fraction._value;
        }

        //Step 1 - Check by buttons (yes - no)
        public void CheckDenominators()
        {
            bool temp = IsEquals(denominator1, denominator2);
            // isCorrect = temp;

            /*
             If true, then do something to feedback in screen
             If false, then do something to feedback a bad behaviour
             */

            if (temp)
            {
                //Run a sound maybe
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                fraction._left._fraction.UpdateColor(success);
                inputField.text = "";
                Interact(_signButtonMenu, true);
                TurnOn_SignButtonMenu();
                MessageManager.Instance.Animate_Message(true);
                CanvasAnimator.Instance.TurnButton(false);
            }
            else
            {
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager.Instance.Animate_Message(false);
                TurnOn_InputFieldMenu();
            }
        }



        //Step 2 - Check in Input
        public void CheckInputValue()
        {
            if (!TryGetInputTuple(out int numeratorMultiplier, out int denominatorMultiplier) ||
                numeratorMultiplier <= 0 || denominatorMultiplier <= 0 ||
                numeratorMultiplier != denominatorMultiplier)
            {
                RejectMultiplierInput();
                return;
            }

            fraction._left._fraction.MultiplyWith(denominatorMultiplier);
            int tempDenominator1 = fraction._left._fraction.Denominator * fraction._left._fraction.MultiplyVal;
            if (IsEquals(tempDenominator1, denominator2))
            {
                inputField.text = "";
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                fraction._left._fraction.UpdateColor(success);
                TurnOn_SignButtonMenu();
                Interact(_signButtonMenu, true);
                MessageManager.Instance.Animate_Message(true);
                CanvasAnimator.Instance.TurnButton(false);
            }
            else
            {
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager.Instance.Animate_Message(false);
                inputField.text = "";
            }
        }

        private void RejectMultiplierInput()
        {
            Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
            SoundManager.Instance?.Run_SFX(SoundManager.SoundType.Wrong);
            fraction._left._fraction.UpdateColor(wrong);
            MessageManager.Instance?.Animate_Message(false);
            inputField.text = "";
        }

        public void Reset()
        {
            fraction._left._fraction.UpdateColor(normal);
            fraction._left._fraction.MultiplyWith(1);
            SoundManager.Instance.Run_SFX(SoundManager.SoundType.Selection2);
        }

        //Step 3 - Comparing by signs
        public void CheckSignInput(int _sign)
        {
            if (isCorrect) return;

            if (_sign == (int)value)
            {
                isCorrect = true;
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(true);
                Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(true, fraction != null && fraction._left != null ? fraction._left.plate.transform.position : Vector3.zero);
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                Interact(_buttonMenu, false);
                Interact(_inputFieldMenu, false);
                Interact(_signButtonMenu, false);
                MessageManager.Instance.Animate_Message(true);

                // Analytics
                AnalyticsManager.Instance?.TrackAnswerCorrect(0);
                AnalyticsManager.Instance?.TrackCustom("sign_placed", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "sign",    _sign },
                    { "correct", true  }
                });
            }
            else if (_sign != (int)value)
            {
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
                Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(false, fraction != null && fraction._right != null ? fraction._right.plate.transform.position : Vector3.zero);
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                MessageManager.Instance.Animate_Message(false);

                // Analytics
                AnalyticsManager.Instance?.TrackAnswerWrong(0);
                AnalyticsManager.Instance?.TrackCustom("sign_placed", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "sign",    _sign },
                    { "correct", false }
                });
            }
        }

        //No
        public void TurnOn_InputFieldMenu()
        {
            TurnOffButtonMenu(_buttonMenu);
            TurnOnButtonMenu(_inputFieldMenu);
            HintManager.Instance.TurnOn();
            SetQuestionPrompt(1);
        }

        public void TurnOn_IfDenominatorsEquals_InputFieldMenu()
        {
            bool temp = IsEquals(denominator1, denominator2);

            if (!temp)
            {
                TurnOffButtonMenu(_buttonMenu);
                TurnOnButtonMenu(_inputFieldMenu);
                HintManager.Instance.TurnOn();
                SetQuestionPrompt(1);
            }
            else
            {
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager.Instance.Animate_Message(false);
            }
        }

        public void TurnOn_SignButtonMenu()
        {
            TurnOffButtonMenu(_buttonMenu);
            TurnOffButtonMenu(_inputFieldMenu);
            TurnOnButtonMenu(_signButtonMenu);
            HintManager.Instance.TurnOff();
            SetQuestionPrompt(2);
        }

        public void TurnOff_InputFieldMenu()
        {
            TurnOnButtonMenu(_buttonMenu);
            TurnOffButtonMenu(_inputFieldMenu);
            HintManager.Instance.TurnOff();
            Reset();
            SetQuestionPrompt(0);
        }

        private void SetQuestionPrompt(int index)
        {
            if (questions == null || index < 0 || index >= questions.Count || questionText == null) return;
            activeQuestionIndex = index;
            BakeryPromptLocalization.SetText(questionText, questions[index]);
        }

        private IEnumerator LoadLocalizedQuestionPrompts()
        {
            string[] keys = { "minigame.mg1.prompt.0", "minigame.mg1.prompt.1", "minigame.mg1.prompt.2" };
            int count = Mathf.Min(keys.Length, questions.Count);
            for (int i = 0; i < count; i++)
            {
                int promptIndex = i;
                yield return BakeryPromptLocalization.Resolve(keys[i], questions[i], value => questions[promptIndex] = value);
            }

            SetQuestionPrompt(activeQuestionIndex);
        }


        void TurnOffButtonMenu(CanvasGroup _canvasGroup)
        {
            _canvasGroup.alpha = 0;
            Interact(_canvasGroup, false);
        }

        void TurnOnButtonMenu(CanvasGroup _canvasGroup)
        {
            _canvasGroup.alpha = 1;
            Interact(_canvasGroup, true);
            HintManager.Instance.TurnOff();
        }

        void Interact(CanvasGroup _canvasGroup, bool canInteract)
        {
            _canvasGroup.interactable = canInteract;
            _canvasGroup.blocksRaycasts = canInteract;
        }

        public void ResetIsCorrect()
        {
            isCorrect = false;
        }

        bool IsEquals(int denominator1, int denominator2)
        {
            return denominator1 == denominator2;
        }

        //Not Used
        // int GetInputValue()
        // {
        //     string content = inputField.text;
        //     string[] split=content.Split(new char[] {' ','/'});

        //     if(split.Length>=2){
        //         int a, b;

        //         if(int.TryParse(split[0], out a) && int.TryParse(split[1], out b)) {
        //             if(split.Length >= 2) {
        //                 return a / b;
        //             }
        //         }
        //     }
        //     return 0;
        // }

        bool TryGetInputTuple(out int numeratorMultiplier, out int denominatorMultiplier)
        {
            numeratorMultiplier = 0;
            denominatorMultiplier = 0;
            if (string.IsNullOrWhiteSpace(inputField.text)) return false;

            string trimmed = inputField.text.Trim();
            string[] split;
            if (trimmed.IndexOf('/') >= 0)
            {
                if (trimmed.IndexOf('/') != trimmed.LastIndexOf('/')) return false;
                split = trimmed.Split('/');
                if (split.Length == 2)
                {
                    split[0] = split[0].Trim();
                    split[1] = split[1].Trim();
                }
            }
            else
            {
                split = trimmed.Split(new[] { ' ', '\t', '\r', '\n' },
                    System.StringSplitOptions.RemoveEmptyEntries);
            }

            return split.Length == 2 &&
                int.TryParse(split[0], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out numeratorMultiplier) &&
                int.TryParse(split[1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out denominatorMultiplier);
        }

    }
}
