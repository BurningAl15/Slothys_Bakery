using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
// using LoLSDK;
using UnityEngine.SceneManagement;

namespace INART.SlothyBakery.CanvasAnimator
{
    public class CanvasAnimator : MonoBehaviour
    {
        public static CanvasAnimator _instance;

        public enum CanvasAnimationTypes
        {
            Horizontal_LeftRight,
            Horizontal_RightLeft,
            Vertical_BottomTop,
            Diagonally_LeftRight,
            Diagonally_RightLeft
        }

        public CanvasAnimationTypes canvasAnimationType;

        [Header("Animations")]
        [Tooltip("The character you want to animate")]
        [SerializeField] private RectTransform character;
        [SerializeField] private Animator characterAnimator;

        [Tooltip("The SpeechBubble where the text will appear")]
        [SerializeField] private CanvasGroup speechBubbleCanvasGroup;
        [SerializeField] private CanvasGroup speechBubbleGroup;

        [Tooltip("The waiting icon that animates when you didn't press the button to go to the next phrase ")]
        [SerializeField] private Animator waitingImage;

        [Tooltip("The text in the SpeechBubble")]
        [SerializeField] private TextMeshProUGUI speechText;

        [SerializeField] private Vector3 startPosition, endPosition;
        [SerializeField] private bool autoCalculateStartPosition = false;
        private Vector3 dynamicStartPosition;
        [SerializeField] private Color fontColor;

        [Header("First Phrases")]
        [Tooltip("This phrases appear before you do anything")]
        [SerializeField] private List<string> introPhrases = new List<string>();

        [Header("Second Phrases")]
        [Tooltip("This phrases appear after you finish the first try of the minigame succesfully")]
        [FormerlySerializedAs("phrases")]
        [SerializeField] private List<string> congratPhrases = new List<string>();

        [Header("End Phrases")]
        [Tooltip("This phrases appear after you finish the main game, then the character says this and dissapears")]
        [SerializeField] private List<string> endPhrases = new List<string>();

        [Header("Animation Parameters")]
        [SerializeField] private PhraseUtils.PhraseType _phraseType;
        [Range(.1f, 2f)][SerializeField] private float timeScaler;


        [SerializeField] private AnimationCurve _animationCurve;
        private Vector3 tempStartPosition;

        [Header("Instructions")]
        [SerializeField] private RectTransform instructions;
        [SerializeField] private CanvasGroup _questionButton;

        private bool isTesting = false;

        private int actualLevel;

        [SerializeField] private TextMeshProUGUI testingText;

        private bool finishInstructions = false;

        private Coroutine currentCoroutine = null;


        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            if (autoCalculateStartPosition)
                switch (canvasAnimationType)
                {
                    case CanvasAnimationTypes.Vertical_BottomTop:
                        dynamicStartPosition = new Vector3(endPosition.x, -800f, endPosition.z);
                        break;
                    case CanvasAnimationTypes.Horizontal_LeftRight:
                        dynamicStartPosition = new Vector3(-1000f, endPosition.y, endPosition.z);
                        break;
                    case CanvasAnimationTypes.Horizontal_RightLeft:
                        dynamicStartPosition = new Vector3(1000f, endPosition.y, endPosition.z);
                        break;
                    case CanvasAnimationTypes.Diagonally_LeftRight:
                        dynamicStartPosition = new Vector3(-1000f, -800f, endPosition.z);
                        break;
                    case CanvasAnimationTypes.Diagonally_RightLeft:
                        dynamicStartPosition = new Vector3(1000f, -800f, endPosition.z);
                        break;
                }

            character.localPosition = startPosition;
            // speechBubble.color = new Color(1, 1, 1, 0);
            speechBubbleCanvasGroup.alpha = 0;
            if (speechBubbleGroup != null)
                speechBubbleGroup.alpha = 1;
            speechText.text = "";
            speechText.color = new Color(fontColor.r, fontColor.g, fontColor.b, 0);
            waitingImage.gameObject.SetActive(false);
            instructions.GetComponent<CanvasGroup>().alpha = 0;
            if (_questionButton != null)
            {
                _questionButton.alpha = 0;
                _questionButton.interactable = false;
                _questionButton.blocksRaycasts = false;
            }
        }

        public void Test(bool _isTesting)
        {
            isTesting = _isTesting;
            testingText.gameObject.SetActive(isTesting);
        }

