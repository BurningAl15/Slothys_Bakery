using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>Shows the active language name and flag on the Settings language row.</summary>
    public sealed class BakeryLanguageLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image flag;
        [SerializeField] private Sprite spanishFlag;
        [SerializeField] private Sprite englishFlag;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += Refresh;
            Refresh(LocalizationSettings.SelectedLocale);
        }

        private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= Refresh;

        private void Refresh(Locale locale)
        {
            bool spanish = locale != null && locale.Identifier.Code.StartsWith("es");
            if (label != null) label.text = spanish ? "Español" : "English";
            if (flag != null) flag.sprite = spanish ? spanishFlag : englishFlag;
        }
    }
}
