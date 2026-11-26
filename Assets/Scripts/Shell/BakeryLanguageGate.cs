using FatCat.Localization.UI;
using INART.Framework.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Opens the language picker on the first launch until the player confirms a language,
    /// and exposes OpenPicker / Confirm for Settings and the picker Continue button.
    /// </summary>
    public sealed class BakeryLanguageGate : MonoBehaviour
    {
        public const string ChosenKey = "bakery.language_chosen";
        public static bool NeedsChoice => PlayerPrefs.GetInt(ChosenKey, 0) == 0;
        [SerializeField] private LocalePickerController picker;

        private void Start()
        {
            if (picker != null && NeedsChoice)
            {
                picker.transform.SetAsLastSibling();
                picker.Open();
            }
        }

        public void OpenPicker()
        {
            if (picker != null)
            {
                picker.transform.SetAsLastSibling();
                picker.Open();
            }
        }

        /// <summary>Bound to the picker's Continue event.</summary>
        public void Confirm()
        {
            PlayerPrefs.SetInt(ChosenKey, 1);
            var locale = LocalizationSettings.SelectedLocale;
            var mgr = FindAnyObjectByType<INART.Framework.Core.SettingsManager>();
            if (locale != null && mgr != null)
                mgr.Language = locale.Identifier.Code;
            PlayerPrefs.Save();
            if (picker != null)
                picker.Close();

            FindAnyObjectByType<INART.SlothyBakery.Base.InitialMenuManager>()?.BeginDeferredIntro();
            FindAnyObjectByType<BakeryModesView>()?.BeginDeferredDockIntro();
        }
    }
}
