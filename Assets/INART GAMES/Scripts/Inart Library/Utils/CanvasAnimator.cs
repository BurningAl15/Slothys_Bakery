using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace INART.SlothyBakery.CanvasAnimator
{
    using Core;
    using SceneTransition; // SceneUtils

    /// <summary>
    /// Manages the character/speech-bubble intro sequence for each minigame scene.
    ///
    /// Migration notes:
    ///   - All Lerp / alpha manual loops replaced with DOTween.
    ///   - TypeWrite remains a coroutine (text logic, not animation).
    ///   - All public method signatures are unchanged so GameManager compiles as-is.
    ///   - ReducedMotion support: when SettingsManager.Instance.ReducedMotion is true
    ///     the character animation is skipped and we jump straight to typewrite.
    /// </summary>
    public class CanvasAnimator : Singleton<CanvasAnimator>
    {
        // ── Enums ──────────────────────────────────────────────────────────────

        public enum CanvasAnimationTypes
        {
            Horizontal_LeftRight,
            Horizontal_RightLeft,
            Vertical_BottomTop,
            Diagonally_LeftRight,
            Diagonally_RightLeft
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Animation Type")]
        public CanvasAnimationTypes canvasAnimationType;

        [Header("Character")]
        [SerializeField] private RectTransform  character;
        [SerializeField] private Animator        characterAnimator;

        [Header("Speech Bubble")]
        [SerializeField] private CanvasGroup     speechBubbleCanvasGroup;
        [SerializeField] private CanvasGroup     speechBubbleGroup;
        [SerializeField] private TextMeshProUGUI speechText;
        [SerializeField] private Animator        waitingImage;
        [SerializeField] private Color           fontColor;

        [Header("Positions")]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition;
        [SerializeField] private bool    autoCalculateStartPosition;

        [Header("Dialogs")]
        [SerializeField] private DialogSO _introDialogSo;
        [SerializeField] private DialogSO _midDialogSo;
        [SerializeField] private DialogSO _endDialogSo;

        [Header("Instructions")]
        [SerializeField] private RectTransform instructions;
        [SerializeField] private CanvasGroup    _questionButton;

        [Header("Timings")]
        [SerializeField] private float charMoveDuration  = 0.5f;
        [SerializeField] private float bubbleFadeDuration = 0.3f;
        [SerializeField] private float instrFadeDuration  = 0.3f;
        [SerializeField] private AnimationCurve _animationCurve;

        [Header("Debug")]
        [SerializeField] private TextMeshProUGUI testingText;

        // ── Runtime ────────────────────────────────────────────────────────────

        private bool _isTesting;
        private bool _finishInstructions;
        private Vector3 _dynamicStartPosition;

        private static readonly int TalkHash = Animator.StringToHash("Talk");

        protected override bool PersistAcrossScenes => false;

        // ── Singleton lifecycle ────────────────────────────────────────────────

        protected override void OnAwake() => Initialize();

        private void Initialize()
        {
            if (autoCalculateStartPosition)
                _dynamicStartPosition = canvasAnimationType switch
                {
                    CanvasAnimationTypes.Vertical_BottomTop    => new Vector3(endPosition.x, -800f, endPosition.z),
                    CanvasAnimationTypes.Horizontal_LeftRight  => new Vector3(-1000f, endPosition.y, endPosition.z),
                    CanvasAnimationTypes.Horizontal_RightLeft  => new Vector3( 1000f, endPosition.y, endPosition.z),
                    CanvasAnimationTypes.Diagonally_LeftRight  => new Vector3(-1000f, -800f, endPosition.z),
                    CanvasAnimationTypes.Diagonally_RightLeft  => new Vector3( 1000f, -800f, endPosition.z),
                    _                                           => endPosition
                };

            character.localPosition        = startPosition;
            speechBubbleCanvasGroup.alpha  = 0f;
            if (speechBubbleGroup != null) speechBubbleGroup.alpha = 1f;
            speechText.text                = "";
            speechText.color               = new Color(fontColor.r, fontColor.g, fontColor.b, 0f);
            waitingImage.gameObject.SetActive(false);

            var instrCg = instructions.GetComponent<CanvasGroup>();
            instrCg.alpha = 0f;

            if (_questionButton != null)
            {
                _questionButton.alpha          = 0f;
                _questionButton.interactable   = false;
                _questionButton.blocksRaycasts = false;
            }
        }

        // ── Public API — signatures unchanged ─────────────────────────────────

        public void Test(bool isTesting)
        {
            _isTesting = isTesting;
            if (testingText != null) testingText.gameObject.SetActive(isTesting);
        }

        /// <summary>
        /// Runs the character intro: slides character in, fades speech bubble,
        /// typerites the phrase, then slides everything out.
        /// GameManager: yield return StartCoroutine(CanvasAnimation(duration, phraseType))
        /// </summary>
        public IEnumerator CanvasAnimation(float animationDuration, PhraseUtils.PhraseType phraseType)
        {
            if (_isTesting) yield break;

            // Check reduced-motion accessibility setting
            bool reducedMotion = INART.Framework.Core.SettingsManager.Instance != null
                                 && INART.Framework.Core.SettingsManager.Instance.ReducedMotion;

            Vector3 tempStart = autoCalculateStartPosition ? _dynamicStartPosition : startPosition;
            character.localPosition = tempStart;
            speechText.text         = "";

            // ── 1. Slide character in ──────────────────────────────────────────
            if (!reducedMotion)
            {
                yield return character
                    .DOLocalMove(endPosition, charMoveDuration)
                    .SetEase(_animationCurve)
                    .WaitForCompletion();
            }
            else
            {
                character.localPosition = endPosition;
            }

            // ── 2. Fade speech bubble and text in ─────────────────────────────
            var fadeIn = DOTween.Sequence()
                .Append(speechBubbleCanvasGroup.DOFade(1f, bubbleFadeDuration).SetEase(Ease.OutQuad))
                .Join(speechText.DOColor(new Color(fontColor.r, fontColor.g, fontColor.b, 1f),
                                         bubbleFadeDuration).SetEase(Ease.OutQuad));
            yield return fadeIn.WaitForCompletion();

            // ── 3. TypeWrite the phrase ────────────────────────────────────────
            yield return TypeWrite(phraseType);

            // ── 4. First-level-only: hide speech bubble group before showing explanation ──
            if (SceneUtils.IsCurrentSceneFirstLevel() && speechBubbleGroup != null)
            {
                yield return speechBubbleGroup
                    .DOFade(0f, bubbleFadeDuration).SetEase(Ease.InQuad)
                    .WaitForCompletion();

                if (phraseType == PhraseUtils.PhraseType.INTRO)
                {
                    yield return StartCoroutine(ExplanationManager.Instance.Instruction());
                    yield return new WaitUntil(() => _finishInstructions);
                }
            }

            // ── 5. Fade speech bubble and text out ────────────────────────────
            var fadeOut = DOTween.Sequence()
                .Append(speechBubbleCanvasGroup.DOFade(0f, bubbleFadeDuration).SetEase(Ease.InQuad))
                .Join(speechText.DOColor(new Color(fontColor.r, fontColor.g, fontColor.b, 0f),
                                          bubbleFadeDuration).SetEase(Ease.InQuad));
            yield return fadeOut.WaitForCompletion();

            // ── 6. First-level cleanup ─────────────────────────────────────────
            if (SceneUtils.IsCurrentSceneFirstLevel() && phraseType == PhraseUtils.PhraseType.INTRO)
                ExplanationManager.Instance.TurnOff();

            yield return new WaitForSeconds(PhraseUtils.waitUntilPrevious);

            // ── 7. Slide character out ─────────────────────────────────────────
            if (!reducedMotion)
            {
                yield return character
                    .DOLocalMove(tempStart, charMoveDuration)
                    .SetEase(_animationCurve)
                    .WaitForCompletion();
            }
            else
            {
                character.localPosition = tempStart;
            }

            if (speechBubbleGroup != null) speechBubbleGroup.alpha = 1f;
        }

        /// <summary>
        /// Fades the instructions panel (and optionally the question button) in or out.
        /// GameManager: yield return StartCoroutine(InstructionsAnimation(...))
        /// </summary>
        public IEnumerator InstructionsAnimation(bool show,
            bool turnOnInstructions  = true,
            bool turnOnQuestionButton = true)
        {
            float target   = show ? 1f : 0f;
            var   instrCg  = instructions.GetComponent<CanvasGroup>();

            var seq = DOTween.Sequence();

            if (turnOnInstructions)
                seq.Append(instrCg.DOFade(target, instrFadeDuration).SetEase(Ease.OutQuad));

            if (_questionButton != null && turnOnQuestionButton)
            {
                if (seq.IsActive())
                    seq.Join(_questionButton.DOFade(target, instrFadeDuration).SetEase(Ease.OutQuad));
                else
                    seq.Append(_questionButton.DOFade(target, instrFadeDuration).SetEase(Ease.OutQuad));
            }

            yield return seq.WaitForCompletion();
        }

        /// <summary>
        /// Fades the question button in, then enables interaction.
        /// GameManager: yield return StartCoroutine(EnableTurnButton(true))
        /// </summary>
        public IEnumerator EnableTurnButton(bool activate)
        {
            if (_questionButton == null) yield break;

            float target = activate ? 1f : 0f;
            yield return _questionButton
                .DOFade(target, instrFadeDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            TurnButton(activate);
        }

        /// <summary>Enables/disables question button interaction without animation.</summary>
        public void TurnButton(bool active)
        {
            if (_questionButton == null) return;
            _questionButton.interactable   = active;
            _questionButton.blocksRaycasts = active;
        }

        /// <summary>Called by the "understood" button in ExplanationManager.</summary>
        public void TurnOnFinish()
        {
            _finishInstructions = true;
            ExplanationManager.Instance.TurnOffButton();
        }

        // ── TypeWrite — kept as coroutine (text logic, not animation) ──────────

        private IEnumerator TypeWrite(PhraseUtils.PhraseType phraseType)
        {
            List<string> phrases = phraseType switch
            {
                PhraseUtils.PhraseType.INTRO  => _introDialogSo.Phrases,
                PhraseUtils.PhraseType.CONGRAT => _midDialogSo.Phrases,
                PhraseUtils.PhraseType.END    => _endDialogSo.Phrases,
                _                              => new List<string>()
            };

            string tempText  = "";
            int    wordIndex = 0;

            while (wordIndex < phrases.Count)
            {
                characterAnimator.SetBool(TalkHash, true);

                // Type out each character
                for (int i = 0; i < phrases[wordIndex].Length; i++)
                {
                    tempText        += phrases[wordIndex][i];
                    speechText.text  = tempText;
                    yield return new WaitForSeconds(PhraseUtils.waitUntilNextLetter);
                }

                // Wait for tap/click to advance
                characterAnimator.SetBool(TalkHash, false);
                waitingImage.gameObject.SetActive(true);

                yield return new WaitUntil(
                    () => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

                waitingImage.gameObject.SetActive(false);
                wordIndex++;
                tempText = "";
            }

            characterAnimator.SetBool(TalkHash, false);
            yield return new WaitForSeconds(PhraseUtils.waitUntilPrevious);
        }
    }
}
