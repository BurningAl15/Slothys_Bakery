namespace INART.Framework.Core.Services
{
    /// <summary>
    /// Contract for the audio service. Handles SFX, background music, and volume.
    ///
    /// Get it via:
    ///   ServiceLocator.Get&lt;IAudioService&gt;().PlaySFX("success");
    /// </summary>
    public interface IAudioService : IGameService
    {
        // ── SFX ───────────────────────────────────────────────────────────────
        void PlaySFX(string soundName);

        // ── Music ─────────────────────────────────────────────────────────────
        void PlayMusic(string musicName);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
        bool IsMusicPlaying();

        // ── Volume ────────────────────────────────────────────────────────────
        void SetMusicVolume(float volume);   // 0–1
        void SetSFXVolume(float volume);     // 0–1
        void SetMasterVolume(float volume);  // 0–1
    }
}
