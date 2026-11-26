using DG.Tweening;
using UnityEngine;

namespace FatCat.Feedback
{
    /// <summary>
    /// The feedback kit's small motions (owner-approved prototype, 2026-11-21), all DOTween so every game moves the
    /// same way: pop in/out, hop, shake, pulse, squash on landing, arc back home, slide in/out. Each returns its
    /// tween (callers can wait with <c>WaitForCompletion()</c>) and kills earlier tweens on the same target first.
    /// No game code here: the package-to-be (com.fatcat.feedback).
    /// </summary>
    public static class Juice
    {
        public const float Fast = 0.25f, Normal = 0.4f, Slow = 0.65f;

        public static Tween PopIn(Transform t, float duration = Normal, float delay = 0f)
        {
            t.DOKill(true);
            t.localScale = Vector3.zero;
            return t.DOScale(1f, duration).SetEase(Ease.OutBack).SetDelay(delay);
        }

        public static Tween PopOut(Transform t, float duration = Fast)
        {
            t.DOKill(true);
            return t.DOScale(0f, duration).SetEase(Ease.InBack);
        }

        /// <summary>A happy jump in place.</summary>
        public static Tween Hop(Transform t, float height = 0.35f, float duration = Normal)
        {
            t.DOComplete();
            return t.DOPunchPosition(Vector3.up * height, duration, 1, 0f);
        }

        /// <summary>"No": a short sideways shake.</summary>
        public static Tween Shake(Transform t, float strength = 0.15f, float duration = 0.35f)
        {
            t.DOComplete();
            return t.DOShakePosition(duration, new Vector3(strength, 0f, 0f), 18, 0f, false, true);
        }

        public static Tween Shake(RectTransform t, float strength = 12f, float duration = 0.35f)
        {
            t.DOComplete();
            return t.FxShakeAnchorPos(duration, new Vector2(strength, 0f), 18, 0f, true);
        }

        /// <summary>"Look here": grows a little and back, <paramref name="times"/> times.</summary>
        public static Tween Pulse(Transform t, float amount = 0.08f, int times = 2, float duration = 0.5f)
        {
            t.DOComplete();
            return t.DOPunchScale(Vector3.one * amount, duration, times * 2, 0.5f);
        }

        /// <summary>Lands: squash, stretch, settle.</summary>
        public static Tween Squash(Transform t, float duration = 0.38f)
        {
            t.DOComplete();
            var rest = t.localScale;
            return DOTween.Sequence()
                .Append(t.DOScale(new Vector3(rest.x * 1.2f, rest.y * 0.8f, rest.z), duration * 0.3f).SetEase(Ease.OutQuad))
                .Append(t.DOScale(new Vector3(rest.x * 0.94f, rest.y * 1.08f, rest.z), duration * 0.3f).SetEase(Ease.InOutQuad))
                .Append(t.DOScale(rest, duration * 0.4f).SetEase(Ease.OutBack))
                .SetTarget(t);
        }

        /// <summary>Goes back home on an arc (a wrong answer returns the piece).</summary>
        public static Tween ArcTo(Transform t, Vector3 worldTarget, float height = 0.8f, float duration = 0.5f)
        {
            t.DOKill();
            return t.DOJump(worldTarget, height, 1, duration).SetEase(Ease.OutQuad);
        }

        /// <summary>Enters from an offset to where it is now (local space). Returns the tween; the object is moved
        /// to the offset immediately.</summary>
        public static Tween SlideInLocal(Transform t, Vector3 home, Vector3 offset, float duration = Slow, float delay = 0f)
        {
            t.DOKill();
            t.localPosition = home + offset;
            return t.DOLocalMove(home, duration).SetEase(Ease.OutBack, 1.1f).SetDelay(delay);
        }

        public static Tween SlideOutLocal(Transform t, Vector3 home, Vector3 offset, float duration = Normal, float delay = 0f)
        {
            t.DOKill();
            return t.DOLocalMove(home + offset, duration).SetEase(Ease.InBack, 1.1f).SetDelay(delay);
        }

        public static Tween SlideIn(RectTransform t, Vector2 home, Vector2 offset, float duration = Slow, float delay = 0f)
        {
            t.DOKill();
            t.anchoredPosition = home + offset;
            return t.FxAnchorPos(home, duration).SetEase(Ease.OutBack, 1.1f).SetDelay(delay);
        }

        public static Tween SlideOut(RectTransform t, Vector2 home, Vector2 offset, float duration = Normal, float delay = 0f)
        {
            t.DOKill();
            return t.FxAnchorPos(home + offset, duration).SetEase(Ease.InBack, 1.1f).SetDelay(delay);
        }
    }
}
