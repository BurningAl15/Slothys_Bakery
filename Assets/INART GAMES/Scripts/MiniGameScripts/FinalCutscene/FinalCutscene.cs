using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace INART.SlothyBakery.Base
{
    using Analytics;
    using CanvasAnimator;
    using Rating;

    /// <summary>
    /// Final cutscene — plays the celebration animation, unlocks/shows the shareable Diploma
    /// ("Ver mi diploma") and "Volver al inicio", and triggers Analytics & Rating.
    /// </summary>
    public class FinalCutscene : MiniGameParent
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _sprite;
        [FormerlySerializedAs("light")]
        [SerializeField] private Light2D _light;
        [FormerlySerializedAs("anim")]
        [SerializeField] private Animator _anim;
        [SerializeField] private ParticleSystem _starParticleSystem;
        [SerializeField] private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

        [Header("Shell Polish (installed by BakeryFinalSceneInstaller)")]
        [SerializeField] private TMP_Text finishedLine;
        [SerializeField] private Button diplomaButton;
        [SerializeField] private Button backButton;
        [SerializeField] private CanvasGroup rewardDim;

        [Header("Animation")]
        [SerializeField] private float scaleInDuration = 1f;
        [SerializeField] private float lightMaxIntensity = 0.75f;
        [SerializeField] private float holdBeforeStar = 0.15f;
        [SerializeField] private float holdAfterStar = 0.5f;

        private Vector3 _starScale = Vector3.one;

        public override void Init()
        {
            if (_sprite != null)
            {
                if (_anim == null) _anim = _sprite.GetComponentInChildren<Animator>();
                _starScale = _sprite.transform.localScale == Vector3.zero ? Vector3.one : _sprite.transform.localScale;
                _sprite.transform.localScale = Vector3.zero;
            }
            if (_anim != null) _anim.enabled = false;
            if (_light != null) _light.intensity = 0f;
            if (finishedLine != null) finishedLine.alpha = 0f;
            if (rewardDim != null) rewardDim.alpha = 0f;
            if (diplomaButton != null)
            {
                diplomaButton.transform.localScale = Vector3.zero;
                diplomaButton.onClick.RemoveAllListeners();
                diplomaButton.onClick.AddListener(Fatcat.Slothy.Bakery.Shell.BakeryDiploma.Open);
            }
            if (backButton != null)
            {
                backButton.transform.localScale = Vector3.zero;
                backButton.interactable = false;
            }
            foreach (var ps in particleSystems)
                if (ps != null) ps.Stop();
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            Fatcat.Slothy.Bakery.Modes.BakeryStats.AtLeast("story.finished", 1);
            TurnOnParticles();
            if (rewardDim != null)
                DOTween.To(() => rewardDim.alpha, a => rewardDim.alpha = a, 1f, 0.5f);

            if (_sprite != null)
            {
                var seq = DOTween.Sequence().Append(_sprite.transform.DOScale(_starScale, scaleInDuration).SetEase(Ease.OutBack, 1.4f));
                if (_light != null)
                    seq.Join(DOTween.To(() => _light.intensity, v => _light.intensity = v, lightMaxIntensity, scaleInDuration).SetEase(Ease.OutQuad));
                yield return seq.WaitForCompletion();
            }

            yield return new WaitForSeconds(holdBeforeStar);

            if (_starParticleSystem != null) _starParticleSystem.Play();
            if (_anim != null) _anim.enabled = true;
            TurnOnParticles();

            if (_sprite != null)
                FatCat.Feedback.FeedbackStamp.Sparkle(_sprite.transform.position, 20);

            if (finishedLine != null)
            {
                var line = (RectTransform)finishedLine.transform;
                var home = line.anchoredPosition;
                line.anchoredPosition = home + Vector2.down * 20f;
                DOTween.Sequence()
                    .Append(DOTween.To(() => finishedLine.alpha, a => finishedLine.alpha = a, 1f, 0.38f))
                    .Join(line.DOAnchorPos(home, 0.45f).SetEase(Ease.OutBack));
            }

            yield return new WaitForSeconds(holdAfterStar);

            AnalyticsManager.Instance?.TrackGameCompleted();
            RatingManager.Instance?.OnGameCompleted();

            if (diplomaButton != null)
            {
                diplomaButton.interactable = true;
                diplomaButton.transform.DOScale(1f, 0.55f).SetEase(Ease.OutBack, 1.4f).SetDelay(0.12f);
            }
            if (backButton != null)
            {
                yield return backButton.transform.DOScale(1f, 0.55f).SetEase(Ease.OutBack, 1.4f).WaitForCompletion();
                backButton.interactable = true;
            }
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return null;
        }

        public void TurnOnParticles()
        {
            foreach (var ps in particleSystems)
                if (ps != null) ps.Play();
        }

        public void TurnOffLight()
        {
            StartCoroutine(TurnOff_Light());
        }

        private IEnumerator TurnOff_Light()
        {
            yield return new WaitForSeconds(1f);
            if (_light != null)
            {
                yield return DOTween.To(
                    () => _light.intensity,
                    v => _light.intensity = v,
                    0f,
                    0.5f)
                    .SetEase(Ease.InQuad)
                    .WaitForCompletion();
            }
        }
    }
}
