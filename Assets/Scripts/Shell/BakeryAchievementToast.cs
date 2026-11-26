using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Fatcat.Slothy.Bakery.Modes;
using TMPro;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>Queues cozy, localized achievement banners across scene changes.</summary>
    public sealed class BakeryAchievementToast : MonoBehaviour
    {
        private const string ResourcePath = "UI/BakeryAchievementToast";
        [SerializeField] private RectTransform banner;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text heading;
        [SerializeField] private TMP_Text title;

        private static BakeryAchievementToast instance;
        private static readonly Queue<BakeryAchievements.Definition> Queue = new();
        private Coroutine playback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe()
        {
            BakeryAchievements.Unlocked -= OnUnlocked;
            BakeryAchievements.Unlocked += OnUnlocked;
        }

        private static void OnUnlocked(BakeryAchievements.Definition achievement)
        {
            if (!Application.isPlaying || achievement == null) return;
            if (instance == null)
            {
                var prefab = Resources.Load<BakeryAchievementToast>(ResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[BakeryAchievementToast] Missing Resources/{ResourcePath}.prefab");
                    return;
                }
                instance = Instantiate(prefab);
                instance.name = nameof(BakeryAchievementToast);
                DontDestroyOnLoad(instance.gameObject);
            }
            Queue.Enqueue(achievement);
            if (instance.playback == null) instance.playback = instance.StartCoroutine(instance.PlayQueue());
        }

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        private IEnumerator PlayQueue()
        {
            while (Queue.Count > 0)
            {
                var transition = FindAnyObjectByType<INART.SlothyBakery.SceneTransition.SceneTransitionController>();
                while (transition != null && !transition.EndAnimation)
                {
                    yield return null;
                    if (transition == null)
                        transition = FindAnyObjectByType<INART.SlothyBakery.SceneTransition.SceneTransitionController>();
                }

                var achievement = Queue.Dequeue();
                if (heading != null) heading.text = Localized("achievement.toast", "¡Logro desbloqueado!");
                if (title != null) title.text = Localized(achievement.TitleKey, achievement.Id);
                if (banner == null || group == null) continue;

                float height = Mathf.Max(92f, banner.rect.height);
                Vector2 hidden = new Vector2(0f, height + 36f);
                Vector2 visible = new Vector2(0f, -14f);
                banner.DOKill();
                group.DOKill();
                banner.anchoredPosition = hidden;
                group.alpha = 0f;

                var enterSeq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject)
                    .Join(banner.DOAnchorPos(visible, 0.36f).SetEase(Ease.OutBack, 1.3f))
                    .Join(group.DOFade(1f, 0.22f).SetEase(Ease.OutQuad));
                yield return enterSeq.WaitForCompletion();
                yield return new WaitForSecondsRealtime(2.4f);

                var exitSeq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject)
                    .Join(banner.DOAnchorPos(hidden, 0.24f).SetEase(Ease.InCubic))
                    .Join(group.DOFade(0f, 0.20f).SetEase(Ease.InQuad));
                yield return exitSeq.WaitForCompletion();
            }
            playback = null;
        }

        private void OnDisable() => DOTween.Kill(this);

        private static string Localized(string key, string fallback)
            => BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);
    }
}
