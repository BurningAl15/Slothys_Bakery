using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace FatCat.Feedback
{
    /// <summary>
    /// "Enter" and "Finish" of the feedback kit: a scene's interface starts off screen and comes in piece by piece
    /// (progress bar from the top, tray from the bottom, Check from the right...), then leaves the same way at the
    /// end. Works for UI (RectTransform: anchored position) and world objects (local position). One per scene;
    /// the game calls <see cref="EnterCurrent"/> when play starts and <see cref="ExitCurrent"/> when it ends.
    /// </summary>
    public sealed class UiChoreography : MonoBehaviour
    {
        public enum From { Top, Bottom, Left, Right, Pop }

        [Serializable]
        public sealed class Entry
        {
            public Transform target;
            public From from = From.Bottom;
            [Tooltip("Seconds after the previous entry starts.")]
            public float stagger = 0.1f;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [Tooltip("UI distance (canvas units) and world distance (units) the pieces travel.")]
        [SerializeField] private float uiDistance = 700f, worldDistance = 12f;
        [SerializeField] private float enterDuration = 0.6f, exitDuration = 0.4f;
        [SerializeField] private bool hideOnAwake = true;

        public static UiChoreography Current { get; private set; }
        public bool IsIn { get; private set; }
        public IReadOnlyList<Entry> Entries => entries;

        private readonly Dictionary<Transform, Vector3> homes = new Dictionary<Transform, Vector3>();

        private void Awake()
        {
            Current = this;
            foreach (var e in entries)
                if (e.target != null) homes[e.target] = Home(e.target);
            if (hideOnAwake) HideNow();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            foreach (var e in entries) if (e.target != null) e.target.DOKill();
        }

        public static IEnumerator EnterCurrent() => Current != null ? Current.Enter() : Empty();
        public static IEnumerator ExitCurrent() => Current != null ? Current.Exit() : Empty();
        static IEnumerator Empty() { yield break; }

        /// <summary>Everything off screen (no animation).</summary>
        public void HideNow()
        {
            foreach (var e in entries)
            {
                if (e.target == null) continue;
                e.target.DOKill();
                if (e.from == From.Pop) e.target.localScale = Vector3.zero;
                else Place(e.target, homes[e.target] + Offset(e));
            }
            IsIn = false;
        }

        public IEnumerator Enter()
        {
            if (IsIn) yield break;
            IsIn = true;
            float delay = 0f;
            Tween last = null;
            foreach (var e in entries)
            {
                if (e.target == null) continue;
                delay += e.stagger;
                var t = e.target;
                last = e.from == From.Pop
                    ? Juice.PopIn(t, enterDuration * 0.7f, delay)
                    : Move(t, homes[t] + Offset(e), homes[t], enterDuration, delay, true);
            }
            if (last != null) yield return last.WaitForCompletion();
        }

        public IEnumerator Exit()
        {
            if (!IsIn) yield break;
            IsIn = false;
            float delay = 0f;
            Tween last = null;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (e.target == null) continue;
                var t = e.target;
                last = e.from == From.Pop
                    ? t.DOScale(0f, exitDuration).SetEase(Ease.InBack).SetDelay(delay)
                    : Move(t, Now(t), homes[t] + Offset(e), exitDuration, delay, false);
                delay += e.stagger * 0.6f;
            }
            if (last != null) yield return last.WaitForCompletion();
        }

        // ---- helpers ----------------------------------------------------------------------------------------

        Vector3 Offset(Entry e)
        {
            bool ui = e.target is RectTransform;
            float d = ui ? uiDistance : worldDistance;
            switch (e.from)
            {
                case From.Top: return Vector3.up * d;
                case From.Bottom: return Vector3.down * d;
                case From.Left: return Vector3.left * d;
                case From.Right: return Vector3.right * d;
                default: return Vector3.zero;
            }
        }

        static Vector3 Home(Transform t) => t is RectTransform rt ? (Vector3)rt.anchoredPosition : t.localPosition;
        static Vector3 Now(Transform t) => Home(t);

        static void Place(Transform t, Vector3 p)
        {
            if (t is RectTransform rt) rt.anchoredPosition = p; else t.localPosition = p;
        }

        static Tween Move(Transform t, Vector3 from, Vector3 to, float duration, float delay, bool entering)
        {
            t.DOKill();
            Place(t, from);
            var ease = entering ? Ease.OutBack : Ease.InBack;
            return t is RectTransform rt
                ? rt.FxAnchorPos(to, duration).SetEase(ease, 1.1f).SetDelay(delay)
                : t.DOLocalMove(to, duration).SetEase(ease, 1.1f).SetDelay(delay);
        }
    }
}
