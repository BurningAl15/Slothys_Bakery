using System;
using System.Collections;
using UnityEngine;
using INART.Framework.Core;
using UnityEngine.Localization.Settings;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace INART.SlothyBakery.Notifications
{
    using Core;

    /// <summary>
    /// Re-engagement push notification manager (Unity Mobile Notifications 2.4+).
    ///
    /// Behavior:
    ///   App goes to background / quits  → schedules up to three gentle return reminders
    ///   App reopens                      → cancels pending reminders
    ///
    /// Integration with SettingsManager:
    ///   Reads SettingsManager.Instance.NotificationsEnabled before scheduling. OS permission is requested
    ///   only after the player enables reminders from Settings or completes the first daily activity.
    ///
    /// SETUP:
    ///   1. Install "Mobile Notifications" from the Unity Package Manager.
    ///   2. Add this component to your persistent Loader GameObject
    ///      (same one as SettingsManager / AnalyticsManager).
    ///   3. For iOS: add your app icon to the iOS Notification icon set in
    ///      Edit → Project Settings → Mobile Notifications.
    ///   4. Keep the localized title/body entries in the BakeryShell String Table.
    ///   5. Set ANDROID_CHANNEL_ID to something unique per game
    ///      (e.g. "slothys_bakery_main").
    /// </summary>
    public class NotificationManager : Singleton<NotificationManager>
    {
        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Notification Content")]
        [Tooltip("Localization key for the notification title in the 'Notifications' StringTable.")]
        [SerializeField] private string notificationTitleKey = "notif_title";

        [Tooltip("Localization key for the notification body in the 'Notifications' StringTable.")]
        [SerializeField] private string notificationBodyKey  = "notif_body";

        [Tooltip("Fallback title used if LocalizationManager is not ready.")]
        [SerializeField] private string fallbackTitle = "Slothy's Bakery misses you! 🥐";

        [Tooltip("Fallback body used if LocalizationManager is not ready.")]
        [SerializeField] private string fallbackBody  = "New desserts are ready to compare. Come back and help Slothy!";

        [Tooltip("Hours after the player exits before the notification fires.")]
        [SerializeField] private float  delayHours        = 24f;

        [Header("Android")]
        [Tooltip("Must be unique per app. Use snake_case, no spaces.")]
        [SerializeField] private string androidChannelId   = "slothys_bakery_main";
        [SerializeField] private string androidChannelName = "Slothy's Bakery";
        [SerializeField] private string androidChannelDesc = "Recordatorios para volver a jugar Slothy's Bakery";

        // ── PlayerPrefs ────────────────────────────────────────────────────────
        private const string AskedKey = "bakery.reminders.asked";

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            RegisterAndroidChannel();

            // React to the player toggling notifications in Settings at runtime
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnNotificationsChanged += OnNotificationsSettingChanged;
        }

        private void Start()
        {
            // Player just opened the app — cancel any pending reminder
            CancelAllPendingNotifications();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnNotificationsChanged -= OnNotificationsSettingChanged;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                TryScheduleReEngagement();
            else
                CancelAllPendingNotifications();
        }

        private void OnApplicationQuit()
        {
            TryScheduleReEngagement();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Schedules the re-engagement notification if the player hasn't opted out.
        /// Called automatically on pause/quit; expose for manual control if needed.
        /// </summary>
        public void TryScheduleReEngagement()
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.NotificationsEnabled)
            {
                GameLogger.Log("Notifications disabled by user — skipping schedule.",
                    LogCategory.System);
                return;
            }

            ScheduleReEngagementNotification();
        }

        /// <summary>First-daily opt-in, matching Café's reminder consent timing.</summary>
        public static void AskAfterFirstDaily()
        {
            if (PlayerPrefs.GetInt(AskedKey, 0) == 1 || PlayerPrefs.GetInt("cfg_notif_enabled", 1) == 0)
                return;
            PlayerPrefs.SetInt(AskedKey, 1);
            PlayerPrefs.Save();
            RequestEnable();
        }

        /// <summary>Cancels every pending notification this manager scheduled.</summary>
        public void CancelAllPendingNotifications()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
