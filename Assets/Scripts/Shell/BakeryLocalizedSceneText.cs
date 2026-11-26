using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>Localizes a fixed TMP label in a Bakery minigame and refreshes it when the locale changes.</summary>
    [DisallowMultipleComponent]
    public sealed class BakeryLocalizedSceneText : MonoBehaviour
    {
        [SerializeField] private string table = "BakeryShell";
        [SerializeField] private string key;

        private TMP_Text label;
        private Febucci.UI.TextAnimator animator;
        private Coroutine refreshRoutine;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            animator = GetComponent<Febucci.UI.TextAnimator>();
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (refreshRoutine != null) StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        private void OnLocaleChanged(Locale _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (refreshRoutine != null) StopCoroutine(refreshRoutine);
            if (isActiveAndEnabled && !string.IsNullOrWhiteSpace(key))
                refreshRoutine = StartCoroutine(LoadLocalizedText());
        }

        private IEnumerator LoadLocalizedText()
        {
            var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
            yield return operation;
            if (operation.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                yield break;

            var value = operation.Result;
            if (BakeryPromptLocalization.IsMissingTranslation(value, key)) yield break;
            if (animator != null) animator.SyncText(value, false);
            else if (label != null) label.text = value;
            refreshRoutine = null;
        }

        public void Configure(string tableName, string entryKey)
        {
            table = string.IsNullOrWhiteSpace(tableName) ? "BakeryShell" : tableName;
            key = entryKey;
        }
    }
}
