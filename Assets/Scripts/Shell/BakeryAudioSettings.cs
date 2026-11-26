using INART.Framework.Core;
using INART.Framework.Core.Services;
using INART.SlothyBakery.Core;
using INART.SlothyBakery.SoundManager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fatcat.Slothy.Bakery.Shell
{
    /// <summary>
    /// Persists and applies music and sound-effect volumes across Bakery scenes to both
    /// BackgroundSoundManager and SoundManager / IAudioService.
    /// </summary>
    public static class BakeryAudioSettings
    {
        const string MusicKey = "cfg_audio_music";
        const string EffectsKey = "cfg_audio_sfx";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!Application.isPlaying) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySaved();
        }

        static void OnSceneLoaded(Scene _, LoadSceneMode __) => ApplySaved();

        public static float Music
        {
            get
            {
                var mgr = FindSettings();
                return mgr != null ? mgr.MusicVolume : PlayerPrefs.GetFloat(MusicKey, 0.7f);
            }
        }

        public static float Effects
        {
            get
            {
                var mgr = FindSettings();
                return mgr != null ? mgr.SFXVolume : PlayerPrefs.GetFloat(EffectsKey, 0.9f);
            }
        }

        public static void SetMusic(float value)
        {
            value = Mathf.Clamp01(value);
            var mgr = FindSettings();
            if (mgr != null) mgr.MusicVolume = value;
            else PlayerPrefs.SetFloat(MusicKey, value);
            PlayerPrefs.Save();
            Apply(value, Effects);
        }

        public static void SetEffects(float value)
        {
            value = Mathf.Clamp01(value);
            var mgr = FindSettings();
            if (mgr != null) mgr.SFXVolume = value;
            else PlayerPrefs.SetFloat(EffectsKey, value);
            PlayerPrefs.Save();
            Apply(Music, value);
        }

        public static void ApplySaved() => Apply(Music, Effects);

        static SettingsManager FindSettings() => Object.FindAnyObjectByType<SettingsManager>();

        static void Apply(float music, float effects)
        {
            var background = Object.FindAnyObjectByType<BackgroundSoundManager>();
            if (background != null)
            {
                background.SetBackgroundMusicVolume(Mathf.Max(0.0001f, music));
                background.SetBsfxMusicVolume(Mathf.Max(0.0001f, effects));
            }

            if (ServiceLocator.Has<IAudioService>())
            {
                var audio = ServiceLocator.Get<IAudioService>();
                audio.SetMusicVolume(music);
                audio.SetSFXVolume(effects);
            }
            else
            {
                var sounds = Object.FindAnyObjectByType<INART.SlothyBakery.SoundManager.SoundManager>();
                if (sounds != null)
                {
                    sounds.SetMusicVolume(music);
                    sounds.SetSFXVolume(effects);
                }
            }
        }
    }
}
