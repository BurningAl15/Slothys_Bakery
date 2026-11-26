using FatCat.Localization.UI;
using FatCat.Mobile;
using FatCat.SettingsUI;
using FatCat.Share;
using INART.Framework.Core;
using INART.SlothyBakery.Notifications;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Slothy's Bakery backend for the shared Settings screen (com.fatcat.settings-ui).
    /// Ids: volumes "music", "effects"; switches "vibration", "reminders";
    /// shortcuts "language", "achievements", "rate", "share", "more_games".
    /// </summary>
    public sealed class BakerySettingsBackend : SettingsBackendBehaviour
    {
        const string VibrationKey = "cfg_gameplay_vib";
        const string RemindersKey = "cfg_notif_enabled";

        [SerializeField] private LocalePickerController languagePicker;
        [SerializeField] private BakeryLanguageGate languageGate;
        [SerializeField] private BakeryModesView modesView;

        public static bool VibrationEnabled
        {
            get
            {
                var mgr = FindAnyObjectByType<SettingsManager>();
                return mgr != null ? mgr.VibrationEnabled : PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            }
        }

        public static bool RemindersEnabled
        {
            get
            {
                var mgr = FindAnyObjectByType<SettingsManager>();
                return mgr != null ? mgr.NotificationsEnabled : PlayerPrefs.GetInt(RemindersKey, 1) == 1;
            }
        }

        public override float GetVolume(string id) =>
            id == "music" ? BakeryAudioSettings.Music : BakeryAudioSettings.Effects;

        public override void SetVolume(string id, float value)
        {
            if (id == "music") BakeryAudioSettings.SetMusic(value);
            else if (id == "effects") BakeryAudioSettings.SetEffects(value);
        }

        public override bool GetSwitch(string id)
        {
            if (id == "vibration") return VibrationEnabled;
            if (id == "reminders") return RemindersEnabled;
            return false;
        }

        public override void SetSwitch(string id, bool on)
        {
            var mgr = FindAnyObjectByType<SettingsManager>();
            if (id == "vibration")
            {
                if (mgr != null) mgr.VibrationEnabled = on;
                else PlayerPrefs.SetInt(VibrationKey, on ? 1 : 0);
                PlayerPrefs.Save();
#if !UNITY_EDITOR
                if (on) FatcatHaptics.PlayMediumImpact();
#endif
                return;
            }

            if (id == "reminders")
            {
                if (on)
                {
                    NotificationManager.RequestEnable(_ =>
                    {
                        var view = FindAnyObjectByType<SettingsScreenView>();
                        if (view != null) view.Refresh();
                    });
                }
                else if (mgr != null) mgr.NotificationsEnabled = false;
                else
                {
                    PlayerPrefs.SetInt(RemindersKey, 0);
                    PlayerPrefs.Save();
                }
            }
        }

        public override string GetChoice(string id) => null;
        public override void SetChoice(string id, string value) { }

        public override void OnShortcut(string id)
        {
            switch (id)
            {
                case "language":
                    if (languageGate != null) languageGate.OpenPicker();
                    else if (languagePicker != null) languagePicker.Open();
                    break;
                case "achievements":
                    if (modesView != null) modesView.OpenAchievements();
                    break;
                case "rate":
                    BakeryParentalGate.Ask(() => FatCat.Mobile.StoreReview.OpenWriteReview(null, Application.identifier));
                    break;
                case "share":
                    BakeryParentalGate.Ask(ShareGame);
                    break;
                case "more_games":
                    if (modesView != null) modesView.OpenMoreGames();
                    else BakeryParentalGate.Ask(OpenStorePage);
                    break;
                case "educator_feedback":
                    BakeryParentalGate.Ask(OpenEducatorFeedback);
                    break;
                default:
                    Debug.Log("[BakerySettingsBackend] Unhandled shortcut: " + id);
                    break;
            }
        }

        private static void OpenEducatorFeedback()
        {
            bool es = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale == null
                   || UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("es");
            string subject = es ? "Ideas para Slothy's Bakery" : "Ideas for Slothy's Bakery";
            string body = es
                ? "¿Qué te gustaría enseñar?\n\n¿Qué idea o problema quieres compartir?\n\n¿Para qué edad o grado?\n\nPor favor, no incluyas nombres ni datos de alumnos."
                : "What would you like to teach?\n\nWhat idea or problem would you like to share?\n\nWhat age or grade is it for?\n\nPlease do not include students' names or personal information.";
            string url = "mailto:aldhairvera15@gmail.com?subject=" + System.Uri.EscapeDataString(subject) +
                "&body=" + System.Uri.EscapeDataString(body);
            Application.OpenURL(url);
        }

        private static void ShareGame()
        {
            bool es = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale == null
                   || UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("es");
            string title = "Slothy's Bakery";
            string text = es
                ? "¡Aprende fracciones horneando con Slothy!"
                : "Learn fractions baking with Slothy!";
            string url = Application.platform == RuntimePlatform.IPhonePlayer
                ? null
                : FatCat.Mobile.StoreReview.StoreUrl(null, Application.identifier, android: true);
            FatcatShare.ShareText(title, text, url);
        }

        private static void OpenStorePage()
        {
            string url = Application.platform == RuntimePlatform.IPhonePlayer
                ? "https://apps.apple.com/developer/fatcat-game-studio/id1775797699"
                : "https://play.google.com/store/apps/dev?id=7901842880017361877";
            Application.OpenURL(url);
        }

        public override void OnOpening() => BakeryAudioSettings.ApplySaved();
    }
}
