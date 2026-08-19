using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using INART.Framework.Core;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Orchestrates a full character dialog sequence.
    ///
    /// This replaces CanvasAnimator.CanvasAnimation() in new scenes.
    /// Legacy scenes (2.Minigame1, 3.Minigame2, 4.Minigame3) keep using
    /// CanvasAnimator untouched — zero breaking changes.
    ///
    /// SEQUENCE (mirrors the original CanvasAnimator flow):
    ///   1. CharacterPresenter.SlideIn()
    ///   2. SpeechBubblePresenter.FadeIn()
    ///   3. Resolve phrases from LocalizedDialogSO (async — waits for localization)
    ///   4. TypeWriterPresenter.Play(phrases)  ← waits for OnAllPhrasesComplete
    ///   5. SpeechBubblePresenter.FadeOut()
    ///   6. CharacterPresenter.SlideOut()
    ///   7. Fire OnSequenceComplete
    ///
    /// SETUP:
    ///   Add this component to the same GameObject as your character / speech bubble,
    ///   or to a dedicated "Dialog" GameObject in the scene.
    ///   Wire up CharacterPresenter, SpeechBubblePresenter, TypeWriterPresenter
    ///   in the Inspector.
    ///
    /// USAGE FROM GameManager (drop-in for CanvasAnimation):
    /// <code>
    ///   // Old (legacy):
    ///   yield return StartCoroutine(CanvasAnimator.Instance.CanvasAnimation(0.6f, PhraseType.INTRO));
    ///
    ///   // New (modular):
    ///   yield return StartCoroutine(dialogPresenter.PlayDialog(introDialog));
    /// </code>
    /// </summary>
    public class DialogPresenter : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Sub-Presenters")]
        [Tooltip("Handles character slide-in / slide-out.")]
        [SerializeField] private CharacterPresenter characterPresenter;

        [Tooltip("Handles speech bubble fade-in / fade-out.")]
        [SerializeField] private SpeechBubblePresenter speechBubblePresenter;

        [Tooltip("Handles typewriter text output.")]
        [SerializeField] private TypeWriterPresenter typeWriterPresenter;

        [Header("Optional — Instructions Panel")]
        [Tooltip("When assigned, the inner speech bubble group is hidden before this panel\n" +
                 "is shown (replicates the Minigame1 explanation flow).\n" +
                 "Leave empty if this scene has no explanation panel.")]
        [SerializeField] private ExplanationPanelPresenter explanationPanel;

        [Tooltip("If true, the explanation panel is shown during INTRO dialogs only.\n" +
                 "Has no effect when explanationPanel is null.")]
        [SerializeField] private bool showExplanationOnIntro = true;

        [Header("Timing")]
        [Tooltip("Seconds between the bubble fading out and the character sliding out.")]
        [SerializeField] private float pauseAfterBubble = 0.5f;

        // ── Runtime ────────────────────────────────────────────────────────────

        private bool _typewriterDone;
        private Coroutine _activeSequence;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a full dialog sequence using the phrases from
        /// <paramref name="dialogSO"/>, resolved in the active locale.
        ///
        /// Coroutine — yield on this from GameManager:
        /// <code>yield return StartCoroutine(dialogPresenter.PlayDialog(introDialog));</code>
        /// </summary>
        public IEnumerator PlayDialog(LocalizedDialogSO dialogSO, bool isIntro = false)
        {
            if (dialogSO == null)
            {
                GameLogger.Warning("DialogPresenter.PlayDialog: dialogSO is null — skipping.",
                    LogCategory.UI);
                yield break;
            }

            // Cancel any running sequence
            if (_activeSequence != null)
                StopCoroutine(_activeSequence);

            _activeSequence = StartCoroutine(RunSequence(dialogSO, isIntro));
            yield return _activeSequence;
        }

        /// <summary>
        /// Plays a dialog from a plain List&lt;string&gt; instead of a SO.
        /// Useful for testing or for strings that aren't in a localization table yet.
        /// </summary>
        public IEnumerator PlayDialog(List<string> phrases, bool isIntro = false)
        {
            if (phrases == null || phrases.Count == 0)
            {
                GameLogger.Warning("DialogPresenter.PlayDialog: phrases list is null/empty — skipping.",
                    LogCategory.UI);
                yield break;
            }

            if (_activeSequence != null)
                StopCoroutine(_activeSequence);

            _activeSequence = StartCoroutine(RunSequenceFromList(phrases, isIntro));
            yield return _activeSequence;
        }

        /// <summary>
        /// Stops any running dialog immediately and resets all sub-presenters.
        /// </summary>
        public void ForceStop()
        {
            if (_activeSequence != null)
            {
                StopCoroutine(_activeSequence);
                _activeSequence = null;
            }

            typeWriterPresenter?.Stop();
            speechBubblePresenter?.SnapHidden();
            characterPresenter?.SnapToStart();
        }

        // ── Sequence coroutines ────────────────────────────────────────────────

        private IEnumerator RunSequence(LocalizedDialogSO dialogSO, bool isIntro)
        {
            // Resolve phrases async — safe regardless of preload state
            List<string> phrases = null;
            yield return StartCoroutine(
                dialogSO.GetPhrasesAsync(list => phrases = list, this));

            yield return StartCoroutine(RunSequenceFromList(phrases, isIntro));
        }

        private IEnumerator RunSequenceFromList(List<string> phrases, bool isIntro)
        {
            // ── 1. Slide character in ──────────────────────────────────────────
            if (characterPresenter != null)
                yield return StartCoroutine(characterPresenter.SlideIn());

            // ── 2. Fade bubble in ──────────────────────────────────────────────
            if (speechBubblePresenter != null)
                yield return StartCoroutine(speechBubblePresenter.FadeIn());

            // ── 3. TypeWrite phrases ───────────────────────────────────────────
            _typewriterDone = false;

            if (typeWriterPresenter != null)
            {
                typeWriterPresenter.OnAllPhrasesComplete += OnTypewriterDone;
                typeWriterPresenter.Play(phrases);
                yield return new WaitUntil(() => _typewriterDone);
                typeWriterPresenter.OnAllPhrasesComplete -= OnTypewriterDone;
            }

            // ── 4. Optional: explanation panel (Minigame1-style) ──────────────
            bool showPanel = isIntro && showExplanationOnIntro && explanationPanel != null;
            if (showPanel)
            {
                // Hide inner bubble before showing explanation
                if (speechBubblePresenter != null)
                    yield return StartCoroutine(speechBubblePresenter.HideInner());

                yield return StartCoroutine(explanationPanel.Show());
                yield return new WaitUntil(() => explanationPanel.IsDismissed);
                yield return StartCoroutine(explanationPanel.Hide());
            }

            // ── 5. Fade bubble out ─────────────────────────────────────────────
            if (speechBubblePresenter != null)
            {
                // Restore inner group before fading out (mirrors legacy behaviour)
                if (showPanel)
                    yield return StartCoroutine(speechBubblePresenter.ShowInner());

                yield return StartCoroutine(speechBubblePresenter.FadeOut());
            }

            yield return new WaitForSeconds(pauseAfterBubble);

            // ── 6. Slide character out ─────────────────────────────────────────
            if (characterPresenter != null)
                yield return StartCoroutine(characterPresenter.SlideOut());

            _activeSequence = null;
        }

        private void OnTypewriterDone() => _typewriterDone = true;

#if UNITY_EDITOR
        [ContextMenu("Debug: Play with test phrases")]
        private void DebugPlay()
        {
            StartCoroutine(PlayDialog(new List<string>
            {
                "Hello! I'm Slothy.",
                "Welcome to the modular dialog system.",
                "This replaces the old CanvasAnimator."
            }));
        }

        [ContextMenu("Debug: Force Stop")]
        private void DebugStop() => ForceStop();

        private void OnValidate()
        {
            if (characterPresenter == null)
                Debug.LogWarning("[DialogPresenter] 'characterPresenter' is not assigned.", this);
            if (speechBubblePresenter == null)
                Debug.LogWarning("[DialogPresenter] 'speechBubblePresenter' is not assigned.", this);
            if (typeWriterPresenter == null)
                Debug.LogWarning("[DialogPresenter] 'typeWriterPresenter' is not assigned.", this);
        }
#endif
    }
}
