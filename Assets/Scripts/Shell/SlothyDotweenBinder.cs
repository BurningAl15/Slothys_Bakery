using DG.Tweening;
using Fatcat.Slothy.Core.ShellUI;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Binds SlothyShellPanel (from shared com.fatcat.slothy.core) to Bakery's DOTween choreography.
    /// Keeps com.fatcat.slothy.core decoupled from DOTween while giving every panel smooth OutBack animations.
    /// </summary>
    public static class SlothyDotweenBinder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SlothyShellPanel.CustomOpen = (panel, group, card, staggered) =>
            {
                if (panel == null) return;
                DOTween.Kill(panel);
                if (group != null)
                {
                    group.DOKill();
                    group.interactable = true;
                    group.blocksRaycasts = true;
                    group.alpha = 0.65f;
                }
                if (card != null)
                {
                    card.DOKill();
                    card.localScale = Vector3.one * 0.84f;
                }

                var seq = DOTween.Sequence().SetUpdate(true).SetTarget(panel).SetLink(panel.gameObject);
                if (group != null)
                    seq.Join(group.DOFade(1f, 0.16f).SetEase(Ease.OutQuad));
                if (card != null)
                    seq.Join(card.DOScale(1f, 0.34f).SetEase(Ease.OutBack, 1.55f));

                if (staggered != null)
                {
                    for (int i = 0; i < staggered.Length; i++)
                    {
                        var item = staggered[i];
                        if (item == null || !item.gameObject.activeSelf) continue;
                        item.DOKill();
                        item.localScale = Vector3.one * 0.78f;
                        seq.Insert(0.06f + i * 0.04f,
                            item.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 1.45f));
                    }
                }

                if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                    seq.Complete(true);
            };

            SlothyShellPanel.CustomClose = (panel, group, card, onDone) =>
            {
                if (panel == null)
                {
                    onDone?.Invoke();
                    return;
                }

                DOTween.Kill(panel);
                if (group != null)
                {
                    group.DOKill();
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }

                var seq = DOTween.Sequence().SetUpdate(true).SetTarget(panel).SetLink(panel.gameObject);
                if (group != null)
                    seq.Join(group.DOFade(0f, 0.14f).SetEase(Ease.InQuad));
                if (card != null)
                    seq.Join(card.DOScale(0.90f, 0.14f).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    if (card != null) card.localScale = Vector3.one;
                    onDone?.Invoke();
                });

                if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                    seq.Complete(true);
            };

            SlothyDiplomaView.CustomShow = (view, group, paper, seal, stars, placeholder) =>
            {
                if (view == null) return;
                DOTween.Kill(view);
                if (group != null)
                {
                    group.DOKill();
                    group.blocksRaycasts = true;
                    group.interactable = true;
                    group.alpha = 0f;
                }
                if (paper != null)
                {
                    paper.DOKill();
                    paper.localScale = Vector3.one * 0.85f;
                    paper.localRotation = Quaternion.Euler(0, 0, -6f);
                }
                if (seal != null) { seal.DOKill(); seal.localScale = Vector3.zero; }
                if (stars != null)
                    for (int i = 0; i < stars.Length; i++)
                        if (stars[i] != null) stars[i].transform.localScale = Vector3.zero;

                var seq = DOTween.Sequence().SetUpdate(true).SetTarget(view).SetLink(view.gameObject);
                if (group != null) seq.Append(group.DOFade(1f, 0.2f));
                if (paper != null)
                {
                    seq.Join(paper.DOScale(1f, 0.55f).SetEase(Ease.OutBack, 1.3f));
                    seq.Join(paper.DOLocalRotate(Vector3.zero, 0.55f).SetEase(Ease.OutBack));
                }
                if (seal != null)
                {
                    seq.Insert(0.32f, seal.DOScale(1f, 0.30f).SetEase(Ease.OutBack, 2.0f));
                }
                if (stars != null)
                    for (int i = 0; i < stars.Length; i++)
                        if (stars[i] != null)
                            seq.Insert(0.45f + i * 0.045f, stars[i].transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 2f));
                if (placeholder != null && placeholder.gameObject.activeInHierarchy)
                    seq.Append(placeholder.DOPunchScale(Vector3.one * 0.08f, 0.45f, 4, 0.5f));

                if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                    seq.Complete(true);
            };

            SlothyDiplomaView.CustomHide = (view, group, paper, onDone) =>
            {
                if (view == null) { onDone?.Invoke(); return; }
                DOTween.Kill(view);
                var seq = DOTween.Sequence().SetUpdate(true).SetTarget(view).SetLink(view.gameObject);
                if (paper != null) seq.Append(paper.DOScale(0.9f, 0.18f).SetEase(Ease.InQuad));
                if (group != null) seq.Join(group.DOFade(0f, 0.18f));
                seq.OnComplete(() =>
                {
                    if (paper != null) paper.localScale = Vector3.one;
                    onDone?.Invoke();
                });

                if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                    seq.Complete(true);
            };
        }
    }
}
