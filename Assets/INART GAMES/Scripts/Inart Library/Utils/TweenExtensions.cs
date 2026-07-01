using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace INART.Framework.Utils
{
    /// <summary>
    /// Extension methods and static helpers that wrap common DOTween patterns.
    /// Import this namespace in any file that needs animations:
    ///   using INART.Framework.Utils;
    ///
    /// All methods return the Tween so callers can chain .OnComplete() or yield on it.
    /// </summary>
    public static class TweenExtensions
    {
        // ── Default durations ──────────────────────────────────────────────────
        public const float FADE_DURATION  = 0.35f;
        public const float MOVE_DURATION  = 0.5f;
        public const float SCALE_DURATION = 0.3f;

        // ─────────────────────────────────────────────────────────────────────
        // CANVAS GROUP
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Fades a CanvasGroup alpha from current value to 1 and optionally enables interaction.</summary>
        public static Tween FadeIn(this CanvasGroup cg, float duration = FADE_DURATION,
            bool enableInteraction = false, Ease ease = Ease.OutQuad)
        {
            cg.gameObject.SetActive(true);
            return cg.DOFade(1f, duration)
                     .SetEase(ease)
                     .OnComplete(() =>
                     {
                         if (enableInteraction)
                         {
                             cg.interactable      = true;
                             cg.blocksRaycasts    = true;
                         }
                     });
        }

        /// <summary>Fades a CanvasGroup alpha to 0 and optionally disables interaction.</summary>
        public static Tween FadeOut(this CanvasGroup cg, float duration = FADE_DURATION,
            bool disableInteraction = true, Ease ease = Ease.InQuad)
        {
            if (disableInteraction)
            {
                cg.interactable   = false;
                cg.blocksRaycasts = false;
            }
            return cg.DOFade(0f, duration).SetEase(ease);
        }

        /// <summary>Sets alpha instantly without animation (helper for initialisation).</summary>
        public static void SetAlpha(this CanvasGroup cg, float alpha, bool interactable = false)
        {
            cg.alpha          = alpha;
            cg.interactable   = interactable;
            cg.blocksRaycasts = interactable;
        }

        // ─────────────────────────────────────────────────────────────────────
        // RECT TRANSFORM — position
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Moves a RectTransform to a local position.</summary>
        public static Tween MoveToLocal(this RectTransform rt, Vector3 target,
            float duration = MOVE_DURATION, Ease ease = Ease.OutCubic)
            => rt.DOLocalMove(target, duration).SetEase(ease);

        /// <summary>Moves a RectTransform to a local position using an AnimationCurve.</summary>
        public static Tween MoveToLocal(this RectTransform rt, Vector3 target,
            float duration, AnimationCurve curve)
            => rt.DOLocalMove(target, duration).SetEase(curve);

        // ─────────────────────────────────────────────────────────────────────
        // TRANSFORM — scale
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Scales a Transform to a target uniform scale.</summary>
        public static Tween ScaleTo(this Transform t, float target,
            float duration = SCALE_DURATION, Ease ease = Ease.OutBack)
            => t.DOScale(target, duration).SetEase(ease);

        /// <summary>
        /// Punch-scale pop: grows to maxScale then springs back to restScale.
        /// Great for feedback messages (correct / wrong indicators).
        /// </summary>
        public static Sequence ScalePop(this Transform t, float maxScale, float restScale,
            float growDuration = 0.1f, float shrinkDuration = 0.15f,
            Ease growEase = Ease.OutQuad, Ease shrinkEase = Ease.InBack)
        {
            return DOTween.Sequence()
                .Append(t.DOScale(maxScale, growDuration).SetEase(growEase))
                .Append(t.DOScale(restScale, shrinkDuration).SetEase(shrinkEase));
        }

        // ─────────────────────────────────────────────────────────────────────
        // IMAGE — fill
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Animates Image.fillAmount to a target value.</summary>
        public static Tween FillTo(this Image img, float target,
            float duration = MOVE_DURATION, Ease ease = Ease.OutQuad)
            => img.DOFillAmount(target, duration).SetEase(ease);

        // ─────────────────────────────────────────────────────────────────────
        // COLOR
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tweens a Graphic (Image, Text, TMP) color.</summary>
        public static Tween ColorTo(this Graphic g, Color target,
            float duration = FADE_DURATION, Ease ease = Ease.Linear)
            => g.DOColor(target, duration).SetEase(ease);

        // ─────────────────────────────────────────────────────────────────────
        // YIELD HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs a tween and waits for it to finish inside a coroutine.
        /// Usage: yield return tween.WaitForCompletion();
        /// (This is just a readability alias — DOTween already supports this natively.)
        /// </summary>
        public static Coroutine PlayAndWait(this Tween tween, MonoBehaviour host)
            => host.StartCoroutine(WaitCoroutine(tween));

        private static System.Collections.IEnumerator WaitCoroutine(Tween tween)
        {
            yield return tween.WaitForCompletion();
        }
    }
}
