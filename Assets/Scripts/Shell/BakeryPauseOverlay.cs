using System;
using System.Collections.Generic;
using DG.Tweening;
using Fatcat.Slothy.Bakery.Modes;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.Core;
using INART.SlothyBakery.SceneTransition;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Shared gameplay Pause HUD overlay for Slothy's Bakery (Option A pastel styling + DOTween animations).
    /// Shown only during active minigame scenes when GameStates is in Play (or paused).
    /// </summary>
    public sealed class BakeryPauseOverlay : MonoBehaviour
    {
        private static BakeryPauseOverlay _instance;

        private Canvas _canvas;
        private Button _pauseButton;
        private GameObject _modal;
        private bool _paused;

        private TMP_FontAsset _displayFont;
        private TMP_FontAsset _bodyFont;
        private Sprite _cardSprite;
        private Sprite _pillHeaderSprite;
        private Sprite _berryButtonSprite;
        private Sprite _plumBoxSprite;
        private Sprite _lightBoxSprite;

        private static readonly Color Plum = new Color32(0x58, 0x35, 0x53, 0xFF);
        private static readonly Color Cream = new Color32(0xFF, 0xF9, 0xF0, 0xFF);
        private static readonly Color Vanilla = new Color32(0xFD, 0xEC, 0xD7, 0xFF);
        private static readonly Color Berry = new Color32(0xE4, 0x74, 0x98, 0xFF);
        private static readonly Color DimColor = new Color(0.21f, 0.11f, 0.19f, 0.58f);

        public bool IsPaused => _paused;
        public Button PauseButton => _pauseButton;
        public static BakeryPauseOverlay Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var root = new GameObject(nameof(BakeryPauseOverlay));
            _instance = root.AddComponent<BakeryPauseOverlay>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildCanvas();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (_instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            EnsureEventSystem();
            CloseModal();
            _paused = false;
            Time.timeScale = 1f;
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(IsGameplayScene(scene.name));
        }

        private void Update()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (_pauseButton != null && IsGameplayScene(sceneName))
            {
                bool playing = GameStates.Instance != null && GameStates.Instance.IsGamePlaying();
                bool show = _paused || playing;
                if (_pauseButton.gameObject.activeSelf != show)
                    _pauseButton.gameObject.SetActive(show);
            }
            else if (_pauseButton != null && _pauseButton.gameObject.activeSelf)
            {
                _pauseButton.gameObject.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && IsGameplayScene(sceneName))
            {
                if (_paused) Resume();
                else OpenPause();
            }
        }

        public static bool IsGameplayScene(string sceneName)
            => sceneName == SceneUtils.FirstMinigameScene
            || sceneName == SceneUtils.SecondMinigameScene
            || sceneName == SceneUtils.ThirdMinigameScene;

        public void OpenPause()
        {
            if (_paused || !IsGameplayScene(SceneManager.GetActiveScene().name)) return;
            if (GameStates.Instance != null && !GameStates.Instance.IsGamePlaying()) return;

            _paused = true;
            Time.timeScale = 0f;
            GameStates.Instance?.Set_GameState_Load();

            ShowPauseCard();
        }

        public void Resume()
        {
            if (!_paused) return;
            CloseModal();
            Time.timeScale = 1f;
            GameStates.Instance?.Set_GameState_Play();
            _paused = false;
        }

        public void Restart()
        {
            CloseModal();
            Time.timeScale = 1f;
            _paused = false;
            BakerySession.RestartCurrentChapter();
        }

        public void RequestExitToMenu()
        {
            if (!_paused) return;
            CloseModal();
            ShowConfirmExitCard();
        }

        public void ConfirmExitToMenu()
        {
            CloseModal();
            Time.timeScale = 1f;
            _paused = false;
            BakerySession.AbandonToMenu();
        }

        private void BuildCanvas()
        {
            var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _displayFont = Resources.Load<TMP_FontAsset>("Fonts/LilitaOne-Regular SDF") ?? fallback;
            _bodyFont = Resources.Load<TMP_FontAsset>("Fonts/Fredoka-VariableFont_wdth,wght SDF") ?? fallback;
            _cardSprite = Resources.Load<Sprite>("BakeryUI/ui_bakery_card");
            _pillHeaderSprite = Resources.Load<Sprite>("BakeryUI/ui_pill_header");
            _berryButtonSprite = Resources.Load<Sprite>("BakeryUI/ui_bakery_btn_berry");
            _plumBoxSprite = Resources.Load<Sprite>("BakeryUI/ui_rounded_plum");
            _lightBoxSprite = Resources.Load<Sprite>("BakeryUI/ui_rounded_light") ?? _plumBoxSprite;

            var canvasGo = new GameObject("BakeryPauseCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 600;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 576f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();

            var btnRt = CreateRect("PauseButton", canvasGo.transform);
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0f, 1f);
            btnRt.sizeDelta = new Vector2(58f, 58f);
            btnRt.anchoredPosition = new Vector2(22f, -22f);

            var circleSprite = Resources.Load<Sprite>("BakeryUI/ui_circle");
            var bg = btnRt.gameObject.AddComponent<Image>();
            if (circleSprite != null)
            {
                bg.sprite = circleSprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;
                bg.color = Color.white;
            }
            else
            {
                ApplySprite(bg, _lightBoxSprite ?? _plumBoxSprite, Color.white, 1.8f);
                var ol = btnRt.gameObject.AddComponent<Outline>();
                ol.effectColor = Plum;
                ol.effectDistance = new Vector2(2f, -2f);
            }
            var btnShadow = btnRt.gameObject.AddComponent<Shadow>();
            btnShadow.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.24f);
            btnShadow.effectDistance = new Vector2(0f, -3f);

            _pauseButton = btnRt.gameObject.AddComponent<Button>();
            _pauseButton.targetGraphic = bg;
            _pauseButton.onClick.AddListener(OpenPause);

            CreatePauseBar(btnRt, "BarL", -5.5f);
            CreatePauseBar(btnRt, "BarR", 5.5f);
            _pauseButton.gameObject.SetActive(false);
        }

        private void CreatePauseBar(RectTransform parent, string name, float xOffset)
        {
            var bar = CreateRect(name, parent);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(6f, 20f);
            bar.anchoredPosition = new Vector2(xOffset, 0f);
            var img = bar.gameObject.AddComponent<Image>();
            ApplySprite(img, _plumBoxSprite ?? _lightBoxSprite, Plum, 2.5f);
            img.raycastTarget = false;
        }

        private void ShowPauseCard()
        {
            CloseModal();
            var buttons = new List<RectTransform>();
            var card = CreateModalCard(262f, IsSpanish() ? "Pausa" : "Paused", out var dim, out var cardGroup);
            buttons.Add(CreateActionButton(card, "ContinueButton", IsSpanish() ? "Continuar" : "Continue", true, Resume));
            buttons.Add(CreateActionButton(card, "RestartButton", IsSpanish() ? "Reiniciar" : "Restart", false, Restart));
            buttons.Add(CreateActionButton(card, "MenuButton", IsSpanish() ? "Menú principal" : "Main Menu", false, RequestExitToMenu));
            AnimateModalOpen(dim, card, cardGroup, buttons);
        }

        private void ShowConfirmExitCard()
        {
            var buttons = new List<RectTransform>();
            var card = CreateModalCard(278f, IsSpanish() ? "¿Volver al menú?" : "Back to menu?", out var dim, out var cardGroup);
            string note = BakerySession.OffStory
                ? (IsSpanish() ? "Se cerrará esta sesión sin cambios en Historia." : "This session will close without changing Story.")
                : (IsSpanish() ? "Tu ronda completada seguirá guardada." : "Your completed round stays saved.");
            var msg = CreateText(card, "Note", note, 16f, Plum, TextAlignmentOptions.Center, false);
            msg.textWrappingMode = TextWrappingModes.Normal;
            var le = msg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 46f;

            buttons.Add(CreateActionButton(card, "ConfirmExitButton", IsSpanish() ? "Sí, salir" : "Yes, leave", true, ConfirmExitToMenu));
            buttons.Add(CreateActionButton(card, "CancelExitButton", IsSpanish() ? "Seguir jugando" : "Keep playing", false, ShowPauseCard));
            AnimateModalOpen(dim, card, cardGroup, buttons);
        }

        private RectTransform CreateModalCard(float height, string title, out Image dimImage, out CanvasGroup cardGroup)
        {
            _modal = new GameObject("PauseModal", typeof(RectTransform));
            _modal.transform.SetParent(_canvas.transform, false);
            var backdropRt = (RectTransform)_modal.transform;
            Stretch(backdropRt);
            dimImage = _modal.AddComponent<Image>();
            dimImage.color = DimColor;
            dimImage.raycastTarget = true;

            var cardRt = CreateRect("Card", _modal.transform);
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(390f, height);
            cardRt.anchoredPosition = new Vector2(0f, -10f);
            cardGroup = cardRt.gameObject.AddComponent<CanvasGroup>();
            var cardBg = cardRt.gameObject.AddComponent<Image>();
            ApplySprite(cardBg, _cardSprite, Color.white, 1.4f);
            if (_cardSprite == null)
            {
                cardBg.color = Cream;
                var ol = cardRt.gameObject.AddComponent<Outline>();
                ol.effectColor = Plum;
                ol.effectDistance = new Vector2(4f, -4f);
            }

            var layout = cardRt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 46, 26);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titlePill = CreateRect("TitlePill", cardRt);
            var ple = titlePill.gameObject.AddComponent<LayoutElement>();
            ple.ignoreLayout = true;
            titlePill.anchorMin = titlePill.anchorMax = titlePill.pivot = new Vector2(0.5f, 1f);
            titlePill.sizeDelta = new Vector2(268f, 52f);
            titlePill.anchoredPosition = new Vector2(0f, 18f);
            var pillBg = titlePill.gameObject.AddComponent<Image>();
            ApplySprite(pillBg, _pillHeaderSprite, Berry, 1.6f);
            if (_pillHeaderSprite != null) pillBg.color = Color.white;

            var titleTmp = CreateText(titlePill, "Title", title, 27f, Color.white, TextAlignmentOptions.Center, true);
            Stretch(titleTmp.rectTransform);
            titleTmp.rectTransform.offsetMin = new Vector2(16f, 4f);
            titleTmp.rectTransform.offsetMax = new Vector2(-16f, -4f);

            return cardRt;
        }

        private RectTransform CreateActionButton(Transform parent, string name, string label, bool primary, Action onClick)
        {
            var rt = CreateRect(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            var sprite = primary ? _berryButtonSprite : (_lightBoxSprite ?? _plumBoxSprite);
            ApplySprite(bg, sprite, Color.white, 1.6f);
            if (sprite == null)
            {
                bg.color = primary ? Berry : Cream;
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = Plum;
                ol.effectDistance = new Vector2(2f, -2f);
            }

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var tmp = CreateText(rt, "Label", label, 21f, primary ? Color.white : Plum, TextAlignmentOptions.Center, true);
            Stretch(tmp.rectTransform);
            return rt;
        }

        private void AnimateModalOpen(Image dim, RectTransform card, CanvasGroup cardGroup, List<RectTransform> buttons)
        {
            DOTween.Kill(this);
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(_modal);
            if (dim != null)
            {
                var c = dim.color;
                dim.color = new Color(c.r, c.g, c.b, 0.35f);
                seq.Join(dim.DOFade(c.a, 0.16f).SetEase(Ease.OutQuad));
            }
            if (cardGroup != null)
            {
                cardGroup.alpha = 0.65f;
                seq.Join(cardGroup.DOFade(1f, 0.16f).SetEase(Ease.OutQuad));
            }
            if (card != null)
            {
                card.localScale = Vector3.one * 0.84f;
                seq.Join(card.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 1.5f));
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;
                btn.localScale = Vector3.one * 0.78f;
                seq.Insert(0.08f + i * 0.045f, btn.DOScale(1f, 0.24f).SetEase(Ease.OutBack, 1.45f));
            }
            if (INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
                seq.Complete(true);
        }

        private static void ApplySprite(Image img, Sprite sprite, Color tint, float ppu)
        {
            img.sprite = sprite;
            img.color = tint;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppu;
            }
        }

        private void CloseModal()
        {
            DOTween.Kill(this);
            if (_modal != null)
            {
                Destroy(_modal);
                _modal = null;
            }
        }

        private static bool IsSpanish()
        {
            var loc = LocalizationSettings.SelectedLocale;
            return loc == null || loc.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, bool useDisplay)
        {
            var rt = CreateRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = useDisplay ? _displayFont : _bodyFont;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
