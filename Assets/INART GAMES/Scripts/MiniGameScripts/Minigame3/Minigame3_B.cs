using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Fatcat.Slothy.Bakery.Shell;

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
        private bool contentReady;
        private bool answerAccepted;

        [SerializeField] private List<string> paragraphs = new List<string>();
        [SerializeField] private TextMeshProUGUI questionText;
        private Coroutine promptLocalizationRoutine;


        private void Update()
        {
            if (GameStates.Instance.GetGameState() == GameStates.States.Play)
                if (next)
                    next = false;
        }


        public void Option()
        {
            if (!contentReady || answerAccepted || currentCoroutine != null ||
                index < 0 || index >= optionsGroup.Count || inputField == null)
                return;

            if (!TryParseFraction(inputField.text, out double answer))
            {
                RejectAnswer();
                return;
            }

            OptionsPerQuestion question = optionsGroup[index];
            bool isCorrect = question.currentQuestion == 0
                ? Math.Abs(answer - question.currentAnswer) < 1e-4d
                : question.currentQuestion == 1
                    ? answer > question.currentAnswer
                    : question.currentQuestion == 2 && answer < question.currentAnswer;

            if (isCorrect)
                AcceptAnswer();
            else
                RejectAnswer();
        }

        private static bool TryParseFraction(string content, out double value)
        {
            value = 0d;
            if (string.IsNullOrWhiteSpace(content)) return false;

            string trimmed = content.Trim();
            string[] parts;
            if (trimmed.IndexOf('/') >= 0)
            {
                if (trimmed.IndexOf('/') != trimmed.LastIndexOf('/')) return false;
                parts = trimmed.Split('/');
                if (parts.Length == 2)
                {
                    parts[0] = parts[0].Trim();
                    parts[1] = parts[1].Trim();
                }
            }
            else
            {
                parts = trimmed.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
            }

            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numerator) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int denominator) ||
                denominator == 0)
                return false;

            value = (double)numerator / denominator;
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        void ClearInputField()
        {
            inputField.text = "";
        }

        private void AcceptAnswer()
        {
            answerAccepted = true;
            inputField.interactable = false;
            Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(true);
            Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(true, midPosition != null ? midPosition.position : Vector3.zero);
            SoundManager.Instance?.Run_SFX(SoundManager.SoundType.Success);
            MessageManager.Instance?.Animate_Message(true);
            AnalyticsManager.Instance?.TrackAnswerCorrect(index);
            currentCoroutine = StartCoroutine(Change(1f));
        }

        private void RejectAnswer()
        {
            Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
            Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(false, midPosition != null ? midPosition.position : Vector3.zero);
            ClearInputField();
            SoundManager.Instance?.Run_SFX(SoundManager.SoundType.Wrong);
            MessageManager.Instance?.Animate_Message(false);
            AnalyticsManager.Instance?.TrackAnswerWrong(index);
        }


        public override void Init()
        {
            contentReady = ValidateContent();
            index = 0;
            answerAccepted = false;
            if (!contentReady)
            {
                if (inputField != null) inputField.interactable = false;
                if (buttonsGroup != null)
                {
                    buttonsGroup.interactable = false;
                    buttonsGroup.blocksRaycasts = false;
                }
                return;
            }

            if (Fatcat.Slothy.Bakery.Modes.BakerySession.OffStory &&
                Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.HasValue)
                Fatcat.Slothy.Bakery.Modes.BakerySession.ShuffleWithSeed(
                    optionsGroup, Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.Value ^ 0x303);

            int resumeIdx = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetRoundCheckpoint(2) : 0;
            index = Mathf.Clamp(resumeIdx, 0, optionsGroup.Count - 1);
            if (promptLocalizationRoutine != null) StopCoroutine(promptLocalizationRoutine);
            promptLocalizationRoutine = StartCoroutine(LoadLocalizedQuestionPrompts());
            SaveLoadState.Instance?.EnterMinigame(2, SceneUtils.ThirdMinigameScene);
            ProgressionCanvas.Instance?.ApplyCompletedRounds(index);
            CanvasGroupAlpha(0);
            CanvasGroupEnabler(false);

            inputField.interactable = false;
            ConfigureNumericFractionInput();

            for (var i = 0; i < optionsGroup.Count; i++)
                optionsGroup[i].bread.localPosition = i < index ? endPosition.localPosition : startPosition.localPosition;
        }

        private void ConfigureNumericFractionInput()
        {
            if (inputField == null) return;
            inputField.contentType = TMP_InputField.ContentType.Custom;
            inputField.inputType = TMP_InputField.InputType.Standard;
            inputField.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            inputField.characterLimit = 7;
            inputField.onValidateInput = ValidateFractionChar;
        }

        private static char ValidateFractionChar(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar)) return addedChar;
            if (addedChar == '/' && (string.IsNullOrEmpty(text) || !text.Contains("/"))) return addedChar;
            return '\0';
        }

        private bool ValidateContent()
        {
            if (optionsGroup == null || optionsGroup.Count == 0 || paragraphs == null ||
                inputField == null || questionText == null || canvasGroup == null || buttonsGroup == null ||
                startPosition == null || midPosition == null || endPosition == null || _animationCurve == null ||
                _animationCurve.length == 0 || timeScaler <= 0f || float.IsNaN(timeScaler) || float.IsInfinity(timeScaler))
            {
                Debug.LogError("[Bakery MG3] Missing round content or required scene references.", this);
                return false;
            }

            for (int i = 0; i < optionsGroup.Count; i++)
            {
                OptionsPerQuestion question = optionsGroup[i];
                if (question == null || question.bread == null ||
                    question.currentQuestion < 0 || question.currentQuestion >= paragraphs.Count || question.currentQuestion > 2 ||
                    double.IsNaN(question.currentAnswer) || double.IsInfinity(question.currentAnswer))
                {
                    Debug.LogError($"[Bakery MG3] Invalid authored data at round {i}.", this);
                    return false;
                }
            }

            return true;
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            if (!contentReady) yield break;
            yield return new WaitForSeconds(.2f);

            float moveDelay = 0.65f;
            float fadeDur = Mathf.Max(0.25f, _delay);

            if (optionsGroup[index].bread != null)
            {
                yield return optionsGroup[index].bread
                    .DOLocalMove(midPosition.localPosition, moveDelay)
                    .SetEase(Ease.OutCubic)
                    .WaitForCompletion();
            }

            SetQuestionPrompt(optionsGroup[index].currentQuestion);

            if (canvasGroup != null)
            {
                yield return canvasGroup.DOFade(1f, fadeDur).SetEase(Ease.OutQuad).WaitForCompletion();
            }

            CanvasGroupEnabler(true);
            inputField.interactable = true;

            currentCoroutine = null;
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return new WaitForSeconds(.2f);
            float dur = Mathf.Max(0.25f, _delay * 0.6f);

            yield return StartCoroutine(
                ProgressionCanvas.Instance.FillBar(index, ProgressionCanvas.ProgressionType.One));
            yield return StartCoroutine(ProgressionCanvas.Instance.UnlockStuff(index));

            // Analytics + Daily Challenge + Checkpoint before transition delay
            AnalyticsManager.Instance?.TrackLevelCompleted($"minigame3_round_{index}", true);
            DailyChallengeManager.Instance?.AddProgress("complete_rounds", 1);
            if (index < optionsGroup.Count - 1)
                SaveLoadState.Instance?.SaveRoundCheckpoint(2, SceneUtils.ThirdMinigameScene, index + 1);
            else
                SaveLoadState.Instance?.CompleteStoryChapter(0, SceneUtils.FirstMinigameScene, true);

            // Slide old bread from center to end + fade out buttons with DOTween
            var outSeq = DOTween.Sequence();
            if (optionsGroup[index].bread != null)
                outSeq.Join(optionsGroup[index].bread.DOLocalMove(endPosition.localPosition, dur).SetEase(Ease.InCubic));
            if (buttonsGroup != null)
                outSeq.Join(buttonsGroup.DOFade(0f, dur).SetEase(Ease.OutQuad));
            yield return outSeq.WaitForCompletion();

            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;

            if (index < optionsGroup.Count - 1)
            {
                index++;
                answerAccepted = false;

                if (index == 1)
                {
                    CanvasGroupEnabler(false);
                    if (canvasGroup != null)
                        yield return canvasGroup.DOFade(0f, dur).SetEase(Ease.OutQuad).WaitForCompletion();

                    yield return StartCoroutine(
                        CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));

                    SetQuestionPrompt(optionsGroup[index].currentQuestion);
                    if (canvasGroup != null)
                        yield return canvasGroup.DOFade(1f, dur).SetEase(Ease.OutQuad).WaitForCompletion();

                    CanvasGroupEnabler(true);
                }

                SetQuestionPrompt(optionsGroup[index].currentQuestion);
                if (optionsGroup[index].bread != null)
                {
                    yield return optionsGroup[index].bread
                        .DOLocalMove(midPosition.localPosition, dur)
                        .SetEase(Ease.OutCubic)
                        .WaitForCompletion();
                }

                ClearInputField();

                if (buttonsGroup != null)
                    yield return buttonsGroup.DOFade(1f, dur).SetEase(Ease.OutQuad).WaitForCompletion();

                buttonsGroup.interactable = true;
                buttonsGroup.blocksRaycasts = true;

                next = true;
                inputField.interactable = true;
            }
            else
            {
                CanvasGroupEnabler(false);
                if (canvasGroup != null)
                    yield return canvasGroup.DOFade(0f, dur).SetEase(Ease.OutQuad).WaitForCompletion();

                yield return StartCoroutine(CanvasAnimator.Instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                if (SceneTransitionController.Instance != null && SceneTransitionController.Instance.gameObject.activeInHierarchy)
                    yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
                SceneUtils.ToNextScene();
            }

            currentCoroutine = null;
        }

        #region Utils

        private void SetQuestionPrompt(int promptIndex)
        {
            if (paragraphs == null || promptIndex < 0 || promptIndex >= paragraphs.Count || questionText == null) return;
            BakeryPromptLocalization.SetText(questionText, paragraphs[promptIndex]);
        }

        private IEnumerator LoadLocalizedQuestionPrompts()
        {
            string[] keys = { "minigame.mg3.prompt.0", "minigame.mg3.prompt.1", "minigame.mg3.prompt.2" };
            int count = Mathf.Min(keys.Length, paragraphs.Count);
            for (int i = 0; i < count; i++)
            {
                int promptIndex = i;
                yield return BakeryPromptLocalization.Resolve(keys[i], paragraphs[i], value => paragraphs[promptIndex] = value);
            }

            if (index >= 0 && index < optionsGroup.Count)
                SetQuestionPrompt(optionsGroup[index].currentQuestion);
            promptLocalizationRoutine = null;
        }

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
