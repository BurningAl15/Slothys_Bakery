using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Feedback
{
    /// <summary>
    /// "Finish" of the feedback kit: once the interface has left, a card drops in and the stars land one by one
    /// with a sparkle each, then confetti. It waits for a tap on Continue (or a few seconds). Part of the
    /// FeedbackStamp prefab.
    /// </summary>
    public sealed class FeedbackCelebration : MonoBehaviour
    {
        [SerializeField] private CanvasGroup dim;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title, subtitle, buttonText;
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Color starOn = new Color32(0xF2, 0xC1, 0x4E, 0xFF), starOff = new Color32(0xE6, 0xD9, 0xE2, 0xFF);
        [SerializeField] private Button continueButton;
        [SerializeField] private float autoContinue = 6f;

        /// <summary>Played on each star that lands lit (index 0..2); the game plays its chime.</summary>
        public static Action<int> StarSound;

        bool tapped;
        Vector2 cardHome;

        private void Awake()
        {
            cardHome = card.anchoredPosition;
            if (continueButton != null) continueButton.onClick.AddListener(() => tapped = true);
            gameObject.SetActive(false);
        }

        private void OnDestroy() => DOTween.Kill(this);

        public static IEnumerator Show(int litStars, string titleText, string subtitleText, string continueText)
        {
            var stamp = FeedbackStamp.Instance;
            var c = stamp != null ? stamp.GetComponentInChildren<FeedbackCelebration>(true) : null;
            if (c == null) yield break;
            yield return c.Play(litStars, titleText, subtitleText, continueText);
        }

        IEnumerator Play(int lit, string titleText, string subtitleText, string continueText)
        {
            gameObject.SetActive(true);
            tapped = false;
            title.text = titleText; subtitle.text = subtitleText;
            if (buttonText != null) buttonText.text = continueText;
            dim.alpha = 0f; dim.blocksRaycasts = true;
            card.anchoredPosition = cardHome + Vector2.up * 700f;
            for (int i = 0; i < stars.Length; i++) { stars[i].color = i < lit ? starOn : starOff; stars[i].transform.localScale = Vector3.zero; }
            if (continueButton != null) continueButton.transform.localScale = Vector3.zero;

            yield return DOTween.Sequence().SetTarget(this)
                .Append(dim.FxFade(1f, 0.3f))
                .Append(card.FxAnchorPos(cardHome, 0.6f).SetEase(Ease.OutBack))
                .WaitForCompletion();

            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i].transform;
                s.localRotation = Quaternion.Euler(0, 0, -90f);
                yield return DOTween.Sequence().SetTarget(this)
                    .Append(s.DOScale(1f, 0.38f).SetEase(Ease.OutBack, 2f))
                    .Join(s.DOLocalRotate(Vector3.zero, 0.38f).SetEase(Ease.OutBack))
                    .WaitForCompletion();
                if (i < lit)
                {
                    StarSound?.Invoke(i);
                    FeedbackStamp.Sparkle(WorldOf(s), 10);
                }
            }
            if (continueButton != null) continueButton.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetTarget(this);

            for (float t = 0; t < autoContinue && !tapped; t += Time.unscaledDeltaTime) yield return null;

            yield return DOTween.Sequence().SetTarget(this)
                .Append(card.FxAnchorPos(cardHome + Vector2.down * 700f, 0.4f).SetEase(Ease.InBack))
                .Join(dim.FxFade(0f, 0.4f))
                .WaitForCompletion();
            dim.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>World point under a UI element (FeedbackStamp.Sparkle takes world points).</summary>
        static Vector3 WorldOf(Transform ui)
        {
            var cam = Camera.main;
            var canvas = ui.GetComponentInParent<Canvas>();
            var screen = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? RectTransformUtility.WorldToScreenPoint(null, ui.position)
                : RectTransformUtility.WorldToScreenPoint(canvas != null ? canvas.worldCamera : cam, ui.position);
            return cam != null ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f)) : Vector3.zero;
        }
    }
}
