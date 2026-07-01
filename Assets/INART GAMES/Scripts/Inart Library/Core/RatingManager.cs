using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using INART.Framework.Core;

namespace INART.SlothyBakery.Rating
{
    using Core;
    using Analytics;

    /// <summary>
    /// Shows a friendly rating popup after the player completes the game N times.
    /// Generates its own UI at runtime — NO prefab or scene setup required.
    ///
    /// SETUP (one-time):
    ///   1. Add to your persistent Loader GameObject (same as AnalyticsManager).
    ///   2. Fill PlayStoreUrl and AppStoreUrl in the Inspector.
    ///   3. Adjust CompletionsBeforePrompt (default: 2).
    ///
    /// HOW IT WORKS:
    ///   Call OnGameCompleted() from your game's end sequence.
    ///   It checks completion count internally and shows the popup if conditions are met.
    ///   After the player rates or chooses "Never", the popup never shows again.
    ///   "Later" postpones by another N completions.
    ///
    /// Integration:
    ///   - Reads AnalyticsManager.Keys.COMPLETIONS_COUNT (set by AnalyticsManager.TrackGameCompleted)
    ///   - Tracks the rating action via AnalyticsManager.TrackCustom
    ///   - Respects SettingsManager theme colors if assigned
    /// </summary>
    public class RatingManager : Singleton<RatingManager>
    {
        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Store URLs")]
        [Tooltip("https://play.google.com/store/apps/details?id=YOUR_BUNDLE_ID")]
        [SerializeField] private string playStoreUrl = "";
        [Tooltip("Leave empty if not published on iOS.")]
        [SerializeField] private string appStoreUrl  = "";

        [Header("Trigger")]
        [Tooltip("Show the popup after this many full game completions.")]
        [SerializeField] private int completionsBeforePrompt = 2;

        [Header("Popup Text (localize as needed)")]
        [SerializeField] private string titleText   = "¿Disfrutando Slothy's Bakery?";
        [SerializeField] private string bodyText    = "Tu opinión nos ayuda a seguir mejorando el juego.";
        [SerializeField] private string rateBtnText  = "⭐  Calificar";
        [SerializeField] private string laterBtnText = "Más tarde";
        [SerializeField] private string neverBtnText = "No, gracias";

        [Header("Theme Colors")]
        [SerializeField] private Color accentColor   = new Color(0.98f, 0.76f, 0.10f);
        [SerializeField] private Color cardColor     = new Color(0.14f, 0.12f, 0.11f, 0.98f);
        [SerializeField] private Color overlayColor  = new Color(0f, 0f, 0f, 0.72f);

        // ── PlayerPrefs keys ───────────────────────────────────────────────────
        private const string KEY_RATING_DONE        = "rating_never_show";
        private const string KEY_LAST_PROMPTED      = "rating_last_prompted_completion";

        // ── Runtime ────────────────────────────────────────────────────────────
        private GameObject _popupRoot;
        private bool       _isShowing;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Call this AFTER AnalyticsManager.TrackGameCompleted() at the end of each full game.
        /// Checks conditions internally before showing the popup.
        /// </summary>
        public void OnGameCompleted()
        {
            if (PlayerPrefs.GetInt(KEY_RATING_DONE, 0) == 1) return;

            int completions  = PlayerPrefs.GetInt(AnalyticsManager.Keys.COMPLETIONS_COUNT, 0);
            int lastPrompted = PlayerPrefs.GetInt(KEY_LAST_PROMPTED, 0);

            bool shouldShow = completions >= completionsBeforePrompt
                           && (completions - lastPrompted) >= completionsBeforePrompt;

            GameLogger.Log(
                $"RatingManager: completions={completions}, threshold={completionsBeforePrompt}, show={shouldShow}",
                LogCategory.System);

            if (!shouldShow) return;

            PlayerPrefs.SetInt(KEY_LAST_PROMPTED, completions);
            PlayerPrefs.Save();

            StartCoroutine(ShowDelayed(1.8f));
        }

        /// <summary>Forces the popup to show regardless of completion count. Useful for testing.</summary>
        public void ForceShow() => StartCoroutine(ShowDelayed(0f));

        // ── Popup lifecycle ────────────────────────────────────────────────────

