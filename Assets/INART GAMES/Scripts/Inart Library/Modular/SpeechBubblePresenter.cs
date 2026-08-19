using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Handles the speech bubble fade-in / fade-out animations.
    ///
    /// RESPONSIBILITIES (this component only):
    ///   - Fade the speech bubble CanvasGroup in and out.
    ///   - Animate the text color alpha alongside the bubble.
    ///   - Expose FadeIn() / FadeOut() as coroutines for DialogPresenter to yield on.
    ///   - Hide/show the inner group separately when needed (e.g. Minigame1 shows
    ///     an explanation panel before the bubble disappears).
    ///
    /// NOT RESPONSIBLE FOR:
    ///   - Text content or typewriting.
    ///   - Character movement.
    ///   - Any game logic.
    ///
    /// SETUP:
    ///   Add to the speech bubble root GameObject.
    ///   Assign 'bubbleCanvasGroup' (the outer group that controls overall visibility),
    ///   'innerCanvasGroup' (optional — an inner group shown/hidden independently),
    ///   and 'speechText' (the TMP graphic whose color alpha is animated with the bubble).
    /// </summary>
    public class SpeechBubblePresenter : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("Outer CanvasGroup controlling the entire speech bubble visibility.")]
        [SerializeField] private CanvasGroup bubbleCanvasGroup;

        [Tooltip("Optional inner CanvasGroup (e.g. contains the text + arrow).\n" +
                 "Used when the outer group needs to fade independently of the inner content.")]
        [SerializeField] private CanvasGroup innerCanvasGroup;

        [Tooltip("The Graphic (usually TMP text) whose color alpha is animated alongside the bubble.\n" +
                 "Leave empty if you only need the CanvasGroup alpha.")]
        [SerializeField] private Graphic speechTextGraphic;

        [Tooltip("The opaque color of the speech text (alpha will be driven by the tween).")]
        [SerializeField] private Color textColor = Color.white;

        [Header("Animation Timings")]
        [Tooltip("How long the fade-in takes in seconds.")]
        [SerializeField] private float fadeInDuration  = 0.3f;

        [Tooltip("How long the fade-out takes in seconds.")]
        [SerializeField] private float fadeOutDuration = 0.3f;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Start fully hidden
            SetAlpha(0f);
            if (innerCanvasGroup != null) innerCanvasGroup.alpha = 1f;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Fades the speech bubble and text in.
        /// DialogPresenter yields on this before starting the typewriter.
        /// </summary>
        public IEnumerator FadeIn()
        {
            if (bubbleCanvasGroup == null) yield break;

            var seq = DOTween.Sequence()
                .Append(bubbleCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

            if (speechTextGraphic != null)
                seq.Join(speechTextGraphic.DOColor(
                    new Color(textColor.r, textColor.g, textColor.b, 1f),
                    fadeInDuration).SetEase(Ease.OutQuad));

            yield return seq.WaitForCompletion();
        }

        /// <summary>
        /// Fades the speech bubble and text out.
        /// DialogPresenter yields on this after the typewriter finishes.
        /// </summary>
        public IEnumerator FadeOut()
        {
            if (bubbleCanvasGroup == null) yield break;

            var seq = DOTween.Sequence()
                .Append(bubbleCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

            if (speechTextGraphic != null)
                seq.Join(speechTextGraphic.DOColor(
                    new Color(textColor.r, textColor.g, textColor.b, 0f),
                    fadeOutDuration).SetEase(Ease.InQuad));

            yield return seq.WaitForCompletion();
        }

        /// <summary>
        /// Hides the inner CanvasGroup (e.g. before showing an explanation panel).
        /// The outer bubble remains at whatever alpha it currently has.
        /// </summary>
        public IEnumerator HideInner()
        {
            if (innerCanvasGroup == null) yield break;
            yield return innerCanvasGroup
                .DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();
        }

        /// <summary>
        /// Shows the inner CanvasGroup again after it was hidden.
        /// </summary>
        public IEnumerator ShowInner()
        {
            if (innerCanvasGroup == null) yield break;
            yield return innerCanvasGroup
                .DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }

        /// <summary>
        /// Snaps alpha to fully visible without animation.
        /// </summary>
        public void SnapVisible()
        {
            SetAlpha(1f);
            if (innerCanvasGroup != null) innerCanvasGroup.alpha = 1f;
            if (speechTextGraphic != null)
                speechTextGraphic.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
        }

        /// <summary>
        /// Snaps alpha to fully hidden without animation.
        /// </summary>
        public void SnapHidden()
        {
            SetAlpha(0f);
            if (speechTextGraphic != null)
                speechTextGraphic.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetAlpha(float alpha)
        {
            if (bubbleCanvasGroup != null) bubbleCanvasGroup.alpha = alpha;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Fade In")]
        private void DebugFadeIn() => StartCoroutine(FadeIn());

        [ContextMenu("Debug: Fade Out")]
        private void DebugFadeOut() => StartCoroutine(FadeOut());

        [ContextMenu("Debug: Snap Visible")]
        private void DebugSnapVisible() => SnapVisible();

        [ContextMenu("Debug: Snap Hidden")]
        private void DebugSnapHidden() => SnapHidden();

        private void OnValidate()
        {
            if (bubbleCanvasGroup == null)
                Debug.LogWarning("[SpeechBubblePresenter] 'bubbleCanvasGroup' is not assigned.", this);
        }
#endif
    }
}
