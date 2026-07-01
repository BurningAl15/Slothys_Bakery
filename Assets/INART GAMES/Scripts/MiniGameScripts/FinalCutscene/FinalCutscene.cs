using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace INART.SlothyBakery.Base
{
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;
    using Analytics;
    using Rating;

    /// <summary>
    /// Final cutscene — plays the celebration animation, then triggers:
    ///   - AnalyticsManager.TrackGameCompleted
    ///   - RatingManager.OnGameCompleted (may show rating popup)
    ///
    /// Animation migrated from manual coroutine Lerp to DOTween.
    /// TurnOff_Light still uses a coroutine (called externally from Animator events).
    /// </summary>
    public class FinalCutscene : MiniGameParent
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private SpriteRenderer    _sprite;
        [SerializeField] private Light2D           _light;
        [SerializeField] private Animator          _anim;
        [SerializeField] private ParticleSystem    _starParticleSystem;
        [SerializeField] private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

        [Header("Animation")]
        [SerializeField] private float scaleInDuration  = 1f;
        [SerializeField] private float lightMaxIntensity = 0.75f;
        [SerializeField] private float holdBeforeStar    = 0.15f;
        [SerializeField] private float holdAfterStar     = 0.5f;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        public override void Init()
        {
            _anim.enabled              = false;
            _sprite.transform.localScale = Vector3.zero;
            _light.intensity           = 0f;
        }

        // ── MiniGameParent overrides ───────────────────────────────────────────

        public override IEnumerator TurnOnGame(float _delay)
        {
            TurnOnParticles();

            // Scale sprite in + fade light simultaneously with DOTween
            var seq = DOTween.Sequence()
                .Append(_sprite.transform
                    .DOScale(Vector3.one, scaleInDuration)
                    .SetEase(Ease.OutBack))
                .Join(DOTween.To(
                    () => _light.intensity,
                    v  => _light.intensity = v,
                    lightMaxIntensity,
                    scaleInDuration)
                    .SetEase(Ease.OutQuad));

            yield return seq.WaitForCompletion();

            yield return new WaitForSeconds(holdBeforeStar);

            _starParticleSystem.Play();
            _anim.enabled = true;
            TurnOnParticles();

            yield return new WaitForSeconds(holdAfterStar);

            // ── Game completed — fire engagement modules ───────────────────────
            AnalyticsManager.Instance?.TrackGameCompleted();
            RatingManager.Instance?.OnGameCompleted();
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return null;
        }

        // ── Public helpers (called from Animator events or external scripts) ───

        public void TurnOnParticles()
        {
            foreach (var ps in particleSystems)
                ps.Play();
        }

        /// <summary>Called from an Animator event to fade the light out.</summary>
        public void TurnOffLight()
        {
            StartCoroutine(TurnOff_Light());
        }

        private IEnumerator TurnOff_Light()
        {
            yield return new WaitForSeconds(1f);

            yield return DOTween.To(
                () => _light.intensity,
                v  => _light.intensity = v,
                0f,
                0.5f)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();
        }
    }
}