        /// <summary>
        /// Runs the first animation, reading the introPhrases
        /// </summary>
        /// <param name="_animationDuration">How much every animation lasts</param>
        /// <param name="_phraseType">Set the phrase type (INTRO,CONGRAT,END), this are the phrase lists we can choose</param>
        /// <returns></returns>
        public IEnumerator CanvasAnimation(float _animationDuration, PhraseUtils.PhraseType _phraseType)
        {
            if (!isTesting)
            {
                var scaledTime = Time.fixedDeltaTime * timeScaler;

                //Fade In SpeechBubble and SpeechText
                float animationTime = 0;

                tempStartPosition = autoCalculateStartPosition ? dynamicStartPosition : startPosition;
                character.localPosition = tempStartPosition;
                speechText.text = "";

                while (animationTime <= _animationDuration)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _animationDuration);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    character.localPosition = Vector3.Lerp(tempStartPosition, endPosition, animationCurveValue);
                    yield return null;
                }

                animationTime = 0;
                while (animationTime <= _animationDuration)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _animationDuration);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    // speechBubble.color = new Color(1, 1, 1, animationCurveValue);
                    speechBubbleCanvasGroup.alpha = animationCurveValue;

                    speechText.color = new Color(fontColor.r, fontColor.g, fontColor.b, animationCurveValue);
                    yield return null;
                }


                //TypeWrite
                yield return TypeWrite(_phraseType);

                #region Just for First Level


                if (SceneUtils.IsCurrentSceneFirstLevel())
                {
                    if (speechBubbleGroup != null)
                    {
                        while (animationTime > 0)
                        {
                            speechBubbleGroup.alpha = _animationCurve.Evaluate(animationTime / _animationDuration);
                            animationTime -= scaledTime;
                            yield return null;
                        }
                        speechBubbleGroup.alpha = _animationCurve.Evaluate(0);
                    }

                    if (_phraseType == PhraseUtils.PhraseType.INTRO)
                    {
                        yield return ExplanationManager._instance.Instruction();
                        yield return new WaitUntil(() => finishInstructions);
                    }
                }

                #endregion

                //Fade Out SpeechBubble and SpeechText
                animationTime = 0;
                while (animationTime <= _animationDuration)
                {
                    animationTime += scaledTime;
                    var percent = 1 - Mathf.Clamp01(animationTime / _animationDuration);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    // speechBubble.color = new Color(1, 1, 1, animationCurveValue);
                    speechBubbleCanvasGroup.alpha = animationCurveValue;

                    speechText.color = new Color(fontColor.r, fontColor.g, fontColor.b, animationCurveValue);
                    yield return null;
                }

                #region Just for First Level

                if (SceneUtils.IsCurrentSceneFirstLevel())
                {
                    if (_phraseType == PhraseUtils.PhraseType.INTRO)
                        ExplanationManager._instance.TurnOff();
                }

                #endregion

                yield return new WaitForSeconds(PhraseUtils.waitUntilPrevious);

                //Or enter the animation here
                animationTime = 0;
                while (animationTime <= _animationDuration)
                {
                    animationTime += scaledTime;
                    var percent = 1 - Mathf.Clamp01(animationTime / _animationDuration);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    character.localPosition = Vector3.Lerp(tempStartPosition, endPosition, animationCurveValue);
                    yield return null;
                }
                if (speechBubbleGroup != null)
                    speechBubbleGroup.alpha = 1;

            }
        }

        public IEnumerator InstructionsAnimation(bool runAnimation, bool turnOnInstructions = true,
            bool turnOnQuestionButton = true)
        {
            var scaledTime = Time.fixedDeltaTime * timeScaler;

            var maxDelay = .5f;

            CanvasGroup _tempCanvas = instructions.GetComponent<CanvasGroup>();

            if (runAnimation)
            {
                for (float i = 0; i <= maxDelay; i += scaledTime)
                {
                    var percent = Mathf.Clamp01(i / maxDelay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    if (turnOnInstructions)
                    {
                        _tempCanvas.alpha = animationCurveValue;
                    }

                    if (_questionButton != null)
                    {
                        if (turnOnQuestionButton)
                        {
                            _questionButton.alpha = animationCurveValue;
                        }
                    }

                    yield return null;
                }
            }
            else if (!runAnimation)
            {
                for (var i = maxDelay; i > 0; i -= scaledTime)
                {
                    var percent = Mathf.Clamp01(i / maxDelay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    // || _tempCanvas.alpha > 0
                    if (turnOnInstructions)
                        _tempCanvas.alpha = animationCurveValue;
                    if (_questionButton != null)
                    {
                        // || _questionButton.alpha > 0
                        if (turnOnQuestionButton)
                            _questionButton.alpha = animationCurveValue;
                    }

                    yield return null;
                }
            }
        }

        IEnumerator TypeWrite(PhraseUtils.PhraseType _phraseType)
        {
            var tempText = "";

            // Typewrite effect Reading one of the multiple list of phrases
            var wordCounter = 0;
            var tempPhrase = new List<string>();

            switch (_phraseType)
            {
                case PhraseUtils.PhraseType.INTRO:
                    tempPhrase = introPhrases;
                    break;
                case PhraseUtils.PhraseType.CONGRAT:
                    tempPhrase = congratPhrases;
                    break;
                case PhraseUtils.PhraseType.END:
                    tempPhrase = endPhrases;
                    break;
            }

            while (true)
                //If wordcounter is less than the tempPhrase size, then run
                if (wordCounter < tempPhrase.Count)
                {
                    // #if UNITY_WEBGL
                    //                     switch (SceneManager.GetActiveScene().name)
                    //                     {
                    //                         case "Minigame1":
                    //                             LoLSDK_SetMinigameText(1, _phraseType, wordCounter);
                    //                             actualLevel = 1;
                    //                             break;
                    //                         case "Minigame2":
                    //                             LoLSDK_SetMinigameText(2, _phraseType, wordCounter);
                    //                             actualLevel = 2;
                    //                             break;
                    //                         case "Minigame3":
                    //                             LoLSDK_SetMinigameText(3, _phraseType, wordCounter);
                    //                             actualLevel = 3;
                    //                             break;
                    //                         case "FinalCutscene":
                    //                             LoLSDK_SetMinigameText(4, _phraseType, wordCounter);
                    //                             actualLevel = 4;
                    //                             break;
                    //                     }
                    // #endif
                    characterAnimator.SetBool("Talk", true);
                    for (var i = 0; i < tempPhrase[wordCounter].Length; i++)
                    {
                        tempText += tempPhrase[wordCounter][i];
                        speechText.text = "" + tempText;
                        yield return new WaitForSeconds(PhraseUtils.waitUntilNextLetter);
                    }

                    while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(0))
                    {
                        waitingImage.gameObject.SetActive(true);
                        characterAnimator.SetBool("Talk", false);
                        yield return null;
                    }

                    waitingImage.gameObject.SetActive(false);

                    wordCounter++;
                    tempText = "";
                    //LOL Instruction Speak
                    // if (_phraseType == PhraseUtils.PhraseType.INTRO && wordCounter >= tempPhrase.Count &&
                    //     actualLevel != 4)
                    // {
                    //     LOLSDK.Instance.SpeakText(StringConstants.Instruction + actualLevel);
                    // }
                }
                else
                {
                    break;
                }

            characterAnimator.SetBool("Talk", false);
            yield return new WaitForSeconds(PhraseUtils.waitUntilPrevious);
        }

        public void TurnButton(bool _buttonActivation)
        {
            if (_questionButton != null)
            {
                _questionButton.interactable = _buttonActivation;
                _questionButton.blocksRaycasts = _buttonActivation;
            }
        }

        // public void EnableTurnButton(bool _buttonActivation)
        // {
        //     _questionButton.gameObject.SetActive(true);
        //     TurnButton(_buttonActivation);
        //     _questionButton.alpha = 0;
        //     _questionButton.interactable = false;
        //     _questionButton.blocksRaycasts = false;

        // }

        public IEnumerator EnableTurnButton(bool _buttonActivation)
        {
            var scaledTime = Time.fixedDeltaTime * timeScaler;
            var maxDelay = .5f;

            for (float i = 0; i <= maxDelay; i += scaledTime)
            {
                var percent = Mathf.Clamp01(i / maxDelay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                if (_questionButton != null)
                {
                    _questionButton.alpha = animationCurveValue;
                }

                yield return null;
            }
            TurnButton(_buttonActivation);
        }

        // void LoLSDK_SetMinigameText(int _level, PhraseUtils.PhraseType _tempPhraseType,int _wordCounter){
        //     if (_level != 4)
        //     {
        //         switch (_tempPhraseType)
        //         {
        //             case PhraseUtils.PhraseType.INTRO:
        //                 LOLSDK.Instance.SpeakText(StringConstants.IntroPhrase + _level + _wordCounter);
        //                 break;
        //             case PhraseUtils.PhraseType.CONGRAT:
        //                 LOLSDK.Instance.SpeakText(StringConstants.CongratPhrase + _level + _wordCounter);
        //                 break;
        //             case PhraseUtils.PhraseType.END:
        //                 LOLSDK.Instance.SpeakText(StringConstants.EndPhrase + _level + _wordCounter);
        //                 break;
        //         }
        //     }
        //     else
        //     {
        //         LOLSDK.Instance.SpeakText(StringConstants.Final + _wordCounter);
        //     }
        // }

        public void TurnOnFinish()
        {
            finishInstructions = true;
            ExplanationManager._instance.TurnOffButton();
        }
    }
}