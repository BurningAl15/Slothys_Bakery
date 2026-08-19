using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using INART.SlothyBakery.Core;

namespace INART.Framework.Core
{
    /// <summary>
    /// Infrastructure manager for Unity Localization (com.unity.localization).
    ///
    /// ═══════════════════════════════════════════════════════════════════
    ///  PACKAGE SETUP (one-time):
    ///    Window → Package Manager → Add by name → com.unity.localization
    ///
    ///  EDITOR SETUP (one-time per project):
    ///    1. Edit → Project Settings → Localization
    ///       - Add locales: "es" (Spanish) and "en" (English) at minimum.
    ///       - Set Default Locale to "es" (primary language for SlothysBakery).
    ///       - Enable "Initialize Synchronously" to avoid a loading frame.
    ///
    ///    2. Create a String Table Collection:
    ///       Assets → Create → Localization → String Table Collection
    ///       Name it "GamePhrases" (must match TABLE_GAME_PHRASES const).
    ///
    ///    3. In the String Table, add keys for every text string in the game.
    ///       Recommended naming convention:
    ///         intro_phrase_0, intro_phrase_1   ← CanvasAnimator intro dialog
    ///         congrat_phrase_0                 ← mid-game congratulation
    ///         end_phrase_0                     ← final dialog
    ///         notification_title               ← push notification title
    ///         notification_body                ← push notification body
    ///         ui_new_game, ui_continue         ← menu buttons
    ///
    ///    4. Add translations for each locale in the String Table editor.
    ///
    ///  HOW TO ADD A NEW LANGUAGE:
    ///    1. Project Settings → Localization → Add locale (e.g. "fr").
    ///    2. Open each String Table Collection and fill in the "fr" column.
    ///    Done — no code changes needed.
    /// ═══════════════════════════════════════════════════════════════════
    ///
    ///  RUNTIME USAGE:
    ///    // Sync (only reliable if "Preload All Tables" is enabled)
    ///    string s = LocalizationManager.Instance.GetString("intro_phrase_0");
    ///
    ///    // Async (safe in all cases)
    ///    yield return StartCoroutine(
    ///        LocalizationManager.Instance.GetStringAsync("intro_phrase_0", v => text = v));
    ///
    ///    // Change language at runtime
    ///    SettingsManager.Instance.Language = "fr";   ← triggers OnLanguageChanged
    ///    // LocalizationManager picks it up automatically.
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        // ── Table names — update here if you rename the String Table assets ───
        public const string TABLE_GAME_PHRASES   = "GamePhrases";
        public const string TABLE_UI             = "UI";
        public const string TABLE_NOTIFICATIONS  = "Notifications";

        // ── Runtime state ──────────────────────────────────────────────────────
        private bool _isInitialized;

        /// <summary>
        /// True once Unity Localization has finished its async initialization.
        /// Check this before calling GetString() without async.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return LocalizationSettings.InitializationOperation;
            _isInitialized = true;

            // Priority order:
            // 1. Language saved by the player (SettingsManager)
            // 2. Device system language
            // 3. Fallback → "en" (project default locale)
            if (SettingsManager.Instance != null
                && !string.IsNullOrEmpty(SettingsManager.Instance.Language))
            {
                // Player has a saved preference — apply it
                ApplyLocale(SettingsManager.Instance.Language);
                SettingsManager.Instance.OnLanguageChanged += ApplyLocale;
            }
            else
            {
                // No saved preference → try device language, Unity Localization
                // already handles this via SystemLocaleSelector in Project Settings.
                // We just save whatever locale Unity picked so it persists next session.
                var picked = LocalizationSettings.SelectedLocale;
                if (picked != null && SettingsManager.Instance != null)
                    SettingsManager.Instance.Language = picked.Identifier.Code;

                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.OnLanguageChanged += ApplyLocale;
            }

