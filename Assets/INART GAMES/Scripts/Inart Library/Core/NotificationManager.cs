using System;
using System.Collections;
using UnityEngine;
using INART.Framework.Core;

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
    ///   App goes to background / quits  → schedules a "come back and play" notification
    ///   App reopens                      → cancels the pending notification
    ///
    /// Integration with SettingsManager:
    ///   Reads SettingsManager.Instance.NotificationsEnabled before scheduling.
    ///   Subscribes to OnNotificationsChanged so it reacts to the player toggling
    ///   the setting at runtime — no scene reload needed.
    ///
    /// SETUP:
    ///   1. Install "Mobile Notifications" from the Unity Package Manager.
    ///   2. Add this component to your persistent Loader GameObject
    ///      (same one as SettingsManager / AnalyticsManager).
    ///   3. For iOS: add your app icon to the iOS Notification icon set in
    ///      Edit → Project Settings → Mobile Notifications.
    ///   4. Customize NotificationTitle, NotificationBody, and DelayHours in Inspector.
    ///   5. Set ANDROID_CHANNEL_ID to something unique per game
    ///      (e.g. "slothys_bakery_main").
    /// </summary>
    public class NotificationManager : Singleton<NotificationManager>
    {
        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Notification Content")]
        [Tooltip("Title shown in the device notification tray.")]
        [SerializeField] private string notificationTitle = "¡Slothy te está esperando! 🦥";

        [Tooltip("Body text of the push notification.")]
        [SerializeField] private string notificationBody  = "El horno de Slothy necesita tu ayuda. ¡Ven a hornear!";

        [Tooltip("Hours after the player exits before the notification fires.")]
        [SerializeField] private float  delayHours        = 24f;

        [Header("Android")]
        [Tooltip("Must be unique per app. Use snake_case, no spaces.")]
        [SerializeField] private string androidChannelId   = "slothys_bakery_main";
        [SerializeField] private string androidChannelName = "Slothy's Bakery";
        [SerializeField] private string androidChannelDesc = "Recordatorios para volver a jugar Slothy's Bakery";

        // ── PlayerPrefs ────────────────────────────────────────────────────────
        private const string KEY_NOTIF_ID = "last_notification_id";

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

#if UNITY_IOS
            StartCoroutine(RequestiOSPermission());
#endif
        }

        private void OnDestroy()
        {
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

        // ── Internal scheduling ────────────────────────────────────────────────

        private void ScheduleReEngagementNotification()
        {
#if UNITY_ANDROID
            ScheduleAndroid();
#elif UNITY_IOS
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

            var notification = new AndroidNotification
            {
                Title     = notificationTitle,
                Text      = notificationBody,
                FireTime  = DateTime.Now.AddHours(delayHours),
                SmallIcon = "icon_0",
                LargeIcon = "icon_1"
            };

            int id = AndroidNotificationCenter.SendNotification(notification, androidChannelId);
            PlayerPrefs.SetInt(KEY_NOTIF_ID, id);
            PlayerPrefs.Save();

            GameLogger.Log($"Android notification scheduled in {delayHours}h (id={id}).",
                LogCategory.System);
#endif
        }

        // ── iOS ────────────────────────────────────────────────────────────────

#if UNITY_IOS
        private IEnumerator RequestiOSPermission()
        {
            var req = new AuthorizationRequest(
                AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound,
                registerForRemoteNotifications: false);

            yield return new WaitUntil(() => req.IsFinished);

            GameLogger.Log($"iOS notification permission: {(req.Granted ? "granted" : "denied")}",
                LogCategory.System);

            // If the player denied, mirror that in SettingsManager
            if (!req.Granted && SettingsManager.Instance != null)
                SettingsManager.Instance.NotificationsEnabled = false;
        }

        private void ScheduleiOS()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();

            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = TimeSpan.FromHours(delayHours),
                Repeats      = false
            };

            var notification = new iOSNotification
            {
                Identifier               = "slothys_bakery_reengage",
                Title                    = notificationTitle,
                Body                     = notificationBody,
                ShowInForeground         = false,
                ForegroundPresentationOption =
                    PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge,
                Trigger                  = trigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
            GameLogger.Log($"iOS notification scheduled in {delayHours}h.", LogCategory.System);
        }
#endif
    }
}
