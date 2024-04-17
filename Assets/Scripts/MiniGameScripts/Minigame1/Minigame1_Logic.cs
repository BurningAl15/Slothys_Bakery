using System;
using System.Collections;
using System.Collections.Generic;
using INART.SlothyBakery.Base;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;

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

        [SerializeField] private bool isCorrect = false;
        public bool IsCorrect => isCorrect;


        public void Init(AnimationCurve animationCurve)
        {
            _rectTransform.localPosition = positionPoints.InitialPos;
            _animationCurve = animationCurve;

            questionText.text = questions[0];

            Interact(_buttonMenu, false);
            Interact(_inputFieldMenu, false);
            Interact(_signButtonMenu, false);
            SaveLoadState.instance.SetActualState(0);
            SaveLoadState.instance.Save();
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
            CanvasAnimator._instance.TurnButton(true);
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SpeakText(StringConstants.minigame1_questionA);
            // #endif
            questionText.text = questions[0];

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
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
                fraction._left._fraction.UpdateColor(success);
                inputField.text = "";
                Interact(_signButtonMenu, true);
                TurnOn_SignButtonMenu();
                MessageManager._instance.Animate_Message(true);
                CanvasAnimator._instance.TurnButton(false);
            }
            else
            {
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager._instance.Animate_Message(false);
                TurnOn_InputFieldMenu();
            }
        }



        //Step 2 - Check in Input
        public void CheckInputValue()
        {
            // int tempVal = GetInputValue();
            (int, int) tempFraction = GetInputValueTuple();

            if (tempFraction.Item1 == tempFraction.Item2)
            {
                fraction._left._fraction.MultiplyWith(tempFraction.Item2);
                int tempDenominator1 = fraction._left._fraction.Denominator * fraction._left._fraction.MultiplyVal;
                if (IsEquals(tempDenominator1, denominator2))
                {
                    inputField.text = "";
                    SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
                    fraction._left._fraction.UpdateColor(success);
                    TurnOn_SignButtonMenu();
                    Interact(_signButtonMenu, true);
                    MessageManager._instance.Animate_Message(true);
                    CanvasAnimator._instance.TurnButton(false);
                }
                else
                {
                    SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                    fraction._left._fraction.UpdateColor(wrong);
                    MessageManager._instance.Animate_Message(false);
                    inputField.text = "";
                }
            }
            else
            {
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager._instance.Animate_Message(false);
                inputField.text = "";
            }
        }

        public void Reset()
        {
            fraction._left._fraction.UpdateColor(normal);
            fraction._left._fraction.MultiplyWith(1);
            SoundManager._instance.Run_SFX(SoundManager.SoundType.Selection2);
        }

        //Step 3 - Comparing by signs
        public void CheckSignInput(int _sign)
        {
            if (_sign == (int)value)
            {
                isCorrect = true;
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
                Interact(_buttonMenu, false);
                Interact(_inputFieldMenu, false);
                Interact(_signButtonMenu, false);
                MessageManager._instance.Animate_Message(true);
            }
            else if (_sign != (int)value)
            {
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                MessageManager._instance.Animate_Message(false);
            }
        }

        //No
        public void TurnOn_InputFieldMenu()
        {
            TurnOffButtonMenu(_buttonMenu);
            TurnOnButtonMenu(_inputFieldMenu);
            HintManager._instance.TurnOn();
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SpeakText(StringConstants.minigame1_questionB);
            // #endif
            questionText.text = questions[1];
        }

        public void TurnOn_IfDenominatorsEquals_InputFieldMenu()
        {
            bool temp = IsEquals(denominator1, denominator2);

            if (!temp)
            {
                TurnOffButtonMenu(_buttonMenu);
                TurnOnButtonMenu(_inputFieldMenu);
                HintManager._instance.TurnOn();
                // #if UNITY_WEBGL
                //             LOLSDK.Instance.SpeakText(StringConstants.minigame1_questionB);
                // #endif
                questionText.text = questions[1];
            }
            else
            {
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                fraction._left._fraction.UpdateColor(wrong);
                MessageManager._instance.Animate_Message(false);
            }
        }

        public void TurnOn_SignButtonMenu()
        {
            TurnOffButtonMenu(_buttonMenu);
            TurnOffButtonMenu(_inputFieldMenu);
            TurnOnButtonMenu(_signButtonMenu);
            HintManager._instance.TurnOff();
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SpeakText(StringConstants.minigame1_questionC);
            // #endif
            questionText.text = questions[2];
        }

        public void TurnOff_InputFieldMenu()
        {
            TurnOnButtonMenu(_buttonMenu);
            TurnOffButtonMenu(_inputFieldMenu);
            HintManager._instance.TurnOff();
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SpeakText(StringConstants.minigame1_questionA);
            // #endif
            Reset();
            questionText.text = questions[0];
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
            HintManager._instance.TurnOff();
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

        (int, int) GetInputValueTuple()
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
                        return (a, b);
                    }
                }
            }
            return (1, 1);
        }

    }
}