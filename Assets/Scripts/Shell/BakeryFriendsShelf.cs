using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FatCat.CrossPromo;
using INART.SlothyBakery.CanvasAnimator;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// "Amigos de Slothy" shelf on the start screen: up to three of the studio's games on a wooden shelf.
    /// Featured game sits first with a "New!" ribbon; tapping any icon opens its store link behind the parental gate;
    /// "See all games" opens the full More Games modal.
    /// </summary>
    public sealed class BakeryFriendsShelf : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public Button button;
            public Image icon;
            public RectTransform body;
        }

        [SerializeField] private CrossPromoCatalog catalog;
        [SerializeField] private Slot[] slots = new Slot[3];
        [SerializeField] private GameObject ribbon;
        [SerializeField] private Button seeAll;
        [SerializeField] private BakeryModesView modesView;

        private CrossPromoGame[] _shown = Array.Empty<CrossPromoGame>();
        private Sequence _sway;
        private Coroutine _refreshRoutine;
        private bool _refreshed;

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i].button != null)
                {
                    slots[i].button.onClick.AddListener(() =>
                    {
                        if (index < _shown.Length)
                        {
                            var game = _shown[index];
                            BakeryParentalGate.Ask(() => CrossPromo.Open(game));
                        }
                    });
                }
            }

            if (seeAll != null)
                seeAll.onClick.AddListener(() => { if (modesView != null) modesView.OpenMoreGames(); });
        }

        private void OnEnable()
        {
            Fill();
            if (!isActiveAndEnabled) return;
            Enter();

            if (!Application.isBatchMode && !_refreshed && catalog != null && !string.IsNullOrEmpty(catalog.remoteCatalogUrl))
            {
                _refreshed = true;
                _refreshRoutine = StartCoroutine(CrossPromo.Refresh(catalog, ok =>
                {
                    _refreshRoutine = null;
                    if (ok && isActiveAndEnabled) Fill();
                }));
            }
        }

        private void OnDisable()
        {
            if (_refreshRoutine != null) { StopCoroutine(_refreshRoutine); _refreshRoutine = null; }
            KillAnimations();
        }

        private void OnDestroy()
        {
            if (_refreshRoutine != null) { StopCoroutine(_refreshRoutine); _refreshRoutine = null; }
            KillAnimations();
        }

        private void KillAnimations()
        {
            _sway?.Kill();
            _sway = null;
            foreach (var s in slots)
            {
                if (s.body != null)
                {
                    s.body.DOKill();
                    s.body.anchoredPosition = Vector2.zero;
                    s.body.localScale = Vector3.one;
                    s.body.localRotation = Quaternion.identity;
                }
            }
            if (ribbon != null)
            {
                ribbon.transform.DOKill();
                ribbon.transform.localScale = Vector3.one;
            }
        }

        private void Fill()
        {
            if (catalog == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var store = Application.platform == RuntimePlatform.IPhonePlayer
                ? StorePlatform.AppStore
                : StorePlatform.GooglePlay;

            var games = CrossPromo.Available(catalog, store)
                .Where(g => g != null && g.icon != null)
                .OrderBy(g => g.order)
                .ToList();

            int n = Mathf.Min(games.Count, slots.Length);
            _shown = games.GetRange(0, n).ToArray();
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < n;
                if (slots[i].button != null) slots[i].button.gameObject.SetActive(used);
                if (used && slots[i].icon != null) slots[i].icon.sprite = _shown[i].icon;
            }

            if (ribbon != null) ribbon.SetActive(n > 0);
            gameObject.SetActive(n > 0 || !Application.isPlaying);
        }

        private void Enter()
        {
            KillAnimations();
            if (CanvasAnimator.SkipForTests) return;

            float delay = 0.45f;
            for (int i = 0; i < slots.Length; i++)
            {
                var body = slots[i].body;
                if (body == null || !body.gameObject.activeInHierarchy) continue;
                body.anchoredPosition = new Vector2(0f, 96f);
                body.localScale = Vector3.one * 0.65f;
                body.DOAnchorPos(Vector2.zero, 0.48f).SetEase(Ease.OutBounce).SetDelay(delay + i * 0.12f);
                body.DOScale(1f, 0.34f).SetEase(Ease.OutBack).SetDelay(delay + i * 0.12f);
            }

            if (ribbon != null && ribbon.activeSelf)
            {
                ribbon.transform.localScale = Vector3.zero;
                ribbon.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack, 2f).SetDelay(delay + 0.55f);
            }

            _sway = DOTween.Sequence().SetDelay(delay + 1.2f).SetLoops(-1).SetLink(gameObject);
            for (int i = 0; i < slots.Length; i++)
            {
                var body = slots[i].body;
                if (body == null) continue;
                _sway.Append(body.DOLocalRotate(new Vector3(0, 0, -5f), 0.22f).SetEase(Ease.InOutSine))
                     .Append(body.DOLocalRotate(new Vector3(0, 0, 4f), 0.28f).SetEase(Ease.InOutSine))
                     .Append(body.DOLocalRotate(Vector3.zero, 0.22f).SetEase(Ease.InOutSine))
                     .AppendInterval(0.45f);
            }
            _sway.AppendInterval(1.4f);
        }
    }
}
