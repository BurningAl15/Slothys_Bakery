using System;
using Fatcat.Slothy.Bakery.Modes;
using Fatcat.Slothy.Core.ShellUI;
using INART.SlothyBakery.SceneTransition;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Host wrapper connecting shared <see cref="SlothyDiplomaView"/> to Slothy's Bakery stats,
    /// player name PlayerPrefs, BakeryParentalGate, and FatcatShare.
    /// </summary>
    public static class BakeryDiploma
    {
        public const string ResourcePath = "UI/BakeryDiploma";
        private const string PlayerNameKey = "slothys_bakery_player_name";
        private static readonly string[] SkillScenes =
        {
            SceneUtils.FirstMinigameScene,
            SceneUtils.SecondMinigameScene,
            SceneUtils.ThirdMinigameScene,
        };

        private static SlothyDiplomaView _instance;
        public static Func<DateTime> Today = () => DateTime.Today;
        public static bool IsOpen => _instance != null && _instance.IsOpen;

        public static string SavedName
        {
            get => PlayerPrefs.GetString(PlayerNameKey, "").Trim();
            set
            {
                string clean = (value ?? "").Trim();
                if (clean.Length > 22) clean = clean.Substring(0, 22);
                PlayerPrefs.SetString(PlayerNameKey, clean);
                PlayerPrefs.Save();
            }
        }

        public static void Open()
        {
            if (_instance == null)
            {
                var prefab = Resources.Load<SlothyDiplomaView>(ResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning("[BakeryDiploma] Missing " + ResourcePath);
                    return;
                }
                _instance = UnityEngine.Object.Instantiate(prefab);
                _instance.name = "BakeryDiploma";
                UnityEngine.Object.DontDestroyOnLoad(_instance.gameObject);
                _instance.ConfigureActions(
                    name => SavedName = name,
                    saving => BakeryParentalGate.Ask(() => CaptureAndShare(saving)));
            }

            _instance.Open(BuildData());
        }

        public static void Close()
        {
            if (_instance != null) _instance.Hide();
        }

        private static SlothyDiplomaView.DiplomaData BuildData()
        {
            bool es = IsSpanish();
            bool finishedStory = BakeryStats.StoryFinished || BakerySession.AfternoonUnlocked;
            var stars = new int[SkillScenes.Length];
            for (int i = 0; i < SkillScenes.Length; i++)
            {
                int s = BakeryStats.BestStars(SkillScenes[i]);
                if (s == 0 && finishedStory) s = 1;
                stars[i] = s;
            }

            var culture = LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.CultureInfo
                : null;
            string dateText = culture != null ? Today().ToString("D", culture) : Today().ToString("D");

            return new SlothyDiplomaView.DiplomaData
            {
                Kicker = T("diploma.kicker", es ? "DIPLOMA DE" : "CERTIFICATE"),
                Title = T("diploma.title", es ? "Asistente estrella" : "Star assistant"),
                Awarded = T("diploma.awarded", es ? "Otorgado a" : "Awarded to"),
                NameHint = T("diploma.name_hint", es ? "Toca y escribe tu nombre" : "Tap to write your name"),
                PlayerName = SavedName,
                SkillNames = new[]
                {
                    T("modes.mg1", es ? "Comparar pasos" : "Step comparison"),
                    T("modes.mg2", es ? "Elegir signo" : "Choose sign"),
                    T("modes.mg3", es ? "Escribir fracción" : "Write fraction"),
                },
                SkillStars = stars,
                FooterGame = "Slothy's Bakery · Fat Cat Game Studio",
                FooterDate = dateText,
                StoreTitle = T("diploma.store", es ? "¡Juega con Slothy!" : "Play with Slothy!"),
                StoreSub = "Google Play · App Store",
                ShareLabel = T("diploma.share", es ? "Compartir" : "Share"),
                SaveLabel = T("diploma.save", es ? "Guardar" : "Save"),
                CloseLabel = T("diploma.close", es ? "Cerrar" : "Close"),
            };
        }

        private static void CaptureAndShare(bool saving)
        {
            if (_instance == null || !_instance.gameObject.activeInHierarchy) return;
            bool es = IsSpanish();
            _instance.CapturePaperRoutine(shot =>
            {
                string url = Application.platform == RuntimePlatform.IPhonePlayer
                    ? null
                    : FatCat.Mobile.StoreReview.StoreUrl(null, Application.identifier, android: true);
                string title = saving ? T("diploma.save", es ? "Guardar" : "Save") : T("diploma.share", es ? "Compartir" : "Share");
                string text = T("diploma.share_text", es ? "¡Mi diploma de Slothy's Bakery!" : "My Slothy's Bakery diploma!");
                FatCat.Share.FatcatShare.ShareTexture(shot, "slothys-bakery-diploma", title, text, url);
            });
        }

        private static bool IsSpanish()
        {
            var loc = LocalizationSettings.SelectedLocale;
            return loc == null || loc.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }

        private static string T(string key, string fallback)
            => BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);
    }
}
