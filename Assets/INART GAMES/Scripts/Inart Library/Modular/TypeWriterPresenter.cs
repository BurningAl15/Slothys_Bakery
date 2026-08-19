using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Handles the typewriter effect for speech-bubble text.
    ///
    /// RESPONSIBILITIES (this component only):
    ///   - Receive a List&lt;string&gt; of phrases and type them out letter by letter.
    ///   - Wait for player tap/click/space to advance to the next phrase.
    ///   - Drive the Animator "Talk" bool and the "waiting" indicator.
    ///   - Fire OnAllPhrasesComplete when the last phrase is dismissed.
    ///
    /// NOT RESPONSIBLE FOR:
    ///   - Knowing where the phrases come from (DialogSO, LocalizedDialogSO, hardcoded).
    ///   - Moving the character or fading the speech bubble.
    ///   - Reading SettingsManager or any game state.
    ///
    /// SETUP:
    ///   Add to the same GameObject as your speech bubble canvas group or anywhere
    ///   that has access to the TMP text field.
    ///   DialogPresenter drives this component — you don't call it directly.
    /// </summary>
    public class TypeWriterPresenter : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("The TextMeshPro field where text is typed out.")]
        [SerializeField] private TextMeshProUGUI speechText;

        [Tooltip("Animator on the character. Must have a bool parameter named 'Talk'.")]
        [SerializeField] private Animator characterAnimator;

        [Tooltip("GameObject shown while waiting for player to advance (e.g. blinking arrow).")]
        [SerializeField] private GameObject waitingIndicator;

        [Header("Timings")]
        [Tooltip("Seconds between each character typed.")]
        [SerializeField] private float secondsPerChar = 0.04f;

        [Tooltip("Seconds to pause after the last phrase before signaling completion.")]
        [SerializeField] private float pauseAfterLast = 0.5f;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the player has advanced through all phrases and the final
        /// pause has elapsed. DialogPresenter listens to this to continue the sequence.
        /// </summary>
        public event Action OnAllPhrasesComplete;

        // ── Runtime ────────────────────────────────────────────────────────────

        private static readonly int TalkHash = Animator.StringToHash("Talk");
        private Coroutine _activeCoroutine;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Starts typing out <paramref name="phrases"/>.
        /// Fires <see cref="OnAllPhrasesComplete"/> when done.
        /// Safe to call while a previous typewrite is still running — cancels it first.
        /// </summary>
        public void Play(List<string> phrases)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(TypeWriteRoutine(phrases));
        }

        /// <summary>
        /// Stops any running typewrite and clears the text field.
        /// </summary>
        public void Stop()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            ClearText();
            SetWaiting(false);
            SetTalking(false);
        }

        // ── Coroutine ──────────────────────────────────────────────────────────

        private IEnumerator TypeWriteRoutine(List<string> phrases)
        {
            if (phrases == null || phrases.Count == 0)
            {
                OnAllPhrasesComplete?.Invoke();
                yield break;
            }

            ClearText();

            for (int wordIndex = 0; wordIndex < phrases.Count; wordIndex++)
            {
                string phrase = phrases[wordIndex];
                SetTalking(true);

                // Type out each character
                string displayed = "";
                for (int i = 0; i < phrase.Length; i++)
                {
                    displayed       += phrase[i];
                    speechText.text  = displayed;
                    yield return new WaitForSeconds(secondsPerChar);
                }

                // Phrase fully typed — show waiting indicator
                SetTalking(false);
                SetWaiting(true);

                // Wait for any tap / click / space
                yield return new WaitUntil(
                    () => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

                SetWaiting(false);
                ClearText();
            }

            SetTalking(false);
            yield return new WaitForSeconds(pauseAfterLast);

            _activeCoroutine = null;
            OnAllPhrasesComplete?.Invoke();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void ClearText()
        {
            if (speechText != null) speechText.text = "";
        }

        private void SetTalking(bool value)
        {
            if (characterAnimator != null)
                characterAnimator.SetBool(TalkHash, value);
        }

        private void SetWaiting(bool value)
        {
            if (waitingIndicator != null)
                waitingIndicator.SetActive(value);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Play test phrases")]
        private void DebugPlay()
        {
            Play(new List<string>
            {
                "Hello! This is phrase one.",
                "And this is phrase two.",
                "Done!"
            });
        }
#endif
    }
}
