using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>Loads short, runtime-composed minigame prompts from the BakeryShell table.</summary>
    public static class BakeryPromptLocalization
    {
        public const string Table = "BakeryShell";

        public static void SetText(TMP_Text label, string value)
        {
            if (label == null) return;
            var animator = label.GetComponent<Febucci.UI.TextAnimator>();
            if (animator != null) animator.SyncText(value, false);
            else label.text = value;
        }

        /// <summary>Returns true when Unity Localization failed to resolve an authored entry.</summary>
        public static bool IsMissingTranslation(string value, string key)
            => string.IsNullOrWhiteSpace(value)
               || value == key
               || value.StartsWith("No translation found", StringComparison.Ordinal);

        /// <summary>Synchronously resolves a BakeryShell string or returns <paramref name="fallback"/> if missing.</summary>
        public static string GetLocalizedOrFallback(string key, string fallback, string table = Table)
        {
            try
            {
                string value = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
                return IsMissingTranslation(value, key) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }

        public static IEnumerator Resolve(string key, string fallback, Action<string> onResolved)
        {
            var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(Table, key);
            yield return operation;

            string value = operation.Status == AsyncOperationStatus.Succeeded
                ? operation.Result
                : null;
            onResolved?.Invoke(IsMissingTranslation(value, key) ? fallback : value);
        }
    }
}
