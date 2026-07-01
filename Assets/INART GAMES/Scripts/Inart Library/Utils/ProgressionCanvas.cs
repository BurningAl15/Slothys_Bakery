using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using INART.Framework.Utils;

namespace INART.SlothyBakery.ProgressionCanvas
{
    using SoundManager;
    using Core;

    /// <summary>
    /// Manages progression UI elements (bars, locks, particles).
    /// Handles visual feedback for level completion and unlocks.
    ///
    /// Migrated from manual delta-time loops to DOTween.
    /// Public API (FillBar / CleanBar / UnlockStuff) stays as coroutines
    /// so existing minigame code can continue to yield on them unchanged.
    /// </summary>
    public class ProgressionCanvas : Singleton<ProgressionCanvas>
    {
        // ── Enums ──────────────────────────────────────────────────────────────

        /// <summary>How much of the bar one action fills.</summary>
        public enum ProgressionType
        {
            One,    // 100 %
            Two,    // 50 %
            Three   // 33.4 %
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Bars & Locks")]
        [UnityEngine.Serialization.FormerlySerializedAs("bars")]
        [SerializeField] private List<Image> bars  = new List<Image>();
        [UnityEngine.Serialization.FormerlySerializedAs("locks")]
        [SerializeField] private List<Image> locks = new List<Image>();
        [SerializeField] private Sprite  unlockSprite;

        [Header("Particles")]
        [UnityEngine.Serialization.FormerlySerializedAs("particleSystems")]
        [SerializeField] private List<ParticleSystem> particleSystems = new List<ParticleSystem>();
        [SerializeField] private ParticleSystem   partyParticles;

        [Header("Animation Timings")]
        [SerializeField] private float fillDuration   = 0.5f;
        [SerializeField] private float cleanDuration  = 0.35f;
        [SerializeField] private float unlockDuration = 0.35f;

        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => false;

        // ── Runtime state ──────────────────────────────────────────────────────
        // Tracks the logical fill value per bar so DOTween always starts from
        // the correct position regardless of animation interruption.
        private float[] _fillValues;

        protected override void OnAwake()
        {
            _fillValues = new float[bars.Count];
        }

        // ── Public API — all coroutines so minigames can yield on them ─────────

        /// <summary>
        /// Fills bar[index] by the amount determined by <paramref name="progression"/>.
        /// Plays the completion particle when fill reaches 1.
        /// </summary>
        public IEnumerator FillBar(int index, ProgressionType progression)
        {
            if (index < 0 || index >= bars.Count)
            {
                INART.Framework.Core.GameLogger.Error(
                    $"ProgressionCanvas.FillBar: index {index} out of range (bars.Count={bars.Count}). " +
                    "Check Inspector assignments.", INART.Framework.Core.LogCategory.UI);
                yield break;
            }

            float amount = progression switch
            {
                ProgressionType.One   => 1.000f,
                ProgressionType.Two   => 0.500f,
                ProgressionType.Three => 0.334f,
                _                     => 1.000f
            };

            float target = Mathf.Clamp01(_fillValues[index] + amount);
            _fillValues[index] = target;

            var tween = bars[index]
                .DOFillAmount(target, fillDuration)
                .SetEase(Ease.OutQuad);

            yield return tween.WaitForCompletion();

            if (target >= 1f && particleSystems != null && index < particleSystems.Count)
                particleSystems[index].Play();
        }

        public IEnumerator CleanBar(int index)
        {
            if (index < 0 || index >= bars.Count)
            {
                INART.Framework.Core.GameLogger.Error(
                    $"ProgressionCanvas.CleanBar: index {index} out of range.", INART.Framework.Core.LogCategory.UI);
                yield break;
            }

            _fillValues[index] = 0f;

            var tween = bars[index]
                .DOFillAmount(0f, cleanDuration)
                .SetEase(Ease.InQuad);

            yield return tween.WaitForCompletion();
        }

        public IEnumerator UnlockStuff(int index)
        {
            if (index < 0 || index >= locks.Count)
            {
                INART.Framework.Core.GameLogger.Error(
                    $"ProgressionCanvas.UnlockStuff: index {index} out of range (locks.Count={locks.Count}). " +
                    "Check Inspector assignments.", INART.Framework.Core.LogCategory.UI);
                yield break;
            }

            _fillValues[index] = 0f;

            var lockRt = locks[index].GetComponent<RectTransform>();

            locks[index].sprite = unlockSprite;
            lockRt.localScale   = Vector3.one * 1.5f;

            SoundManager.Instance.Run_SFX(SoundManager.SoundType.Unlock);

            var tween = lockRt
                .DOScale(Vector3.one, unlockDuration)
                .SetEase(Ease.OutBack);

            yield return tween.WaitForCompletion();
        }

        /// <summary>Plays the end-of-game celebration particles.</summary>
        public void RunPartyParticles() => partyParticles?.Play();
    }
}