#if UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
            GameLogger.Log("Pending notifications cancelled.", LogCategory.System);
        }

        // ── Settings callback ──────────────────────────────────────────────────

        private void OnNotificationsSettingChanged(bool enabled)
        {
            if (!enabled)
                CancelAllPendingNotifications();

            GameLogger.Log($"Notification setting changed → {enabled}", LogCategory.Settings);
        }

        // ── Localization helpers ───────────────────────────────────────────────

        private string GetTitle()
        {
            string fallback = IsSpanishLocale() ? "¡Slothy te espera en la panadería!" : fallbackTitle;
            return GetLocalizedText("reminder.title", fallback);
        }

        private string GetBody(int index)
        {
            string key = index == 1 ? "reminder.miss" : index == 2 ? "reminder.comeback" : "reminder.daily";
            bool spanish = IsSpanishLocale();
            string fallback = index == 1
                ? spanish ? "Slothy extraña tu ayuda con los pedidos." : "Slothy misses your help with today's orders."
                : index == 2
                    ? spanish ? "La panadería sigue abierta. Slothy guardó algo para ti." : "The bakery is still open. Slothy saved something for you."
                    : spanish ? "Hay una nueva actividad de fracciones para ti. ¿Te animas?" : fallbackBody;
            return GetLocalizedText(key, fallback);
        }

        private static bool IsSpanishLocale()
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale == null || locale.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetLocalizedText(string key, string fallback)
            => Fatcat.Slothy.Bakery.Shell.BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);

        /// <summary>Called by the settings toggle so system permission is requested only after opt-in.</summary>
        public static void RequestEnable(Action<bool> completed = null)
        {
            if (Instance == null)
            {
                completed?.Invoke(false);
                return;
            }
            Instance.StartCoroutine(Instance.RequestPermission(completed));
        }

        private IEnumerator RequestPermission(Action<bool> completed)
        {
            bool granted = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
            {
                var request = new PermissionRequest();
                while (request.Status == PermissionStatus.RequestPending) yield return null;
                granted = request.Status == PermissionStatus.Allowed;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            using (var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false))
            {
                while (!request.IsFinished) yield return null;
                granted = request.Granted;
            }
#endif
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.NotificationsEnabled = granted;
            else
            {
                PlayerPrefs.SetInt("cfg_notif_enabled", granted ? 1 : 0);
                PlayerPrefs.Save();
            }
            completed?.Invoke(granted);
            yield break;
        }

        // ── Internal scheduling ────────────────────────────────────────────────

        private void ScheduleReEngagementNotification()
        {
            var settings = SettingsManager.Instance;
            if (settings != null && !settings.NotificationsEnabled)
            {
                CancelAllPendingNotifications();
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
            {
                if (settings != null) settings.NotificationsEnabled = false;
                return;
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            ScheduleAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
            ScheduleiOS();
#endif
        }

        // ── Android ────────────────────────────────────────────────────────────

        private void RegisterAndroidChannel()
        {
#if UNITY_ANDROID
            var channel = new AndroidNotificationChannel
            {
                Id          = androidChannelId,
                Name        = androidChannelName,
                Description = androidChannelDesc,
                Importance  = Importance.Default
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
            GameLogger.Log($"Android notification channel '{androidChannelId}' registered.",
                LogCategory.System);
#endif
        }

        private void ScheduleAndroid()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            foreach (var plan in BakeryReminderPlan.Series(DateTime.Now,
                         Fatcat.Slothy.Bakery.Modes.BakeryStats.IsDailyCompleted(DateTime.Now)))
            {
                var notification = new AndroidNotification
                {
                    Title = GetTitle(),
                    Text = GetBody(plan.Index),
                    FireTime = plan.FireAt,
                    SmallIcon = "bakery_notif_small",
                    LargeIcon = "bakery_notif_large"
                };
                AndroidNotificationCenter.SendNotification(notification, androidChannelId);
            }
            GameLogger.Log("Android return reminders scheduled for the next three planned dates.", LogCategory.System);
#endif
        }

        // ── iOS ────────────────────────────────────────────────────────────────

#if UNITY_IOS
        private void ScheduleiOS()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            foreach (var plan in BakeryReminderPlan.Series(DateTime.Now,
                         Fatcat.Slothy.Bakery.Modes.BakeryStats.IsDailyCompleted(DateTime.Now)))
            {
                var notification = new iOSNotification
                {
                    Identifier = "slothys_bakery_return_" + plan.Index,
                    Title = GetTitle(),
                    Body = GetBody(plan.Index),
                    ShowInForeground = false,
                    ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                    Trigger = new iOSNotificationCalendarTrigger
                    {
                        Year = plan.FireAt.Year,
                        Month = plan.FireAt.Month,
                        Day = plan.FireAt.Day,
                        Hour = plan.FireAt.Hour,
                        Minute = plan.FireAt.Minute,
                        Repeats = false
                    }
                };
                iOSNotificationCenter.ScheduleNotification(notification);
            }
            GameLogger.Log("iOS return reminders scheduled for the next three planned dates.", LogCategory.System);
        }
#endif
    }
}
