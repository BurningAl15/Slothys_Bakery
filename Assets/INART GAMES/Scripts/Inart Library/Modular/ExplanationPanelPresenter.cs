using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Manages the optional "explanation / instruction" panel that appears
    /// mid-dialog in Minigame1 (the panel that shows before the player starts).
    ///
    /// DialogPresenter calls Show() → waits for IsDismissed → calls Hide().
    ///
    /// SETUP:
    ///   Add to the explanation panel GameObject.
    ///   Assign 'panelCanvasGroup'.
    ///   The "I understood" button should call Dismiss() on this component.
    ///
    ///   Wire this into DialogPresenter.explanationPanel in the Inspector
    ///   (leave empty on scenes that have no explanation panel).
    /// </summary>
    public class ExplanationPanelPresenter : MonoBehaviour
    {
        [Tooltip("CanvasGroup of the explanation panel.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Tooltip("How long the fade-in / fade-out takes.")]
        [SerializeField] private float fadeDuration = 0.3f;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>
        /// True once the player has tapped the dismiss button.
        /// DialogPresenter polls this via WaitUntil.
        /// </summary>
        public bool IsDismissed { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha          = 0f;
                panelCanvasGroup.interactable   = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Fades the panel in and enables interaction.</summary>
        public IEnumerator Show()
        {
            IsDismissed = false;

            if (panelCanvasGroup == null) yield break;

            yield return panelCanvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            panelCanvasGroup.interactable   = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        /// <summary>Disables interaction and fades the panel out.</summary>
        public IEnumerator Hide()
        {
            if (panelCanvasGroup == null) yield break;

            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;

            yield return panelCanvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();
        }

        /// <summary>
        /// Call from the "I understood" / "OK" button in the explanation panel.
        /// Sets IsDismissed = true so DialogPresenter can continue the sequence.
        /// </summary>
        public void Dismiss()
        {
            IsDismissed = true;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Show Panel")]
        private void DebugShow() => StartCoroutine(Show());

        [ContextMenu("Debug: Hide Panel")]
        private void DebugHide() => StartCoroutine(Hide());

        [ContextMenu("Debug: Simulate Dismiss")]
        private void DebugDismiss() => Dismiss();
#endif
    }
}
