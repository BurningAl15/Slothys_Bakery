using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Feedback
{
    /// <summary>
    /// The few UI tweens the kit needs, built on DOTween's core API (DOTween.To / ToAlpha / Shake). DOTween's
    /// own UI shortcuts live in its Modules folder, which compiles into the game's firstpass assembly that a
    /// package cannot reference. Same behaviour as the module versions, including the target, so DOKill and
    /// DOComplete on the object still stop them.
    /// </summary>
    internal static class FeedbackTweens
    {
        public static TweenerCore<float, float, FloatOptions> FxFade(this CanvasGroup target, float endValue, float duration)
        {
            var t = DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static TweenerCore<Color, Color, ColorOptions> FxFade(this Graphic target, float endValue, float duration)
        {
            var t = DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static TweenerCore<Vector2, Vector2, VectorOptions> FxAnchorPos(this RectTransform target, Vector2 endValue, float duration)
        {
            var t = DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        public static Tweener FxShakeAnchorPos(this RectTransform target, float duration, Vector2 strength, int vibrato, float randomness, bool fadeOut)
        {
            Vector2 start = target.anchoredPosition;
            return DOTween.Shake(() => (Vector3)(target.anchoredPosition - start), x => target.anchoredPosition = start + (Vector2)x,
                    duration, new Vector3(strength.x, strength.y, 0f), vibrato, randomness, fadeOut)
                .SetTarget(target);
        }
    }
}
