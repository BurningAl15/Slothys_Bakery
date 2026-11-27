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
        [SerializeField] private Sprite frenchFlag;
        [SerializeField] private Sprite italianFlag;
        [SerializeField] private Sprite germanFlag;
        [SerializeField] private Sprite portugueseFlag;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += Refresh;
            Refresh(LocalizationSettings.SelectedLocale);
        }

        private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= Refresh;

        private void Refresh(Locale locale)
        {
            string code = locale != null ? locale.Identifier.Code.ToLowerInvariant() : "en";
            if (code.StartsWith("es"))
            {
                if (label != null) label.text = "Español";
                if (flag != null && spanishFlag != null) flag.sprite = spanishFlag;
            }
            else if (code.StartsWith("fr"))
            {
                if (label != null) label.text = "Français";
                if (flag != null && frenchFlag != null) flag.sprite = frenchFlag;
            }
            else if (code.StartsWith("it"))
            {
                if (label != null) label.text = "Italiano";
                if (flag != null && italianFlag != null) flag.sprite = italianFlag;
            }
            else if (code.StartsWith("de"))
            {
                if (label != null) label.text = "Deutsch";
                if (flag != null && germanFlag != null) flag.sprite = germanFlag;
            }
            else if (code.StartsWith("pt"))
            {
                if (label != null) label.text = "Português";
                if (flag != null && portugueseFlag != null) flag.sprite = portugueseFlag;
            }
            else
            {
                if (label != null) label.text = "English";
                if (flag != null && englishFlag != null) flag.sprite = englishFlag;
            }
        }
    }
}
