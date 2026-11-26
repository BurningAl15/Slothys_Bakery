using FatCat.Feedback;
using FatCat.Mobile;
using INART.SlothyBakery.SoundManager;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>Connects the shared Feedback Kit to Bakery settings, language, and sound.</summary>
    public static class BakeryFeedbackHooks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            FatcatHaptics.EnabledProvider = () => BakerySettingsBackend.VibrationEnabled;
            FeedbackStamp.StreakLabel = count => string.Format(Text("feedback.streak", "Racha ×{0}!"), count);
            FeedbackStamp.Haptic = PlayAnswerHaptic;
            CheckBellButton.Volume = () => BakeryAudioSettings.Effects;
            CheckBellButton.Haptic = () =>
            {
                FatcatHaptics.PlayLightImpact();
            };
            FeedbackCelebration.StarSound = _ => SoundManager.Instance?.Run_SFX(SoundManager.SoundType.Success);
        }

        /// <summary>Central answer haptic path; called by BakerySession so all three games use the same feedback.</summary>
        public static void PlayAnswerHaptic(bool correct)
        {
            if (correct) FatcatHaptics.PlaySuccess();
            else FatcatHaptics.PlayWarning();
        }

        /// <summary>Triggers Feedback Kit visual stamp + sparkle safely in gameplay scenes.</summary>
        public static void ShowAnswerStamp(bool correct, Vector3? worldPoint = null)
        {
            if (Application.isBatchMode) return;
            try
            {
                Vector3 pos = worldPoint ?? Vector3.zero;
                if (correct)
                {
                    FeedbackStamp.Right(Text("feedback.right", "¡Muy bien!"), pos);
                    FeedbackStamp.Sparkle(pos, 10);
                }
                else
                {
                    FeedbackStamp.Wrong(Text("feedback.try_again", "¡Inténtalo otra vez!"), pos);
                }
            }
            catch (System.Exception)
            {
                // Keep gameplay flow isolated if optional Feedback Kit assets aren't present in a scene.
            }
        }

        public static string Text(string key, string fallback)
            => BakeryPromptLocalization.GetLocalizedOrFallback(key, fallback);
    }
}
