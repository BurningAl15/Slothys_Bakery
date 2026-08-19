using System.Collections;
using DG.Tweening;
using UnityEngine;
using INART.Framework.Core;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Handles the character's entrance and exit animations.
    ///
    /// RESPONSIBILITIES (this component only):
    ///   - Slide the character RectTransform in and out using DOTween.
    ///   - Respect SettingsManager.ReducedMotion (skip animation, snap to position).
    ///   - Support multiple entry directions via CanvasAnimationTypes enum.
    ///   - Expose SlideIn() / SlideOut() as coroutines so DialogPresenter can yield on them.
    ///
    /// NOT RESPONSIBLE FOR:
    ///   - Text, speech bubbles, or typewriting.
    ///   - Reading dialog data or knowing what scene is active.
    ///
    /// SETUP:
    ///   Add to the GameObject that holds the character RectTransform.
    ///   Assign 'character' and 'endPosition'. Leave 'autoCalculateStart' on
    ///   to derive the start position automatically from the animation type.
    /// </summary>
    public class CharacterPresenter : MonoBehaviour
    {
        // ── Enums ──────────────────────────────────────────────────────────────

        public enum EntryDirection
        {
            FromLeft,
            FromRight,
            FromBottom,
            DiagonalFromLeft,
            DiagonalFromRight
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("The character RectTransform to animate.")]
        [SerializeField] private RectTransform character;

        [Tooltip("The on-screen position the character moves to (and rests at).")]
        [SerializeField] private Vector3 endPosition;

        [Tooltip("If true, the start position is derived from EntryDirection automatically.\n" +
                 "Disable to set a custom start position below.")]
        [SerializeField] private bool autoCalculateStart = true;

        [Tooltip("Manual start position (only used when autoCalculateStart is false).")]
        [SerializeField] private Vector3 customStartPosition;

        [Tooltip("Direction the character enters from.")]
        [SerializeField] private EntryDirection entryDirection = EntryDirection.FromLeft;

        [Tooltip("How long the slide animation takes in seconds.")]
        [SerializeField] private float moveDuration = 0.5f;

        [Tooltip("Animation curve applied to the slide movement.")]
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ── Runtime ────────────────────────────────────────────────────────────

        private Vector3 _startPosition;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _startPosition = autoCalculateStart
                ? CalculateStartPosition()
                : customStartPosition;

            // Start hidden
            if (character != null)
                character.localPosition = _startPosition;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Slides the character from the start position to <see cref="endPosition"/>.
        /// Snaps instantly when ReducedMotion is active.
        /// </summary>
        public IEnumerator SlideIn()
        {
            if (character == null) yield break;

            character.localPosition = _startPosition;

            if (IsReducedMotion())
            {
                character.localPosition = endPosition;
                yield break;
            }

            yield return character
                .DOLocalMove(endPosition, moveDuration)
                .SetEase(moveCurve)
                .WaitForCompletion();
        }

        /// <summary>
        /// Slides the character from <see cref="endPosition"/> back to start.
        /// Snaps instantly when ReducedMotion is active.
        /// </summary>
        public IEnumerator SlideOut()
        {
            if (character == null) yield break;

            if (IsReducedMotion())
            {
                character.localPosition = _startPosition;
                yield break;
            }

            yield return character
                .DOLocalMove(_startPosition, moveDuration)
                .SetEase(moveCurve)
                .WaitForCompletion();
        }

        /// <summary>
        /// Snaps the character to the end position without animation.
        /// Useful for scene initialisation when the character should start visible.
        /// </summary>
        public void SnapToEnd() =>
            character.localPosition = endPosition;

        /// <summary>
        /// Snaps the character to the start (hidden) position without animation.
        /// </summary>
        public void SnapToStart() =>
            character.localPosition = _startPosition;

        // ── Helpers ────────────────────────────────────────────────────────────

        private Vector3 CalculateStartPosition() => entryDirection switch
        {
            EntryDirection.FromLeft          => new Vector3(-1000f,      endPosition.y, endPosition.z),
            EntryDirection.FromRight         => new Vector3( 1000f,      endPosition.y, endPosition.z),
            EntryDirection.FromBottom        => new Vector3(endPosition.x, -800f,        endPosition.z),
            EntryDirection.DiagonalFromLeft  => new Vector3(-1000f,      -800f,         endPosition.z),
            EntryDirection.DiagonalFromRight => new Vector3( 1000f,      -800f,         endPosition.z),
            _                                => endPosition
        };

        private static bool IsReducedMotion() =>
            SettingsManager.Instance != null && SettingsManager.Instance.ReducedMotion;

#if UNITY_EDITOR
        [ContextMenu("Debug: Slide In")]
        private void DebugSlideIn() => StartCoroutine(SlideIn());

        [ContextMenu("Debug: Slide Out")]
        private void DebugSlideOut() => StartCoroutine(SlideOut());

        [ContextMenu("Debug: Snap to End")]
        private void DebugSnapEnd() => SnapToEnd();

        [ContextMenu("Debug: Snap to Start")]
        private void DebugSnapStart() => SnapToStart();

        private void OnValidate()
        {
            if (character == null)
                Debug.LogWarning("[CharacterPresenter] 'character' RectTransform is not assigned.", this);
        }
#endif
    }
}