            GameLogger.Log(
                $"LocalizationManager ready. Active locale: " +
                $"'{LocalizationSettings.SelectedLocale?.LocaleName ?? "none"}'.",
                LogCategory.System);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnLanguageChanged -= ApplyLocale;
        }

        // ── Locale control ─────────────────────────────────────────────────────

        /// <summary>
        /// Changes the active locale. Called automatically when
        /// SettingsManager.Language changes. Can also be called directly.
        /// <para>
        /// <paramref name="localeCode"/> must be a valid BCP-47 code matching
        /// one of the locales added in Project Settings → Localization (e.g. "es", "en", "fr").
        /// </para>
        /// </summary>
        public void ApplyLocale(string localeCode)
        {
            if (!_isInitialized)
            {
                GameLogger.Warning(
                    $"LocalizationManager.ApplyLocale('{localeCode}'): not yet initialized.",
                    LogCategory.System);
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                GameLogger.Warning(
                    $"LocalizationManager: locale '{localeCode}' not found in Available Locales. " +
                    "Add it in Edit → Project Settings → Localization.",
                    LogCategory.System);
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
            GameLogger.Log(
                $"LocalizationManager: locale changed to '{localeCode}'.",
                LogCategory.System);
        }

        /// <summary>Returns the BCP-47 code of the currently active locale (e.g. "es").</summary>
        public string CurrentLocaleCode =>
            LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";

        // ── String retrieval ───────────────────────────────────────────────────

        /// <summary>
        /// Synchronous string lookup. Safe when "Preload All Tables" is enabled in
        /// Project Settings → Localization, or when called after the table has already
        /// been loaded. Falls back to <paramref name="key"/> on any error so the game
        /// never crashes due to a missing translation.
        /// </summary>
        public string GetString(string key, string table = TABLE_GAME_PHRASES)
        {
            if (!_isInitialized) return key;

            try
            {
                var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
                if (op.IsDone && op.Result != null) return op.Result;

                GameLogger.Warning(
                    $"LocalizationManager.GetString: '{table}/{key}' not preloaded. " +
                    "Use GetStringAsync() or enable 'Preload All Tables'.",
                    LogCategory.System);
                return key;
            }
            catch (Exception ex)
            {
                GameLogger.Warning(
                    $"LocalizationManager.GetString: '{table}/{key}' failed — {ex.Message}.",
                    LogCategory.System);
                return key;
            }
        }

        /// <summary>
        /// Async string lookup. Always safe, even if the table isn't preloaded.
        /// Usage:
        /// <code>
        ///   yield return StartCoroutine(
        ///       LocalizationManager.Instance.GetStringAsync("intro_phrase_0", s => myText = s));
        /// </code>
        /// </summary>
        public IEnumerator GetStringAsync(string key, Action<string> onResult,
            string table = TABLE_GAME_PHRASES)
        {
            if (!_isInitialized)
                yield return new WaitUntil(() => _isInitialized);

            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
            yield return op;
            onResult?.Invoke(op.IsDone && op.Result != null ? op.Result : key);
        }

        /// <summary>
        /// Returns N localized strings for keys named <c>prefix_0</c>, <c>prefix_1</c> …
        /// <c>prefix_{count-1}</c>. Synchronous — see <see cref="GetString"/> caveats.
        ///
        /// Used by <c>LocalizedDialogSO.GetPhrases()</c> to rebuild a phrase list
        /// for the TypeWriterPresenter.
        /// </summary>
        public List<string> GetPhraseList(string keyPrefix, int count,
            string table = TABLE_GAME_PHRASES)
        {
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
                result.Add(GetString($"{keyPrefix}_{i}", table));
            return result;
        }

        /// <summary>
        /// Async version of <see cref="GetPhraseList"/>. Resolves all strings in
        /// sequence and returns the full list via callback.
        /// </summary>
        public IEnumerator GetPhraseListAsync(string keyPrefix, int count,
            Action<List<string>> onResult, string table = TABLE_GAME_PHRASES)
        {
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string resolved = keyPrefix; // fallback
                yield return StartCoroutine(
                    GetStringAsync($"{keyPrefix}_{i}", s => resolved = s, table));
                result.Add(resolved);
            }
            onResult?.Invoke(result);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Log Active Locale")]
        private void DebugLogLocale()
        {
            Debug.Log($"[LocalizationManager] Initialized={_isInitialized} | " +
                      $"Locale={LocalizationSettings.SelectedLocale?.LocaleName ?? "none"}");
        }
#endif
    }
}
