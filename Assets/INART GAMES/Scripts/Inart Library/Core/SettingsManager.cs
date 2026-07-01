using System;
using UnityEngine;
using INART.SlothyBakery.Core;
using INART.Framework.Core.Services;

namespace INART.Framework.Core
{
    /// <summary>
    /// Centralized settings manager for all user preferences.
    /// Persists everything in PlayerPrefs and exposes typed properties
    /// with change-notification events so any UI or system can react.
    ///
    /// Categories:
    ///   Audio       — Master / Music / SFX volumes + Mute toggles
    ///   Gameplay    — Language, vibration, screen-always-on
    ///   Accessibility — Text size, high-contrast, reduced motion, colorblind mode
    ///   Notifications — Push notification opt-in
    ///   Privacy / Ads — Consent flags, personalized-ad toggle
    ///   Parental Controls — Age gate, content rating
    ///
    /// Usage:
    ///   SettingsManager.Instance.MusicVolume = 0.8f;
    ///   SettingsManager.Instance.OnMusicVolumeChanged += v => slider.value = v;
    /// </summary>
    public class SettingsManager : Singleton<SettingsManager>
    {
        // ── Persistence across scenes ──────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ─────────────────────────────────────────────────────────────────────
        // EVENTS — subscribe from UI panels or any system that needs to react
        // ─────────────────────────────────────────────────────────────────────
        public event Action<float>  OnMasterVolumeChanged;
        public event Action<float>  OnMusicVolumeChanged;
        public event Action<float>  OnSFXVolumeChanged;
        public event Action<bool>   OnMuteChanged;
        public event Action<string> OnLanguageChanged;
        public event Action<bool>   OnVibrationChanged;
        public event Action<bool>   OnNotificationsChanged;
        public event Action<bool>   OnAdsConsentChanged;
        public event Action<bool>   OnPersonalizedAdsChanged;
        public event Action<float>  OnTextSizeChanged;
        public event Action<bool>   OnHighContrastChanged;
        public event Action<bool>   OnReducedMotionChanged;
        public event Action<int>    OnColorblindModeChanged;
        public event Action         OnSettingsReset;

        // ─────────────────────────────────────────────────────────────────────
        // PLAYERPREFS KEYS — never hardcode strings outside this class
        // ─────────────────────────────────────────────────────────────────────
        private static class Keys
        {
            // Audio
            public const string MasterVolume   = "cfg_audio_master";
            public const string MusicVolume    = "cfg_audio_music";
            public const string SFXVolume      = "cfg_audio_sfx";
            public const string Mute           = "cfg_audio_mute";
            // Gameplay
            public const string Language       = "cfg_gameplay_lang";
            public const string Vibration      = "cfg_gameplay_vib";
            public const string ScreenAlwaysOn = "cfg_gameplay_screen_on";
            // Accessibility
            public const string TextSize       = "cfg_access_textsize";
            public const string HighContrast   = "cfg_access_highcontrast";
            public const string ReducedMotion  = "cfg_access_reducedmotion";
            public const string ColorblindMode = "cfg_access_colorblind";
            // Notifications
            public const string Notifications  = "cfg_notif_enabled";
            // Privacy / Ads
            public const string AdsConsent         = "cfg_privacy_adsconsent";
            public const string PersonalizedAds     = "cfg_privacy_personalizedads";
            // Parental
            public const string ParentalLock   = "cfg_parental_lock";
            public const string AgeVerified    = "cfg_parental_ageverified";
            // Meta
            public const string FirstLaunch    = "cfg_meta_firstlaunch";
            public const string SettingsVersion = "cfg_meta_version";
        }

        private const int CURRENT_SETTINGS_VERSION = 1;

