using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using FatCat.CrossPromo;
using Fatcat.Slothy.Bakery.Modes;
using Fatcat.Slothy.Core.ShellUI;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.SceneTransition;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Host controller on BakeryShellCanvas for Practice, Daily Challenge, Afternoon Shift, Chef Mode,
    /// Achievements (card list), Progress Dashboard (6 cards), More Games (CrossPromo), Run Summary,
    /// and the integrated Home Modes Dock.
    /// </summary>
    public sealed class BakeryModesView : MonoBehaviour
    {
        [Header("Home Entry & Modes Modal")]
        [SerializeField] private Button openModesButton;
        [SerializeField] private Button openProgressButton;
        [SerializeField] private GameObject modesPanel;
        [SerializeField] private SlothyShellPanel modesPanelHost;
        [SerializeField] private Button modesCloseButton;
        [SerializeField] private Button modesBackdropButton;

        [Header("Integrated Home Mode Dock")]
        [SerializeField] private CanvasGroup homeDockGroup;
        [SerializeField] private RectTransform[] homeDockTiles;
        [SerializeField] private Button homeDailyButton;
        [SerializeField] private TMP_Text homeDailyStatus;
        [SerializeField] private GameObject homeDailyBadge;
        [SerializeField] private Button homePracticeButton;
        [SerializeField] private TMP_Text homePracticeStatus;
        [SerializeField] private Button homeAfternoonButton;
        [SerializeField] private TMP_Text homeAfternoonStatus;
        [SerializeField] private GameObject homeAfternoonLock;
        [SerializeField] private Button homeChefButton;
        [SerializeField] private TMP_Text homeChefStatus;
        [SerializeField] private GameObject homeChefLock;
        [SerializeField] private Button homeAchievementsButton;

        [Header("Practice Cards")]
        [SerializeField] private Button practiceMg1Button;
        [SerializeField] private TMP_Text practiceMg1Status;
        [SerializeField] private Button practiceMg2Button;
        [SerializeField] private TMP_Text practiceMg2Status;
        [SerializeField] private Button practiceMg3Button;
        [SerializeField] private TMP_Text practiceMg3Status;

        [Header("Runs (Daily / Afternoon / Chef)")]
        [SerializeField] private Button dailyButton;
        [SerializeField] private TMP_Text dailyStatus;
        [SerializeField] private Button afternoonButton;
        [SerializeField] private TMP_Text afternoonStatus;
        [SerializeField] private Button chefButton;
        [SerializeField] private TMP_Text chefStatus;

        [Header("Run Summary Modal")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private SlothyShellPanel summaryPanelHost;
        [SerializeField] private TMP_Text summaryTitle;
        [SerializeField] private TMP_Text summaryBody;
        [SerializeField] private Button summaryOkButton;

        [Header("Achievements Modal (Card List + Fallback)")]
        [SerializeField] private GameObject achievementsPanel;
        [SerializeField] private SlothyShellPanel achievementsPanelHost;
        [SerializeField] private SlothyAchievementRowView achievementRowPrefab;
        [SerializeField] private RectTransform achievementsListRoot;
        [SerializeField] private ScrollRect achievementsScroll;
        [SerializeField] private TMP_Text achievementsSummaryLabel;
        [SerializeField] private Sprite[] achievementIcons;
        [SerializeField] private TMP_Text achievementsBody;
        [SerializeField] private Button achievementsCloseButton;

        [Header("Progress Dashboard (6 Cards)")]
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private SlothyShellPanel progressPanelHost;
        [SerializeField] private SlothyDashboardCardView dashStory;
        [SerializeField] private SlothyDashboardCardView dashAfternoon;
        [SerializeField] private SlothyDashboardCardView dashChef;
        [SerializeField] private SlothyDashboardCardView dashDaily;
        [SerializeField] private SlothyDashboardCardView dashPractice;
        [SerializeField] private SlothyDashboardCardView dashAchievements;

        [Header("More Games Modal (CrossPromo)")]
        [SerializeField] private GameObject moreGamesPanel;
        [SerializeField] private SlothyShellPanel moreGamesPanelHost;
        [SerializeField] private CrossPromoCatalog crossPromoCatalog;
        [SerializeField] private CrossPromoCardView crossPromoCardPrefab;
        [SerializeField] private RectTransform moreGamesListRoot;
        [SerializeField] private ScrollRect moreGamesScroll;
        [SerializeField] private GameObject moreGamesEmptyState;
        [SerializeField] private Button moreGamesStoreButton;
        [SerializeField] private Button moreGamesCloseButton;

        private readonly List<CrossPromoCardView> _promoCards = new List<CrossPromoCardView>();
        private Coroutine _promoRefresh;
        private GameObject _lockedPopup;

        private void Awake()
        {
            if (openModesButton != null) openModesButton.onClick.AddListener(OpenModes);
            if (openProgressButton != null) openProgressButton.onClick.AddListener(OpenProgress);
            if (modesCloseButton != null) modesCloseButton.onClick.AddListener(CloseModes);
            if (modesBackdropButton != null) modesBackdropButton.onClick.AddListener(CloseModes);

            if (homeDailyButton != null) homeDailyButton.onClick.AddListener(() => BakerySession.BeginDaily());
            if (homePracticeButton != null) homePracticeButton.onClick.AddListener(OpenModes);
            if (homeAfternoonButton != null) homeAfternoonButton.onClick.AddListener(() => { if (BakerySession.AfternoonUnlocked) BakerySession.BeginAfternoon(); else ShowLockedModePopup(false); });
            if (homeChefButton != null) homeChefButton.onClick.AddListener(() => { if (BakerySession.ChefUnlocked) BakerySession.BeginChef(); else ShowLockedModePopup(true); });
            if (homeAchievementsButton != null) homeAchievementsButton.onClick.AddListener(OpenAchievements);

            if (practiceMg1Button != null) practiceMg1Button.onClick.AddListener(() => BakerySession.BeginPractice("mg1", SceneUtils.FirstMinigameScene));
            if (practiceMg2Button != null) practiceMg2Button.onClick.AddListener(() => BakerySession.BeginPractice("mg2", SceneUtils.SecondMinigameScene));
            if (practiceMg3Button != null) practiceMg3Button.onClick.AddListener(() => BakerySession.BeginPractice("mg3", SceneUtils.ThirdMinigameScene));

            if (dailyButton != null) dailyButton.onClick.AddListener(() => BakerySession.BeginDaily());
            if (afternoonButton != null) afternoonButton.onClick.AddListener(() => { if (BakerySession.AfternoonUnlocked) BakerySession.BeginAfternoon(); else ShowLockedModePopup(false); });
            if (chefButton != null) chefButton.onClick.AddListener(() => { if (BakerySession.ChefUnlocked) BakerySession.BeginChef(); else ShowLockedModePopup(true); });

            if (summaryOkButton != null) summaryOkButton.onClick.AddListener(CloseSummary);
            if (achievementsCloseButton != null) achievementsCloseButton.onClick.AddListener(CloseAchievements);
            if (moreGamesCloseButton != null) moreGamesCloseButton.onClick.AddListener(CloseMoreGames);
            if (moreGamesStoreButton != null)
                moreGamesStoreButton.onClick.AddListener(() => BakeryParentalGate.Ask(OpenDeveloperStore));

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (_promoRefresh != null) { StopCoroutine(_promoRefresh); _promoRefresh = null; }
            DOTween.Kill(this);
        }

        private void OnLocaleChanged(Locale _)
        {
            RefreshStatuses();
            RefreshMenuStoryButtons();
            if (progressPanel != null && progressPanel.activeSelf) RefreshDashboard();
        }

        private void Start()
        {
            RefreshStatuses();
            RefreshMenuStoryButtons();
            if (!BakeryLanguageGate.NeedsChoice)
                PlayHomeDockEntrance();
            else
                HideHomeDockForDeferredIntro();
            if (BakerySession.ConsumePendingSummary(out var summary))
                ShowSummary(summary);
        }

        public void BeginDeferredDockIntro()
        {
            PlayHomeDockEntrance();
        }

        private void HideHomeDockForDeferredIntro()
        {
            if (homeDockTiles == null) return;
            for (int i = 0; i < homeDockTiles.Length; i++)
                if (homeDockTiles[i] != null)
                    homeDockTiles[i].localScale = Vector3.zero;
        }

        /// <summary>
        /// Stacks Play/Continue and compact "New game" vertically in the center column (matching Slothy's Café)
        /// and shows the current Story chapter + exercise number (1-6) on Continue.
        /// </summary>
        private void RefreshMenuStoryButtons()
        {
            Button startBtn = null;
            Button continueBtn = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var b in root.GetComponentsInChildren<Button>(true))
                {
                    if (b.name == "StartBtn") startBtn = b;
                    else if (b.name == "ContinueBtn") continueBtn = b;
                }
            }
            if (startBtn == null || continueBtn == null) return;

            var berrySprite = Resources.Load<Sprite>("BakeryUI/ui_bakery_btn_berry");
            var lightSprite = Resources.Load<Sprite>("BakeryUI/ui_rounded_light");
            var fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var dispFont = Resources.Load<TMP_FontAsset>("Fonts/LilitaOne-Regular SDF") ?? fallbackFont;
            var bodyFont = Resources.Load<TMP_FontAsset>("Fonts/Fredoka-VariableFont_wdth,wght SDF") ?? fallbackFont;
            var plumInk = new Color32(0x58, 0x35, 0x53, 0xFF);

            bool es = IsSpanish();
            bool hasSave = SaveLoadState.Instance != null && SaveLoadState.Instance.HasSavedProgress();
            int chapterState = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetActualState() : 0;
            int roundIdx = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetRoundCheckpoint(chapterState) : 0;
            string chapterScene = SceneUtils.SceneForProgress(chapterState);
            string caption = $"{PrettySceneName(chapterScene, es)} · {Mathf.Clamp(roundIdx + 1, 1, 6)}/6";

            if (hasSave)
            {
                var crt = continueBtn.GetComponent<RectTransform>();
                crt.anchoredPosition = new Vector2(-345f, -32f);
                crt.sizeDelta = new Vector2(248f, 78f);
                if (continueBtn.TryGetComponent<Image>(out var cImg) && berrySprite != null)
                {
                    cImg.sprite = berrySprite;
                    cImg.type = Image.Type.Sliced;
                    cImg.pixelsPerUnitMultiplier = 1.6f;
                    cImg.color = Color.white;
                }

                var cTitle = EnsureButtonText(continueBtn.transform, "Text (TMP)", dispFont, 24f, Color.white, FontStyles.Normal,
                    new Vector2(0f, 11f), new Vector2(-24f, 34f));
                cTitle.text = Localized("home.continue", es ? "Continuar" : "Continue");

                var cSub = EnsureButtonText(continueBtn.transform, "StoryCaption", bodyFont, 13f, new Color(1f, 0.96f, 0.98f, 0.94f),
                    FontStyles.Normal, new Vector2(0f, -16f), new Vector2(-24f, 22f));
                cSub.text = caption;

                var srt = startBtn.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(-345f, -94f);
                srt.sizeDelta = new Vector2(168f, 30f);
                if (startBtn.TryGetComponent<Image>(out var sImg) && lightSprite != null)
                {
                    sImg.sprite = lightSprite;
                    sImg.type = Image.Type.Sliced;
                    sImg.pixelsPerUnitMultiplier = 2.2f;
                    sImg.color = new Color(1f, 0.98f, 0.94f, 0.95f);
                }
                var sLbl = EnsureButtonText(startBtn.transform, "Text (TMP)", bodyFont, 14f, plumInk,
                    FontStyles.Bold | FontStyles.Underline, Vector2.zero, new Vector2(-16f, 24f));
                sLbl.text = es ? "Nueva partida" : "New game";
                var oldLbl = startBtn.transform.Find("Label");
                if (oldLbl != null && oldLbl != sLbl.transform) oldLbl.gameObject.SetActive(false);
            }
            else
            {
                var srt = startBtn.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(-345f, -44f);
                srt.sizeDelta = new Vector2(248f, 74f);
                if (startBtn.TryGetComponent<Image>(out var sImg) && berrySprite != null)
                {
                    sImg.sprite = berrySprite;
                    sImg.type = Image.Type.Sliced;
                    sImg.pixelsPerUnitMultiplier = 1.6f;
                    sImg.color = Color.white;
                }
                var sLbl = EnsureButtonText(startBtn.transform, "Text (TMP)", dispFont, 26f, Color.white,
                    FontStyles.Normal, new Vector2(0f, 2f), new Vector2(-24f, 44f));
                sLbl.text = es ? "Nueva partida" : "New game";
                var oldLbl = startBtn.transform.Find("Label");
                if (oldLbl != null && oldLbl != sLbl.transform) oldLbl.gameObject.SetActive(false);
            }
        }

        private static TMP_Text EnsureButtonText(Transform parent, string childName, TMP_FontAsset font,
            float fontSize, Color color, FontStyles style, Vector2 pos, Vector2 sizeDelta)
        {
            var existing = parent.Find(childName) ?? (childName == "Text (TMP)" ? parent.GetComponentInChildren<TMP_Text>(true)?.transform : null);
            RectTransform rt;
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                rt = (RectTransform)existing;
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject(childName, typeof(RectTransform));
                rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = fontSize;
            tmp.fontSizeMin = fontSize * 0.65f;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void PlayHomeDockEntrance()
        {
            if (homeDockGroup == null) return;
            DOTween.Kill(this);
            homeDockGroup.alpha = 0f;
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this).SetLink(gameObject)
                .AppendInterval(0.32f)
                .Append(homeDockGroup.DOFade(1f, 0.25f).SetEase(Ease.OutQuad));

            if (homeDockTiles == null) return;
            for (int i = 0; i < homeDockTiles.Length; i++)
            {
                var tile = homeDockTiles[i];
                if (tile == null) continue;
                tile.DOKill();
                tile.localScale = Vector3.one * 0.78f;
                seq.Insert(0.35f + i * 0.055f, tile.DOScale(1f, 0.28f).SetEase(Ease.OutBack, 1.5f));
            }
        }

        public void OpenPractice() => OpenModes();
        public void ClosePractice() => CloseModes();

        public void OpenModes()
        {
            RefreshStatuses();
            if (modesPanelHost != null) modesPanelHost.Open();
            else if (modesPanel != null) modesPanel.SetActive(true);
        }

        public void CloseModes()
        {
            if (modesPanelHost != null) modesPanelHost.Close();
            else if (modesPanel != null) modesPanel.SetActive(false);
        }

        public void OpenProgress()
        {
            RefreshDashboard();
            if (progressPanelHost != null) progressPanelHost.Open();
            else if (progressPanel != null) progressPanel.SetActive(true);
        }

        public void CloseProgress()
        {
            if (progressPanelHost != null) progressPanelHost.Close();
            else if (progressPanel != null) progressPanel.SetActive(false);
        }

        public void OpenAchievements()
        {
            RebuildAchievementCards();
            if (achievementsBody != null)
                achievementsBody.text = BuildAchievementsReport();
            if (achievementsPanelHost != null) achievementsPanelHost.Open(false);
            else if (achievementsPanel != null) achievementsPanel.SetActive(true);
        }

        public void CloseAchievements()
        {
            if (achievementsPanelHost != null) achievementsPanelHost.Close();
            else if (achievementsPanel != null) achievementsPanel.SetActive(false);
        }

        public void OpenMoreGames()
        {
            RebuildMoreGamesCards();
            if (moreGamesPanelHost != null) moreGamesPanelHost.Open(false);
            else if (moreGamesPanel != null) moreGamesPanel.SetActive(true);

            if (!Application.isBatchMode && crossPromoCatalog != null && !string.IsNullOrEmpty(crossPromoCatalog.remoteCatalogUrl) && isActiveAndEnabled)
            {
                if (_promoRefresh != null) StopCoroutine(_promoRefresh);
                _promoRefresh = StartCoroutine(CrossPromo.Refresh(crossPromoCatalog, ok =>
                {
                    _promoRefresh = null;
                    if (ok && isActiveAndEnabled && moreGamesPanel != null && moreGamesPanel.activeSelf)
                        RebuildMoreGamesCards();
                }));
            }
        }

        public void CloseMoreGames()
        {
            if (_promoRefresh != null) { StopCoroutine(_promoRefresh); _promoRefresh = null; }
            if (moreGamesPanelHost != null) moreGamesPanelHost.Close();
            else if (moreGamesPanel != null) moreGamesPanel.SetActive(false);
        }

        public void ShowSummary(BakeryRunSummary summary)
        {
            if (summary == null || summaryPanel == null) return;
            bool es = IsSpanish();

            string modeTitle = summary.mode switch
            {
                BakeryModeKind.Practice => es ? "¡Práctica completada!" : "Practice Complete!",
                BakeryModeKind.Daily => es ? "¡Reto diario completado!" : "Daily Challenge Complete!",
                BakeryModeKind.Afternoon => es ? "¡Turno de tarde completado!" : "Afternoon Shift Complete!",
                BakeryModeKind.Chef => es ? "¡Modo Chef completado!" : "Chef Mode Complete!",
                _ => es ? "¡Partida completada!" : "Run Complete!"
            };
            if (summaryTitle != null) summaryTitle.text = modeTitle;

            var sb = new StringBuilder();
            foreach (var ch in summary.chapters)
            {
                string label = PrettySceneName(ch.sceneName, es);
                string stars = new string('*', ch.stars) + (ch.stars < 3 ? new string('-', 3 - ch.stars) : "");
                sb.AppendLine(es
                    ? $"{label}: {ch.stars}/3 estrellas ({ch.answers} resp., {ch.mistakes} errores) [{stars}]"
                    : $"{label}: {ch.stars}/3 stars ({ch.answers} ans., {ch.mistakes} mistakes) [{stars}]");
            }
            if (summary.newlyUnlockedChef)
                sb.AppendLine(es ? "\n¡Desbloqueaste Modo Chef!" : "\nChef Mode unlocked!");

            if (summaryBody != null) summaryBody.text = sb.ToString().TrimEnd();
            if (summaryPanelHost != null) summaryPanelHost.Open(false);
            else summaryPanel.SetActive(true);

            if (summary.mode == BakeryModeKind.Daily)
                INART.SlothyBakery.Notifications.NotificationManager.AskAfterFirstDaily();
        }

        public void CloseSummary()
        {
            if (summaryPanelHost != null) summaryPanelHost.Close();
            else if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        public void RefreshStatuses()
        {
            bool es = IsSpanish();
            SetPracticeLabel(practiceMg1Status, "mg1", SceneUtils.FirstMinigameScene, es);
            SetPracticeLabel(practiceMg2Status, "mg2", SceneUtils.SecondMinigameScene, es);
            SetPracticeLabel(practiceMg3Status, "mg3", SceneUtils.ThirdMinigameScene, es);

            bool dailyDone = BakeryStats.IsDailyCompleted(DateTime.Now.Date);
            string dailyText = dailyDone ? (es ? "Completado" : "Done today") : (es ? "Listo hoy" : "Ready today");
            if (dailyButton != null) dailyButton.interactable = !dailyDone;
            if (dailyStatus != null) dailyStatus.text = dailyText;
            if (homeDailyButton != null) homeDailyButton.interactable = !dailyDone;
            if (homeDailyStatus != null) homeDailyStatus.text = dailyText;
            if (homeDailyBadge != null) homeDailyBadge.SetActive(!dailyDone);

            if (homePracticeStatus != null) homePracticeStatus.text = es ? "3 temas" : "3 topics";

            bool afternoonUnlocked = BakerySession.AfternoonUnlocked;
            string afternoonText = afternoonUnlocked
                ? (BakeryStats.AfternoonFinished ? (es ? "Completado" : "Completed") : (es ? "Desbloqueado" : "Unlocked"))
                : (es ? "Termina historia" : "Finish Story");
            if (afternoonButton != null) { afternoonButton.interactable = true; SetLockedAlpha(afternoonButton.gameObject, afternoonUnlocked); }
            if (afternoonStatus != null) afternoonStatus.text = afternoonText;
            if (homeAfternoonButton != null) { homeAfternoonButton.interactable = true; SetLockedAlpha(homeAfternoonButton.gameObject, afternoonUnlocked); }
            if (homeAfternoonStatus != null) homeAfternoonStatus.text = afternoonText;
            if (homeAfternoonLock != null) homeAfternoonLock.SetActive(!afternoonUnlocked);

            bool chefUnlocked = BakerySession.ChefUnlocked;
            string chefText = chefUnlocked
                ? (BakeryStats.ChefFinished ? (es ? "Maestro Chef" : "Master Chef") : (es ? "Desbloqueado" : "Unlocked"))
                : (es ? "Termina Turno tarde" : "Finish Afternoon");
            if (chefButton != null) { chefButton.interactable = true; SetLockedAlpha(chefButton.gameObject, chefUnlocked); }
            if (chefStatus != null) chefStatus.text = chefText;
            if (homeChefButton != null) { homeChefButton.interactable = true; SetLockedAlpha(homeChefButton.gameObject, chefUnlocked); }
            if (homeChefStatus != null) homeChefStatus.text = chefText;
            if (homeChefLock != null) homeChefLock.SetActive(!chefUnlocked);
        }

        private void RebuildAchievementCards()
        {
            if (achievementsListRoot == null || achievementRowPrefab == null) return;

            bool es = IsSpanish();
            BakeryAchievements.Evaluate();
            for (int i = achievementsListRoot.childCount - 1; i >= 0; i--)
                Destroy(achievementsListRoot.GetChild(i).gameObject);

            string doneLabel = Localized("achievements.done", es ? "¡Logrado!" : "Earned!");
            var list = BakeryAchievements.All;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var row = Instantiate(achievementRowPrefab, achievementsListRoot, false);
                row.name = "Achievement_" + item.Id;
                Sprite icon = achievementIcons != null && i < achievementIcons.Length ? achievementIcons[i] : null;
                string title = Localized(item.TitleKey, item.Id);
                string desc = Localized(item.DescriptionKey, "");
                bool unlocked = BakeryAchievements.IsUnlocked(item.Id);
                int prog = BakeryAchievements.Progress(item);
                row.Bind(icon, title, desc, unlocked, prog, item.Target, doneLabel);
            }

            if (achievementsSummaryLabel != null)
            {
                string fmt = Localized("achievements.summary",
                    es ? "{0} de {1} logros desbloqueados" : "{0} of {1} achievements unlocked");
                achievementsSummaryLabel.text = string.Format(fmt, BakeryAchievements.UnlockedCount, list.Count);
            }

            Canvas.ForceUpdateCanvases();
            if (achievementsScroll != null) achievementsScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshDashboard()
        {
            bool es = IsSpanish();
            BakeryAchievements.Evaluate();

            int state = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetActualState() : 0;
            bool storyDone = BakerySession.AfternoonUnlocked || (SaveLoadState.Instance != null && SaveLoadState.Instance.Progress.completions > 0);
            string storyTitle = Localized("dash.t_story", es ? "Historia" : "Story");
            string storyBody = storyDone
                ? Localized("dash.story_done", es ? "¡Completada!" : "Finished!")
                : string.Format(Localized("dash.story_now", es ? "Vas en: {0}" : "Now: {0}"),
                    PrettySceneName(SceneUtils.SceneForProgress(state), es));
            string diplomaAction = storyDone ? Localized("diploma.open", es ? "Ver mi diploma" : "See my diploma") : null;
            dashStory?.Bind(storyTitle, storyBody, diplomaAction, storyDone ? () => { CloseProgress(); BakeryDiploma.Open(); } : null);

            int aftRuns = Mathf.Max(1, BakeryStats.Get("afternoon.finished"));
            string aftTitle = Localized("dash.t_afternoon", es ? "Turno de tarde" : "Afternoon shift");
            string aftBody = BakerySession.AfternoonUnlocked
                ? (BakeryStats.AfternoonFinished
                    ? (aftRuns == 1
                        ? (es ? "Jugado 1 vez" : "Played 1 time")
                        : string.Format(Localized("dash.times", es ? "Jugado {0} veces" : "Played {0} times"), aftRuns))
                    : Localized("dash.open", es ? "¡Abierto! Pruébalo" : "Open: try it!"))
                : Localized("dash.locked_story", es ? "Termina la historia para abrirlo" : "Finish the story to open it");
            dashAfternoon?.Bind(aftTitle, aftBody);

            string chefTitle = Localized("dash.t_chef", es ? "Modo chef" : "Chef mode");
            string chefBody = BakerySession.ChefUnlocked
                ? $"{PrettySceneName(SceneUtils.FirstMinigameScene, es)}: {BakeryStats.BestStars(SceneUtils.FirstMinigameScene)}/3\n" +
                  $"{PrettySceneName(SceneUtils.SecondMinigameScene, es)}: {BakeryStats.BestStars(SceneUtils.SecondMinigameScene)}/3\n" +
                  $"{PrettySceneName(SceneUtils.ThirdMinigameScene, es)}: {BakeryStats.BestStars(SceneUtils.ThirdMinigameScene)}/3"
                : Localized("dash.locked_afternoon", es ? "Termina el Turno de tarde para abrirlo" : "Finish the afternoon shift to open it");
            dashChef?.Bind(chefTitle, chefBody);

            int dailyDone = BakeryStats.Get("daily.finished");
            string dailyTitle = Localized("dash.t_daily", es ? "Reto diario" : "Daily challenge");
            string dailyBody = es
                ? $"Retos hechos: {dailyDone}\n{(BakeryStats.IsDailyCompleted(DateTime.Now.Date) ? "Hoy completado" : "Disponible hoy")}"
                : $"Challenges done: {dailyDone}\n{(BakeryStats.IsDailyCompleted(DateTime.Now.Date) ? "Done today" : "Available today")}";
            dashDaily?.Bind(dailyTitle, dailyBody);

            int totalPractices = BakeryStats.PracticeCount("mg1") + BakeryStats.PracticeCount("mg2") + BakeryStats.PracticeCount("mg3");
            int practicedTopics = (BakeryStats.PracticeCount("mg1") > 0 ? 1 : 0)
                                + (BakeryStats.PracticeCount("mg2") > 0 ? 1 : 0)
                                + (BakeryStats.PracticeCount("mg3") > 0 ? 1 : 0);
            string pracTitle = Localized("dash.t_practice", es ? "Práctica" : "Practice");
            string pracBody = es
                ? $"Prácticas: {totalPractices}\nTemas practicados: {practicedTopics} de 3"
                : $"Practices: {totalPractices}\nTopics practiced: {practicedTopics} of 3";
            dashPractice?.Bind(pracTitle, pracBody);

            string achTitle = Localized("dash.t_achievements", es ? "Logros" : "Achievements");
            string achBody = string.Format(Localized("dash.achievements", es ? "{0} de {1} conseguidos" : "{0} of {1} earned"),
                BakeryAchievements.UnlockedCount, BakeryAchievements.All.Count);
            string achAction = Localized("dash.see_achievements", es ? "Ver logros" : "See achievements");
            dashAchievements?.Bind(achTitle, achBody, achAction, () =>
            {
                CloseProgress();
                OpenAchievements();
            });
        }

        private void RebuildMoreGamesCards()
        {
            if (moreGamesListRoot == null || crossPromoCardPrefab == null || crossPromoCatalog == null) return;

            bool en = !IsSpanish();
            var store = Application.platform == RuntimePlatform.IPhonePlayer
                ? StorePlatform.AppStore
                : StorePlatform.GooglePlay;
            var games = CrossPromo.Available(crossPromoCatalog, store);

            while (_promoCards.Count < games.Count)
                _promoCards.Add(Instantiate(crossPromoCardPrefab, moreGamesListRoot, false));

            for (int i = 0; i < _promoCards.Count; i++)
            {
                bool active = i < games.Count;
                _promoCards[i].gameObject.SetActive(active);
                if (!active) continue;

                var game = games[i];
                _promoCards[i].Bind(game, en);
                foreach (var b in _promoCards[i].GetComponentsInChildren<Button>(true))
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => BakeryParentalGate.Ask(() => CrossPromo.Open(game)));
                }
            }

            if (moreGamesEmptyState != null) moreGamesEmptyState.SetActive(games.Count == 0);
            Canvas.ForceUpdateCanvases();
            if (moreGamesScroll != null) moreGamesScroll.verticalNormalizedPosition = 1f;
        }

        private static void SetPracticeLabel(TMP_Text target, string topicId, string sceneName, bool es)
        {
            if (target == null) return;
            int count = BakeryStats.PracticeCount(topicId);
            int stars = BakeryStats.BestStars(sceneName);
            if (count <= 0)
            {
                target.text = es ? "¡Nuevo! · Pruébalo" : "New! · Try it";
                return;
            }
            target.text = es
                ? $"Practicado {count} {(count == 1 ? "vez" : "veces")} · Mejor: {stars}/3"
                : $"Practiced {count} {(count == 1 ? "time" : "times")} · Best: {stars}/3";
        }

        private static string BuildAchievementsReport()
        {
            bool es = IsSpanish();
            BakeryAchievements.Evaluate();
            var sb = new StringBuilder();
            string heading = Localized("achievement.progress", es ? "Logros desbloqueados" : "Achievements unlocked");
            sb.Append(heading).Append(": ").Append(BakeryAchievements.UnlockedCount).Append("/").Append(BakeryAchievements.All.Count);
            foreach (var item in BakeryAchievements.All)
            {
                bool unlocked = BakeryAchievements.IsUnlocked(item.Id);
                string title = Localized(item.TitleKey, item.Id);
                string state = unlocked
                    ? Localized("achievement.unlocked", es ? "Desbloqueado" : "Unlocked")
                    : $"{BakeryAchievements.Progress(item)}/{item.Target}";
                string marker = unlocked ? "[+] " : "[ ] ";
                sb.AppendLine().Append(marker).Append(title).Append(" — ").Append(state);
            }
            return sb.ToString();
        }

        private static string Localized(string key, string fallback)
            => BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);

        private static string PrettySceneName(string sceneName, bool es)
        {
            if (sceneName == SceneUtils.FirstMinigameScene) return es ? "Comparar pasos" : "Step Comparison";
            if (sceneName == SceneUtils.SecondMinigameScene) return es ? "Elegir signo" : "Choose Sign";
            if (sceneName == SceneUtils.ThirdMinigameScene) return es ? "Escribir fracción" : "Write Fraction";
            return sceneName;
        }

        private static void OpenDeveloperStore()
        {
            string url = Application.platform == RuntimePlatform.IPhonePlayer
                ? "https://apps.apple.com/developer/fatcat-game-studio/id1775797699"
                : "https://play.google.com/store/apps/dev?id=7901842880017361877";
            Application.OpenURL(url);
        }

        private static void SetLockedAlpha(GameObject go, bool unlocked)
        {
            if (go == null) return;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = unlocked ? 1f : 0.78f;
        }

        private void ShowLockedModePopup(bool isChef)
        {
            if (_lockedPopup != null) Destroy(_lockedPopup);

            bool es = IsSpanish();
            var plum = new Color32(0x58, 0x35, 0x53, 0xFF);
            var plumMuted = new Color32(0x78, 0x55, 0x73, 0xFF);
            var blush = new Color32(0xFA, 0xDE, 0xE7, 0xFF);
            var fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var dispFont = Resources.Load<TMP_FontAsset>("Fonts/LilitaOne-Regular SDF") ?? fallbackFont;
            var bodyFont = Resources.Load<TMP_FontAsset>("Fonts/Fredoka-VariableFont_wdth,wght SDF") ?? fallbackFont;
            var cardSprite = Resources.Load<Sprite>("BakeryUI/ui_rounded");
            var pillSprite = Resources.Load<Sprite>("BakeryUI/ui_pill_header");
            var berrySprite = Resources.Load<Sprite>("BakeryUI/ui_bakery_btn_berry");
            var circleSprite = Resources.Load<Sprite>("BakeryUI/ui_circle");
            var modeSprite = Resources.Load<Sprite>(isChef ? "BakeryUI/icon_chef" : "BakeryUI/icon_sun");
            var lockSprite = Resources.Load<Sprite>("BakeryUI/icon_lock");

            var parentCanvas = GetComponentInParent<Canvas>();
            Transform host = parentCanvas != null ? parentCanvas.transform : transform.root;

            var root = new GameObject("LockedModePopup", typeof(RectTransform), typeof(Image));
            root.layer = gameObject.layer;
            var rootRt = (RectTransform)root.transform;
            rootRt.SetParent(host, false);
            rootRt.SetAsLastSibling();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            var dim = root.GetComponent<Image>();
            dim.color = new Color(0.16f, 0.08f, 0.16f, 0.56f);
            var dimBtn = root.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseLockedModePopup);
            _lockedPopup = root;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var cardRt = (RectTransform)card.transform;
            cardRt.SetParent(rootRt, false);
            cardRt.sizeDelta = new Vector2(450f, 268f);
            var cardImg = card.GetComponent<Image>();
            if (cardSprite != null) { cardImg.sprite = cardSprite; cardImg.type = Image.Type.Sliced; cardImg.pixelsPerUnitMultiplier = 1.6f; }
            cardImg.color = Color.white;
            var cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            cardShadow.effectDistance = new Vector2(0f, -8f);
            var cardCg = card.GetComponent<CanvasGroup>();

            var header = new GameObject("HeaderPill", typeof(RectTransform), typeof(Image));
            var hRt = (RectTransform)header.transform;
            hRt.SetParent(cardRt, false);
            hRt.anchorMin = hRt.anchorMax = hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(270f, 44f);
            hRt.anchoredPosition = new Vector2(0f, 18f);
            var hImg = header.GetComponent<Image>();
            if (pillSprite != null) { hImg.sprite = pillSprite; hImg.type = Image.Type.Sliced; hImg.pixelsPerUnitMultiplier = 1.6f; }
            else hImg.color = plum;

            var hLblGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            var hLblRt = (RectTransform)hLblGo.transform;
            hLblRt.SetParent(hRt, false);
            hLblRt.anchorMin = Vector2.zero; hLblRt.anchorMax = Vector2.one;
            hLblRt.offsetMin = new Vector2(14f, 4f); hLblRt.offsetMax = new Vector2(-14f, -4f);
            var hTmp = hLblGo.GetComponent<TextMeshProUGUI>();
            hTmp.font = dispFont;
            hTmp.fontSize = 21f;
            hTmp.color = Color.white;
            hTmp.alignment = TextAlignmentOptions.Center;
            hTmp.text = es ? "¡Aún está cerrado!" : "Still locked!";

            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            var bRt = (RectTransform)badge.transform;
            bRt.SetParent(cardRt, false);
            bRt.anchorMin = bRt.anchorMax = bRt.pivot = new Vector2(0.5f, 1f);
            bRt.sizeDelta = new Vector2(64f, 64f);
            bRt.anchoredPosition = new Vector2(0f, -40f);
            var bImg = badge.GetComponent<Image>();
            if (circleSprite != null) { bImg.sprite = circleSprite; bImg.preserveAspect = true; }
            bImg.color = blush;

            if (modeSprite != null)
            {
                var mGo = new GameObject("ModeIcon", typeof(RectTransform), typeof(Image));
                var mRt = (RectTransform)mGo.transform;
                mRt.SetParent(bRt, false);
                mRt.anchorMin = mRt.anchorMax = new Vector2(0.5f, 0.5f);
                mRt.sizeDelta = new Vector2(34f, 34f);
                var mImg = mGo.GetComponent<Image>();
                mImg.sprite = modeSprite;
                mImg.preserveAspect = true;
            }
            if (lockSprite != null)
            {
                var lGo = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
                var lRt = (RectTransform)lGo.transform;
                lRt.SetParent(bRt, false);
                lRt.anchorMin = lRt.anchorMax = new Vector2(1f, 0f);
                lRt.sizeDelta = new Vector2(24f, 24f);
                lRt.anchoredPosition = new Vector2(-4f, 4f);
                var lImg = lGo.GetComponent<Image>();
                lImg.sprite = lockSprite;
                lImg.preserveAspect = true;
            }

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.SetParent(cardRt, false);
            bodyRt.anchorMin = new Vector2(0f, 0f); bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(28f, 78f); bodyRt.offsetMax = new Vector2(-28f, -112f);
            var bodyTmp = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyTmp.font = bodyFont;
            bodyTmp.fontSize = 18f;
            bodyTmp.fontSizeMin = 13f;
            bodyTmp.enableAutoSizing = true;
            bodyTmp.color = plumMuted;
            bodyTmp.alignment = TextAlignmentOptions.Center;
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            bodyTmp.text = isChef
                ? (es ? "Completa una ronda de Turno de tarde para abrir el Modo Chef." : "Finish an Afternoon shift run to unlock Chef Mode.")
                : (es ? "Completa los 3 capítulos de Historia para abrir el Turno de tarde." : "Finish all 3 Story chapters to unlock Afternoon shift.");

            var okGo = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var okRt = (RectTransform)okGo.transform;
            okRt.SetParent(cardRt, false);
            okRt.anchorMin = okRt.anchorMax = okRt.pivot = new Vector2(0.5f, 0f);
            okRt.sizeDelta = new Vector2(210f, 48f);
            okRt.anchoredPosition = new Vector2(0f, 20f);
            var okImg = okGo.GetComponent<Image>();
            if (berrySprite != null) { okImg.sprite = berrySprite; okImg.type = Image.Type.Sliced; okImg.pixelsPerUnitMultiplier = 1.6f; }
            else okImg.color = plum;
            var okBtn = okGo.GetComponent<Button>();
            okBtn.targetGraphic = okImg;
            okBtn.onClick.AddListener(CloseLockedModePopup);

            var okLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var okLblRt = (RectTransform)okLblGo.transform;
            okLblRt.SetParent(okRt, false);
            okLblRt.anchorMin = Vector2.zero; okLblRt.anchorMax = Vector2.one;
            okLblRt.offsetMin = new Vector2(12f, 4f); okLblRt.offsetMax = new Vector2(-12f, -4f);
            var okTmp = okLblGo.GetComponent<TextMeshProUGUI>();
            okTmp.font = dispFont;
            okTmp.fontSize = 20f;
            okTmp.color = Color.white;
            okTmp.alignment = TextAlignmentOptions.Center;
            okTmp.text = es ? "¡Entendido!" : "Got it!";

            if (!INART.SlothyBakery.CanvasAnimator.CanvasAnimator.SkipForTests)
            {
                cardRt.localScale = Vector3.one * 0.85f;
                cardCg.alpha = 0f;
                DOTween.Sequence().SetUpdate(true).SetLink(root)
                    .Append(cardCg.DOFade(1f, 0.16f))
                    .Join(cardRt.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 1.6f));
            }
        }

        private void CloseLockedModePopup()
        {
            if (_lockedPopup != null)
            {
                Destroy(_lockedPopup);
                _lockedPopup = null;
            }
        }

        private static bool IsSpanish()
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale == null || locale.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }
    }
}
