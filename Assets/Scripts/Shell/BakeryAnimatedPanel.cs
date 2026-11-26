using System;
using DG.Tweening;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Shared DOTween transition helper for Bakery Option A UI cards and overlays.
    /// Uses unscaled time so transitions animate cleanly while Time.timeScale == 0.
    /// </summary>
    public sealed class BakeryAnimatedPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform[] staggeredItems;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            if (card != null) card.localScale = Vector3.one;
            if (staggeredItems != null)
            {
                for (int i = 0; i < staggeredItems.Length; i++)
                {
                    if (staggeredItems[i] != null) staggeredItems[i].localScale = Vector3.one;
                }
            }
        }

        public void Open(bool stagger = true)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            DOTween.Kill(this);

            if (group != null)
            {
                group.DOKill();
                group.interactable = true;
                group.blocksRaycasts = true;
                group.alpha = 0.6f;
            }

            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.84f;
            }

            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject);
            if (group != null)
                seq.Join(group.DOFade(1f, 0.16f).SetEase(Ease.OutQuad));
            if (card != null)
                seq.Join(card.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 1.5f));

            if (stagger && staggeredItems != null)
            {
                for (int i = 0; i < staggeredItems.Length; i++)
                {
                    var item = staggeredItems[i];
                    if (item == null || !item.gameObject.activeSelf) continue;
                    item.DOKill();
                    item.localScale = Vector3.one * 0.78f;
                    seq.Insert(0.06f + i * 0.04f,
                        item.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 1.45f));
                }
            }
            if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                seq.Complete(true);
        }

        public void Close(Action onClosed = null)
        {
            if (!gameObject.activeSelf)
            {
                onClosed?.Invoke();
                return;
            }

            DOTween.Kill(this);
            if (group != null)
            {
                group.DOKill();
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject);
            if (group != null)
                seq.Join(group.DOFade(0f, 0.14f).SetEase(Ease.InQuad));
            if (card != null)
                seq.Join(card.DOScale(0.90f, 0.14f).SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                if (card != null) card.localScale = Vector3.one;
                gameObject.SetActive(false);
                onClosed?.Invoke();
            });
        }
    }
}