        private IEnumerator ShowDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!_isShowing) Show();
        }

        private void Show()
        {
            if (_isShowing) return;
            _isShowing  = true;
            _popupRoot  = BuildPopup();
            _popupRoot.SetActive(true);
        }

        private void Hide()
        {
            _isShowing = false;
            if (_popupRoot != null) Destroy(_popupRoot);
        }

        // ── Button callbacks ───────────────────────────────────────────────────

        private void OnRateClicked()
        {
            PlayerPrefs.SetInt(KEY_RATING_DONE, 1);
            PlayerPrefs.Save();
            Hide();
            OpenStore();

            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackCustom("rating_popup_rate");
        }

        private void OnLaterClicked()
        {
            Hide();
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackCustom("rating_popup_later");
        }

        private void OnNeverClicked()
        {
            PlayerPrefs.SetInt(KEY_RATING_DONE, 1);
            PlayerPrefs.Save();
            Hide();
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackCustom("rating_popup_never");
        }

        private void OpenStore()
        {
#if UNITY_IOS
            string url = !string.IsNullOrEmpty(appStoreUrl) ? appStoreUrl : playStoreUrl;
#else
            string url = playStoreUrl;
#endif
            if (!string.IsNullOrEmpty(url))
                Application.OpenURL(url);
            else
                GameLogger.Warning("RatingManager: no store URL configured.", LogCategory.System);
        }

        // ── Runtime UI builder ─────────────────────────────────────────────────
        // Generates the popup entirely at runtime — no prefab needed.
        // Customize colors via Inspector fields.

        private GameObject BuildPopup()
        {
            // Root canvas — renders above everything
            var canvasGO  = new GameObject("RatingPopupCanvas");
            var canvas    = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Semi-transparent overlay
            var overlay = CreatePanel(canvasGO.transform, "Overlay",
                overlayColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Card
            var card = CreatePanel(overlay.transform, "Card",
                cardColor,
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.64f),
                Vector2.zero, Vector2.zero);

            // Accent bar
            CreatePanel(card.transform, "AccentBar", accentColor,
                new Vector2(0f, 0.965f), Vector2.one, Vector2.zero, Vector2.zero);

            // Stars
            CreateText(card.transform, "Stars", "⭐  ⭐  ⭐  ⭐  ⭐",
                40, accentColor,
                new Vector2(0f, 0.75f), new Vector2(1f, 0.96f));

            // Title
            CreateText(card.transform, "Title", titleText,
                30, Color.white,
                new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.76f));

            // Body
            CreateText(card.transform, "Body", bodyText,
                22, new Color(0.78f, 0.78f, 0.78f),
                new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.57f));

            // Rate button (primary)
            var rateBtn = CreateButton(card.transform, "BtnRate", rateBtnText,
                accentColor, new Color(0.12f, 0.10f, 0.08f), 24,
                new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.33f));
            rateBtn.onClick.AddListener(OnRateClicked);

            // Later button
            var laterBtn = CreateButton(card.transform, "BtnLater", laterBtnText,
                new Color(0.28f, 0.28f, 0.28f), new Color(0.88f, 0.88f, 0.88f), 20,
                new Vector2(0.07f, 0.02f), new Vector2(0.50f, 0.15f));
            laterBtn.onClick.AddListener(OnLaterClicked);

            // Never button
            var neverBtn = CreateButton(card.transform, "BtnNever", neverBtnText,
                new Color(0.20f, 0.20f, 0.20f), new Color(0.50f, 0.50f, 0.50f), 20,
                new Vector2(0.52f, 0.02f), new Vector2(0.93f, 0.15f));
            neverBtn.onClick.AddListener(OnNeverClicked);

            return canvasGO;
        }

        // ── UI helpers ─────────────────────────────────────────────────────────

        private RectTransform CreatePanel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.color              = color;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Color bgColor, Color textColor, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<Image>().color = bgColor;

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor     = bgColor * 0.8f;
            btn.colors              = colors;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRt       = labelGO.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f,  4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text               = label;
            tmp.fontSize           = fontSize;
            tmp.color              = textColor;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.fontStyle          = FontStyles.Bold;
            tmp.enableWordWrapping = false;

            return btn;
        }

#if UNITY_EDITOR
        [ContextMenu("Force Show Popup (Editor Test)")]
        private void EditorForceShow() => ForceShow();

        [ContextMenu("Reset Rating State")]
        private void EditorResetRating()
        {
            PlayerPrefs.DeleteKey(KEY_RATING_DONE);
            PlayerPrefs.DeleteKey(KEY_LAST_PROMPTED);
            PlayerPrefs.Save();
            Debug.Log("[RatingManager] Rating state reset.");
        }
#endif
    }
}
