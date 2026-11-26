using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Simple multiplication gate before external store links / Share / Rate.
    /// Adapted to Slothy's Bakery shell palette from CafeParentalGate/PizzaParentalGate.
    /// </summary>
    public sealed class BakeryParentalGate : MonoBehaviour
    {
        const string PrefabResourcePath = "UI/ParentalGate";

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private TMP_Text lockLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Button[] options;
        [SerializeField] private TMP_Text[] optionLabels;

        private static BakeryParentalGate _active;
        private Action _onVerified;
        private int _correctIndex;

        public static bool IsOpen => _active != null && _active.gameObject.activeInHierarchy;

        public static void Ask(Action onVerified)
        {
            if (onVerified == null) return;
            EnsureInstance();
            if (_active == null) return;
            _active.Open(onVerified);
        }

        private static void EnsureInstance()
        {
            if (_active != null) return;
            var prefab = Resources.Load<BakeryParentalGate>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("[BakeryParentalGate] Prefab missing at Resources/" + PrefabResourcePath);
                return;
            }
            _active = Instantiate(prefab);
            DontDestroyOnLoad(_active.gameObject);
            _active.gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int idx = i;
                    if (options[i] != null)
                        options[i].onClick.AddListener(() => PickOption(idx));
                }
            }
        }

        public void Open(Action onVerified)
        {
            _onVerified = onVerified;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            GenerateQuestion();
            PlayOpenAnimation();
        }

        public void Close()
        {
            _onVerified = null;
            DOTween.Kill(this);
            if (group != null) group.DOKill();
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one;
            }
            gameObject.SetActive(false);
        }

        public void PickOption(int index)
        {
            if (index == _correctIndex)
            {
                var action = _onVerified;
                Close();
                action?.Invoke();
                return;
            }

            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one;
                card.DOShakeAnchorPos(0.28f, new Vector2(10f, 0f), 14, 0f)
                    .SetUpdate(true)
                    .SetTarget(this)
                    .SetLink(gameObject);
            }
            GenerateQuestion();
        }

        private void GenerateQuestion()
        {
            int a = UnityEngine.Random.Range(4, 10);
            int b = UnityEngine.Random.Range(4, 10);
            int answer = a * b;

            if (lockLabel != null) lockLabel.text = Localize("gate.lock", "Solo para adultos");
            if (titleLabel != null) titleLabel.text = Localize("gate.title", "Pídele ayuda a un adulto");
            if (hintLabel != null) hintLabel.text = Localize("gate.hint", "Vamos a abrir una página fuera del juego.");
            if (questionLabel != null)
            {
                string fmt = Localize("gate.question", "¿Cuánto es {0} × {1}?");
                questionLabel.text = string.Format(fmt, a, b);
            }

            _correctIndex = UnityEngine.Random.Range(0, 3);
            int d1 = answer + (UnityEngine.Random.value < 0.5f ? a : -a);
            if (d1 == answer || d1 < 4) d1 = answer + 6;
            int d2 = answer + (UnityEngine.Random.value < 0.5f ? 4 : -4);
            if (d2 == answer || d2 == d1 || d2 < 4) d2 = answer + 8;

            int wrongIdx = 0;
            for (int i = 0; i < 3; i++)
            {
                int val = i == _correctIndex ? answer : (wrongIdx++ == 0 ? d1 : d2);
                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = val.ToString();
            }
        }

        private void PlayOpenAnimation()
        {
            DOTween.Kill(this);
            if (group != null)
            {
                group.DOKill();
                group.alpha = 0.6f;
            }
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.84f;
            }

            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject);
            if (group != null) seq.Join(group.DOFade(1f, 0.16f).SetEase(Ease.OutQuad));
            if (card != null) seq.Join(card.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 1.5f));
            if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                seq.Complete(true);
        }

        private void OnDisable() => DOTween.Kill(this);

        private static string Localize(string key, string fallback)
            => BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);
    }
}
