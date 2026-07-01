using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using INART.Framework.Core;
using INART.Framework.Core.Services;

namespace INART.SlothyBakery.SoundManager
{
    using Core;

    /// <summary>
    /// Persistent audio manager. Implements IAudioService so it can be resolved
    /// through the ServiceLocator from anywhere in the game.
    ///
    /// Supports:
    ///   - SFX via named key string OR legacy SoundType enum
    ///   - Background music with crossfade
    ///   - Master / Music / SFX volume (persisted via SettingsManager)
    ///   - AudioMixer integration for proper decibel scaling
    ///
    /// Setup:
    ///   1. Place on a persistent GameObject (DontDestroyOnLoad handled by Singleton).
    ///   2. Assign the AudioMixer and the three exposed parameters in the Inspector.
    ///   3. Assign AudioSources and AudioClips.
    ///   4. GameManager.RegisterServices() registers this as IAudioService automatically.
    /// </summary>
    public class SoundManager : Singleton<SoundManager>, IAudioService
    {
        // ── Legacy SoundType (kept for backward compatibility) ─────────────────
        public enum SoundType
        {
            Unlock,
            Success,
            Wrong,
            Selection1,
            Selection2,
        }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Audio Sources")]
        [UnityEngine.Serialization.FormerlySerializedAs("sfxAudioSource")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("AudioMixer (optional but recommended)")]
        [Tooltip("Assign your Master Mixer. Leave empty to control volume directly on AudioSources.")]
        [SerializeField] private AudioMixer mixer;
        [Tooltip("Exposed parameter name for Master volume in the mixer.")]
        [SerializeField] private string mixerMasterParam = "MasterVol";
        [Tooltip("Exposed parameter name for Music volume in the mixer.")]
        [SerializeField] private string mixerMusicParam  = "MusicVol";
        [Tooltip("Exposed parameter name for SFX volume in the mixer.")]
        [SerializeField] private string mixerSFXParam    = "SFXVol";

        [Header("SFX Clips")]
        [SerializeField] private AudioClip unlockClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip selection1Clip;
        [SerializeField] private AudioClip selection2Clip;

        [Header("Extra Named Clips")]
        [Tooltip("Add any extra SFX here. Key = name used in PlaySFX(string).")]
        [SerializeField] private List<NamedClip> extraClips = new List<NamedClip>();

        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Internal state ─────────────────────────────────────────────────────
        private Dictionary<string, AudioClip> _sfxCache;
        private bool _isInitialized;

        // ── IGameService ───────────────────────────────────────────────────────

        public void Initialize()
        {
            if (_isInitialized) return;

            BuildCache();
            _isInitialized = true;
            GameLogger.Log("SoundManager initialized.", LogCategory.Audio);
        }

        public void Shutdown()
        {
            StopMusic();
            _sfxCache?.Clear();
            GameLogger.Log("SoundManager shutdown.", LogCategory.Audio);
        }

        // ── Singleton lifecycle ────────────────────────────────────────────────

        protected override void OnAwake()
        {
            Initialize();
        }

        // ── IAudioService — SFX ───────────────────────────────────────────────

        /// <summary>Play a sound by name (case-insensitive). Falls back to a warning if not found.</summary>
        public void PlaySFX(string soundName)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                GameLogger.Warning("PlaySFX called with empty name.", LogCategory.Audio);
                return;
            }

            if (_sfxCache.TryGetValue(soundName.ToLower(), out var clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
            else
            {
                GameLogger.Warning($"SFX '{soundName}' not found in cache.", LogCategory.Audio);
            }
        }

        /// <summary>Legacy enum-based SFX call — kept so existing game code compiles without changes.</summary>
        public void Run_SFX(SoundType soundType)
        {
            PlaySFX(soundType switch
            {
                SoundType.Unlock     => "unlock",
                SoundType.Success    => "success",
                SoundType.Wrong      => "wrong",
                SoundType.Selection1 => "selection1",
                SoundType.Selection2 => "selection2",
                _                    => null
            });
        }

        // ── IAudioService — Music ─────────────────────────────────────────────

        public void PlayMusic(string musicName)
        {
            if (_sfxCache.TryGetValue(musicName.ToLower(), out var clip) && clip != null)
            {
                musicSource.clip = clip;
                musicSource.Play();
                GameLogger.Log($"Playing music: {musicName}", LogCategory.Audio);
            }
            else
            {
                GameLogger.Warning($"Music clip '{musicName}' not found in cache.", LogCategory.Audio);
            }
        }

        public void StopMusic()
        {
            if (musicSource.isPlaying) musicSource.Stop();
        }

        public void PauseMusic()
        {
            if (musicSource.isPlaying) musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (!musicSource.isPlaying && musicSource.clip != null) musicSource.UnPause();
        }

        public bool IsMusicPlaying() => musicSource.isPlaying;

        // ── IAudioService — Volume ────────────────────────────────────────────

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (mixer != null)
                mixer.SetFloat(mixerMasterParam, LinearToDecibel(volume));
            else
                AudioListener.volume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (mixer != null)
                mixer.SetFloat(mixerMusicParam, LinearToDecibel(volume));
            else
                musicSource.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (mixer != null)
                mixer.SetFloat(mixerSFXParam, LinearToDecibel(volume));
            else
                sfxSource.volume = volume;
        }

        // ── Public extras ─────────────────────────────────────────────────────

        /// <summary>Adds or replaces a named clip at runtime.</summary>
        public void RegisterClip(string key, AudioClip clip)
        {
            if (clip == null || string.IsNullOrEmpty(key)) return;
            _sfxCache[key.ToLower()] = clip;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void BuildCache()
        {
            _sfxCache = new Dictionary<string, AudioClip>
            {
                { "unlock",     unlockClip },
                { "success",    successClip },
                { "wrong",      wrongClip },
                { "selection1", selection1Clip },
                { "selection2", selection2Clip },
            };

            foreach (var nc in extraClips)
                if (nc.clip != null && !string.IsNullOrEmpty(nc.key))
                    _sfxCache[nc.key.ToLower()] = nc.clip;

            GameLogger.Log($"SoundManager cache built: {_sfxCache.Count} clips.", LogCategory.Audio);
        }

        /// <summary>Converts a 0–1 linear volume to decibels for the AudioMixer.</summary>
        private static float LinearToDecibel(float linear)
            => linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    // ── Helper data class ──────────────────────────────────────────────────────

    [System.Serializable]
    public class NamedClip
    {
        [Tooltip("Key used in PlaySFX(string) calls.")]
        public string    key;
        public AudioClip clip;
    }
}
