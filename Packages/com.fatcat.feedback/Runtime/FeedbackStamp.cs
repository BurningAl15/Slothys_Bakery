using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Feedback
{
    /// <summary>
    /// "Got it right" / "Not yet" of the feedback kit (owner-approved prototype, 2026-11-21): a stamp (✓ green or
    /// ✕ rose) that lands on the answer, a sparkle burst, a line that says why, the narrator peeking in on right
    /// answers and a streak chip every third in a row. Lives on a prefab in Resources (<see cref="ResourcePath"/>,
    /// built by the game) and is created on first use in each scene. All motion is DOTween.
    /// </summary>
    public sealed class FeedbackStamp : MonoBehaviour
    {
        public const string ResourcePath = "FatCatFeedback/FeedbackStamp";

        [Header("Stamp")]
        [SerializeField] private RectTransform badge;
        [SerializeField] private Image badgeBackground;
        [SerializeField] private Image badgeIcon;
        [SerializeField] private Sprite rightIcon, wrongIcon;
        [SerializeField] private Color rightColor = new Color32(0x5F, 0x9B, 0x62, 0xFF);
        [SerializeField] private Color wrongColor = new Color32(0xC2, 0x55, 0x4F, 0xFF);
        [Header("Message")]
        [SerializeField] private RectTransform pill;
        [SerializeField] private Image pillFrame;
        [SerializeField] private TMP_Text pillText;
        [SerializeField] private Color pillNeutral = new Color32(0x4B, 0x2A, 0x4F, 0xFF);
        [Header("Narrator peek (right answers)")]
        [SerializeField] private RectTransform peek;
        [SerializeField] private float peekRise = 170f;
        [Header("Streak")]
        [SerializeField] private RectTransform streakChip;
        [SerializeField] private TMP_Text streakText;
        [Header("Sparkles")]
        [SerializeField] private List<Image> sparkles = new List<Image>();
        [SerializeField] private Color[] sparkleColors = { new Color32(0xF2, 0xC1, 0x4E, 0xFF), Color.white, new Color32(0x6F, 0xCF, 0x6F, 0xFF), new Color32(0xCC, 0x75, 0x93, 0xFF) };
        [Header("Timing")]
        [SerializeField] private float rightHold = 1.5f, wrongHold = 2.2f;

        /// <summary>Text of the streak chip ("Streak ×3!"); the game sets it from its localization.</summary>
        public static Func<int, string> StreakLabel = n => $"×{n}!";
        /// <summary>Haptics hook (the game decides, e.g. respecting its vibration setting).</summary>
        public static Action<bool> Haptic;

        static FeedbackStamp instance;
        int streak;
        Vector2 peekHome, pillHome;
        Canvas canvas;

        public static FeedbackStamp Instance
        {
            get
            {
                if (instance != null) return instance;
                var prefab = Resources.Load<FeedbackStamp>(ResourcePath);
                if (prefab == null) return null;
                instance = Instantiate(prefab);
                instance.name = "FeedbackStamp";
                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
            canvas = GetComponentInChildren<Canvas>(true) ?? GetComponent<Canvas>();
            peekHome = peek != null ? peek.anchoredPosition : Vector2.zero;
            pillHome = pill != null ? pill.anchoredPosition : Vector2.zero;
            HideNow();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            DOTween.Kill(this);
        }

        // ---- API --------------------------------------------------------------------------------------------

        /// <summary>Right answer at <paramref name="worldPoint"/>. Returns how long it shows (the game may wait).</summary>
        public static float Right(string message, Vector3 worldPoint) => Instance != null ? Instance.Play(true, message, worldPoint) : 0f;

        /// <summary>Wrong answer: stamp and a hint, no narrator, the streak resets.</summary>
        public static float Wrong(string message, Vector3 worldPoint) => Instance != null ? Instance.Play(false, message, worldPoint) : 0f;

        /// <summary>A burst of sparkles (a piece lands, a bar fills).</summary>
        public static void Sparkle(Vector3 worldPoint, int count = 10)
        {
            if (Instance != null) Instance.Burst(Instance.ToCanvas(worldPoint), count, 1f);
        }

        /// <summary>Sparkles on a UI element (any canvas mode). Its position is not a world point when its canvas
        /// is an overlay, so it is turned into a screen point through its own canvas.</summary>
        public static void Sparkle(RectTransform uiElement, int count = 10)
        {
            if (Instance == null || uiElement == null) return;
            var c = uiElement.GetComponentInParent<Canvas>();
            var root = c != null ? c.rootCanvas : null;
            var cam = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, uiElement.position);
            Instance.Burst(Instance.ScreenToCanvas(screen), count, 1f);
        }

        public static void ResetStreak() { if (instance != null) instance.streak = 0; }

        // ---- play -------------------------------------------------------------------------------------------

        float Play(bool right, string message, Vector3 worldPoint)
        {
            var at = ToCanvas(worldPoint);
            Haptic?.Invoke(right);
            streak = right ? streak + 1 : 0;
            float hold = right ? rightHold : wrongHold;

            // Stamp: lands from big and tilted, settles, then fades.
            badge.DOKill(); badge.gameObject.SetActive(true);
            badge.anchoredPosition = at + new Vector2(48f, 48f);
            badgeBackground.color = right ? rightColor : wrongColor;
            badgeIcon.sprite = right ? rightIcon : wrongIcon;
            badge.localScale = Vector3.one * 2.2f; badge.localRotation = Quaternion.Euler(0, 0, -25f);
            var bg = badge.GetComponent<CanvasGroup>(); bg.alpha = 0f;
            DOTween.Sequence().SetTarget(this)
                .Append(bg.FxFade(1f, 0.12f))
                .Join(badge.DOScale(1f, 0.42f).SetEase(Ease.OutBack))
                .Join(badge.DOLocalRotate(Vector3.zero, 0.42f).SetEase(Ease.OutBack))
                .AppendInterval(hold - 0.6f)
                .Append(bg.FxFade(0f, 0.25f))
                .OnComplete(() => badge.gameObject.SetActive(false));

            // Message: drops in, stays, fades.
            if (!string.IsNullOrEmpty(message))
            {
                pill.DOKill(); pill.gameObject.SetActive(true);
                pillText.text = message;
                pillFrame.color = right ? rightColor : pillNeutral;
                var pg = pill.GetComponent<CanvasGroup>(); pg.alpha = 0f;
                pill.anchoredPosition = pillHome + new Vector2(0f, 40f);
                DOTween.Sequence().SetTarget(this)
                    .Append(pg.FxFade(1f, 0.2f))
                    .Join(pill.FxAnchorPos(pillHome, 0.38f).SetEase(Ease.OutBack))
                    .AppendInterval(hold)
                    .Append(pg.FxFade(0f, 0.25f))
                    .OnComplete(() => pill.gameObject.SetActive(false));
            }

            if (right)
            {
                Burst(at, 18, 1.4f);
                if (streak > 0 && streak % 3 == 0 && streakChip != null) ShowStreak(streak);
            }

            // The character peeks in both cases: cheering on a right answer; on a wrong one, only if it has a face
            // that can look worried (never a happy face over a mistake), and a little lower.
            var face = peek != null ? peek.GetComponentInChildren<IFeedbackFace>(true) : null;
            if (peek != null && (right || face != null))
            {
                face?.React(right);
                float rise = right ? peekRise : peekRise * 0.7f;
                peek.DOKill(); peek.gameObject.SetActive(true);
                peek.anchoredPosition = peekHome;
                DOTween.Sequence().SetTarget(this)
                    .Append(peek.FxAnchorPos(peekHome + Vector2.up * rise, 0.45f).SetEase(Ease.OutCubic)) // no overshoot: never past peekRise
                    .AppendInterval(hold - 0.4f)
                    .Append(peek.FxAnchorPos(peekHome, 0.3f).SetEase(Ease.InBack));
            }
            return hold + 0.3f;
        }

        void ShowStreak(int n)
        {
            streakChip.DOKill(); streakChip.gameObject.SetActive(true);
            streakText.text = StreakLabel(n);
            streakChip.localScale = Vector3.one * 0.3f;
            var g = streakChip.GetComponent<CanvasGroup>(); g.alpha = 0f;
            DOTween.Sequence().SetTarget(this)
                .Append(g.FxFade(1f, 0.15f))
                .Join(streakChip.DOScale(1f, 0.5f).SetEase(Ease.OutElastic, 1.1f, 0.5f))
                .AppendInterval(1.6f)
                .Append(g.FxFade(0f, 0.3f))
                .OnComplete(() => streakChip.gameObject.SetActive(false));
        }

        void Burst(Vector2 at, int count, float power)
        {
            int used = 0;
            foreach (var s in sparkles)
            {
                if (used >= count) break;
                if (s.gameObject.activeSelf) continue;
                used++;
                var rt = (RectTransform)s.transform;
                s.gameObject.SetActive(true);
                s.color = sparkleColors[UnityEngine.Random.Range(0, sparkleColors.Length)];
                rt.anchoredPosition = at;
                rt.localScale = Vector3.one * UnityEngine.Random.Range(0.6f, 1.2f);
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f), d = UnityEngine.Random.Range(60f, 160f) * power;
                var to = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                DOTween.Sequence().SetTarget(this)
                    .Append(rt.FxAnchorPos(to, 0.6f).SetEase(Ease.OutCubic))
                    .Join(rt.DOLocalRotate(new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f)), 0.6f, RotateMode.LocalAxisAdd))
                    .Join(s.FxFade(0f, 0.6f).SetEase(Ease.InQuad))
                    .OnComplete(() => s.gameObject.SetActive(false));
            }
        }

        void HideNow()
        {
            if (badge != null) badge.gameObject.SetActive(false);
            if (pill != null) pill.gameObject.SetActive(false);
            if (streakChip != null) streakChip.gameObject.SetActive(false);
            foreach (var s in sparkles) if (s != null) s.gameObject.SetActive(false);
        }

        Vector2 ToCanvas(Vector3 world)
        {
            var cam = Camera.main;
            var screen = cam != null ? cam.WorldToScreenPoint(world) : new Vector3(Screen.width / 2f, Screen.height / 2f);
            return ScreenToCanvas(screen);
        }

        Vector2 ScreenToCanvas(Vector2 screen)
        {
            var rt = (RectTransform)badge.parent;
            var uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, uiCam, out var local);
            return local;
        }
    }
}