        // ─────────────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            MigrateIfNeeded();
            ApplyAllToSystems();
            GameLogger.Log("SettingsManager loaded.", LogCategory.Settings);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── AUDIO ─────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Master volume (0–1). Applies to the AudioMixer/AudioListener.</summary>
        public float MasterVolume
        {
            get => PlayerPrefs.GetFloat(Keys.MasterVolume, 1f);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(Keys.MasterVolume, value);
                ApplyMasterVolume(value);
                OnMasterVolumeChanged?.Invoke(value);
            }
        }

        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat(Keys.MusicVolume, 0.7f);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(Keys.MusicVolume, value);
                ApplyMusicVolume(value);
                OnMusicVolumeChanged?.Invoke(value);
            }
        }

        public float SFXVolume
        {
            get => PlayerPrefs.GetFloat(Keys.SFXVolume, 0.9f);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(Keys.SFXVolume, value);
                ApplySFXVolume(value);
                OnSFXVolumeChanged?.Invoke(value);
            }
        }

        /// <summary>Mutes / unmutes all audio without changing stored volume values.</summary>
        public bool IsMuted
        {
            get => PlayerPrefs.GetInt(Keys.Mute, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.Mute, value ? 1 : 0);
                AudioListener.pause = value;
                OnMuteChanged?.Invoke(value);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── GAMEPLAY ──────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// BCP-47 language code e.g. "es", "en", "fr".
        /// Fires OnLanguageChanged so LocalizationManager can react.
        /// </summary>
        public string Language
        {
            get => PlayerPrefs.GetString(Keys.Language, Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en");
            set
            {
                PlayerPrefs.SetString(Keys.Language, value);
                OnLanguageChanged?.Invoke(value);
                GameLogger.Log($"Language set to '{value}'.", LogCategory.Settings);
            }
        }

        /// <summary>Haptic feedback on supported devices.</summary>
        public bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(Keys.Vibration, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.Vibration, value ? 1 : 0);
                OnVibrationChanged?.Invoke(value);
            }
        }

        /// <summary>Prevents the screen from sleeping during gameplay.</summary>
        public bool ScreenAlwaysOn
        {
            get => PlayerPrefs.GetInt(Keys.ScreenAlwaysOn, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.ScreenAlwaysOn, value ? 1 : 0);
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── ACCESSIBILITY ─────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// UI text scale multiplier (0.8 = smaller, 1.0 = default, 1.4 = larger).
        /// Fire OnTextSizeChanged and let individual UI elements react.
        /// </summary>
        public float TextSize
        {
            get => PlayerPrefs.GetFloat(Keys.TextSize, 1f);
            set
            {
                value = Mathf.Clamp(value, 0.8f, 1.6f);
                PlayerPrefs.SetFloat(Keys.TextSize, value);
                OnTextSizeChanged?.Invoke(value);
            }
        }

        /// <summary>High-contrast mode — increase outline/shadow on text and UI elements.</summary>
        public bool HighContrast
        {
            get => PlayerPrefs.GetInt(Keys.HighContrast, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.HighContrast, value ? 1 : 0);
                OnHighContrastChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Reduced motion — skips non-essential animations (character intro, bouncing, etc.).
        /// Useful for players with vestibular disorders or for faster gameplay flow.
        /// </summary>
        public bool ReducedMotion
        {
            get => PlayerPrefs.GetInt(Keys.ReducedMotion, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.ReducedMotion, value ? 1 : 0);
                OnReducedMotionChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Colorblind mode.
        ///   0 = None
        ///   1 = Deuteranopia (red-green)
        ///   2 = Protanopia   (red-green, different)
        ///   3 = Tritanopia   (blue-yellow)
        /// </summary>
        public int ColorblindMode
        {
            get => PlayerPrefs.GetInt(Keys.ColorblindMode, 0);
            set
            {
                value = Mathf.Clamp(value, 0, 3);
                PlayerPrefs.SetInt(Keys.ColorblindMode, value);
                OnColorblindModeChanged?.Invoke(value);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── NOTIFICATIONS ─────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether the player opted in to push notifications.
        /// The NotificationManager reads this before scheduling anything.
        /// </summary>
        public bool NotificationsEnabled
        {
            get => PlayerPrefs.GetInt(Keys.Notifications, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.Notifications, value ? 1 : 0);
                OnNotificationsChanged?.Invoke(value);
                GameLogger.Log($"Notifications {(value ? "enabled" : "disabled")}.", LogCategory.Settings);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── PRIVACY / ADS ─────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// General ads consent (GDPR / COPPA gate).
        /// Set to true only after the user explicitly accepts the privacy policy.
        /// The AdsManager reads this before showing any ad.
        /// </summary>
        public bool AdsConsent
        {
            get => PlayerPrefs.GetInt(Keys.AdsConsent, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.AdsConsent, value ? 1 : 0);
                OnAdsConsentChanged?.Invoke(value);
                GameLogger.Log($"Ads consent: {value}.", LogCategory.Settings);
            }
        }

        /// <summary>
        /// Whether to show personalized (targeted) ads vs generic ads.
        /// Only relevant if AdsConsent is true.
        /// For apps targeting children (COPPA) this should always be false.
        /// </summary>
        public bool PersonalizedAdsEnabled
        {
            get => PlayerPrefs.GetInt(Keys.PersonalizedAds, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Keys.PersonalizedAds, value ? 1 : 0);
                OnPersonalizedAdsChanged?.Invoke(value);
            }
        }

        /// <summary>True if the user purchased "Remove Ads". Read by AdsManager.</summary>
        public bool AdsRemoved
        {
            get => PlayerPrefs.GetInt("iap_ads_removed", 0) == 1;
            set => PlayerPrefs.SetInt("iap_ads_removed", value ? 1 : 0);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── PARENTAL CONTROLS ─────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Parent-set 4-digit PIN hash. Empty string means no lock is set.
        /// Store the SHA-256 hash, never the raw PIN.
        /// </summary>
        public string ParentalLockHash
        {
            get => PlayerPrefs.GetString(Keys.ParentalLock, "");
            set => PlayerPrefs.SetString(Keys.ParentalLock, value);
        }

        public bool IsParentalLockSet => !string.IsNullOrEmpty(ParentalLockHash);

        /// <summary>True once a parent/guardian confirmed the age gate.</summary>
        public bool AgeVerified
        {
            get => PlayerPrefs.GetInt(Keys.AgeVerified, 0) == 1;
            set => PlayerPrefs.SetInt(Keys.AgeVerified, value ? 1 : 0);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── META ──────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>True the very first time the app launches (before any settings are saved).</summary>
        public bool IsFirstLaunch
        {
            get => PlayerPrefs.GetInt(Keys.FirstLaunch, 1) == 1;
            set => PlayerPrefs.SetInt(Keys.FirstLaunch, value ? 1 : 0);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── RESET ─────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resets all settings to factory defaults and fires OnSettingsReset.
        /// Does NOT delete game progress (that's SaveManager's job).
        /// </summary>
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(Keys.MasterVolume);
            PlayerPrefs.DeleteKey(Keys.MusicVolume);
            PlayerPrefs.DeleteKey(Keys.SFXVolume);
            PlayerPrefs.DeleteKey(Keys.Mute);
            PlayerPrefs.DeleteKey(Keys.Language);
            PlayerPrefs.DeleteKey(Keys.Vibration);
            PlayerPrefs.DeleteKey(Keys.ScreenAlwaysOn);
            PlayerPrefs.DeleteKey(Keys.TextSize);
            PlayerPrefs.DeleteKey(Keys.HighContrast);
            PlayerPrefs.DeleteKey(Keys.ReducedMotion);
            PlayerPrefs.DeleteKey(Keys.ColorblindMode);
            PlayerPrefs.DeleteKey(Keys.Notifications);
            PlayerPrefs.DeleteKey(Keys.AdsConsent);
            PlayerPrefs.DeleteKey(Keys.PersonalizedAds);
            PlayerPrefs.Save();

            ApplyAllToSystems();
            OnSettingsReset?.Invoke();
            GameLogger.Log("Settings reset to defaults.", LogCategory.Settings);
        }

        /// <summary>Force a PlayerPrefs.Save(). Call when the settings panel closes.</summary>
        public void Save() => PlayerPrefs.Save();

        // ─────────────────────────────────────────────────────────────────────
        // ── APPLY HELPERS (push stored values to Unity systems) ───────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Pushes all stored settings to Unity systems on startup.</summary>
        private void ApplyAllToSystems()
        {
            ApplyMasterVolume(MasterVolume);
            ApplyMusicVolume(MusicVolume);
            ApplySFXVolume(SFXVolume);
            AudioListener.pause = IsMuted;
            Screen.sleepTimeout = ScreenAlwaysOn ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

        private void ApplyMasterVolume(float v)
        {
            if (ServiceLocator.Has<IAudioService>())
                ServiceLocator.Get<IAudioService>().SetMasterVolume(v);
            else
                AudioListener.volume = v;
        }

        private void ApplyMusicVolume(float v)
        {
            if (ServiceLocator.Has<IAudioService>())
                ServiceLocator.Get<IAudioService>().SetMusicVolume(v);
        }

        private void ApplySFXVolume(float v)
        {
            if (ServiceLocator.Has<IAudioService>())
                ServiceLocator.Get<IAudioService>().SetSFXVolume(v);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── MIGRATION ─────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Run when the stored settings version doesn't match the current one.
        /// Add migration cases here as the schema evolves.
        /// </summary>
        private void MigrateIfNeeded()
        {
            int storedVersion = PlayerPrefs.GetInt(Keys.SettingsVersion, 0);
            if (storedVersion == CURRENT_SETTINGS_VERSION) return;

            // v0 → v1: no prior settings existed, just write current version
            if (storedVersion == 0)
                GameLogger.Log("First-time settings setup.", LogCategory.Settings);

            PlayerPrefs.SetInt(Keys.SettingsVersion, CURRENT_SETTINGS_VERSION);
            PlayerPrefs.Save();
        }
    }
}
