using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using INART.Framework.Core;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Drop-in replacement for the legacy <c>DialogSO</c>.
    ///
    /// Instead of storing strings directly (which forces hardcoded Spanish),
    /// this SO stores the localization table name and key prefix.
    /// The actual text is resolved at runtime from the active Unity Localization locale.
    ///
    /// ═══════════════════════════════════════════════════════════════════
    ///  CREATE:
    ///    Right-click in Project → Create → INART → Modular → Localized Dialog
    ///
    ///  CONFIGURE (example for the intro dialog):
    ///    Table Collection : GamePhrases
    ///    Key Prefix       : intro_phrase
    ///    Phrase Count     : 2
    ///
    ///    → Resolves keys: "intro_phrase_0", "intro_phrase_1"
    ///    → Returns them in the active locale.
    ///
    ///  STRING TABLE KEYS TO CREATE:
    ///    For SlothysBakery, create these keys in the "GamePhrases" table:
    ///
    ///    Key                  │ es (Spanish)                                    │ en (English)
    ///    ─────────────────────┼─────────────────────────────────────────────────┼──────────────────────────────
    ///    intro_phrase_0       │ ¡Hola! Hoy vamos a practicar fracciones.        │ Hi! Today we'll practice fractions.
    ///    intro_phrase_1       │ Pon el signo correcto entre las fracciones.      │ Place the correct sign between the fractions.
    ///    congrat_phrase_0     │ ¡Muy bien! Sigue así.                            │ Great job! Keep it up.
    ///    end_phrase_0         │ ¡Felicidades! Ya eres un experto en fracciones.  │ Congratulations! You're now a fractions expert.
    /// ═══════════════════════════════════════════════════════════════════
    /// </summary>
    [CreateAssetMenu(
        menuName = "INART/Modular/Localized Dialog",
        fileName = "LocalizedDialog_New",
        order    = 11)]
    public class LocalizedDialogSO : ScriptableObject
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("Name of the String Table Collection asset.\n" +
                 "Must match exactly (e.g. 'GamePhrases').")]
        public string tableCollection = LocalizationManager.TABLE_GAME_PHRASES;

        [Tooltip("Prefix used to build the key sequence.\n" +
                 "e.g. 'intro_phrase' → keys intro_phrase_0, intro_phrase_1 …")]
        public string keyPrefix = "intro_phrase";

        [Tooltip("How many phrases this dialog has (determines the key range 0…N-1).")]
        [Min(1)]
        public int phraseCount = 1;

        [Tooltip("Optional fallback phrases used when LocalizationManager is not ready " +
                 "(e.g. in Editor without localization package, or during init).\n" +
                 "Should be in the project's primary language (Spanish for SlothysBakery).")]
        public List<string> fallbackPhrases = new List<string>();

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the resolved phrase list in the active locale.
        /// Synchronous — reliable only when tables are preloaded.
        /// Falls back to <see cref="fallbackPhrases"/> if LocalizationManager
        /// is not available or not yet initialized.
        /// </summary>
        public List<string> GetPhrases()
        {
            if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsInitialized)
            {
                if (fallbackPhrases != null && fallbackPhrases.Count > 0)
                    return fallbackPhrases;

                // Last resort: return the key names so the game never crashes
                var keys = new List<string>(phraseCount);
                for (int i = 0; i < phraseCount; i++)
                    keys.Add($"{keyPrefix}_{i}");
                return keys;
            }

            return LocalizationManager.Instance.GetPhraseList(keyPrefix, phraseCount, tableCollection);
        }

        /// <summary>
        /// Async version — always resolves correctly regardless of preload state.
        /// Use this from a coroutine when you need the strings before displaying
        /// the first phrase.
        ///
        /// Usage inside a MonoBehaviour coroutine:
        /// <code>
        ///   List&lt;string&gt; phrases = null;
        ///   yield return StartCoroutine(dialog.GetPhrasesAsync(list => phrases = list));
        ///   // phrases is now populated in the active locale
        /// </code>
        /// </summary>
        public IEnumerator GetPhrasesAsync(System.Action<List<string>> onResult,
            MonoBehaviour host)
        {
            if (LocalizationManager.Instance == null)
            {
                onResult?.Invoke(GetPhrases()); // will use fallback
                yield break;
            }

            yield return host.StartCoroutine(
                LocalizationManager.Instance.GetPhraseListAsync(
                    keyPrefix, phraseCount, onResult, tableCollection));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (phraseCount < 1) phraseCount = 1;
            if (string.IsNullOrWhiteSpace(tableCollection))
                tableCollection = LocalizationManager.TABLE_GAME_PHRASES;
            if (string.IsNullOrWhiteSpace(keyPrefix))
                Debug.LogWarning($"[LocalizedDialogSO] '{name}': keyPrefix is empty.", this);
        }
#endif
    }
}
