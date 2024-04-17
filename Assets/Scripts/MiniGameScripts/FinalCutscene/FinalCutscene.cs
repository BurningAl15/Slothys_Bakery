using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using UnityEngine.SceneManagement;
// using LoLSDK;


namespace INART.SlothyBakery.Base
{
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;

    public class FinalCutscene : MiniGameParent
    {
        private Coroutine currentCoroutine = null;

        //[SerializeField] Button button;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private UnityEngine.Rendering.Universal.Light2D light;
        [SerializeField] private Animator anim;
        [SerializeField] private AnimationCurve _animationCurve;
        [SerializeField] List<ParticleSystem> particleSystems = new List<ParticleSystem>();
        [SerializeField] private ParticleSystem _starParticleSystem;

        public override void Init()
        {
            anim.enabled = false;
            _sprite.transform.localScale = Vector3.zero;
            light.intensity = 0;
            //button.GetComponent<RectTransform>().localScale = Vector3.zero;
            //button.interactable = false;

            // for (int i = 0; i < particleSystems.Count; i++)
            // {
            //     particleSystems[i].Stop();
            // }
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            yield return null;

            var scaledTime = Time.fixedDeltaTime;
            float animationTime = 0;
            var moveDelay = 1f;
            TurnOnParticles();
            while (animationTime <= moveDelay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / moveDelay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                _sprite.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, animationCurveValue);
                light.intensity = animationCurveValue * .75f;

                yield return null;
            }

            yield return new WaitForSeconds(.15f);
            _starParticleSystem.Play();
            anim.enabled = true;
            TurnOnParticles();
            // yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
            yield return new WaitForSeconds(.5f);
            // LOLSDK.Instance.CompleteGame();

            //animationTime = 0;
            //while (animationTime <= moveDelay)
            //{
            //    animationTime += scaledTime;
            //    var percent = Mathf.Clamp01(animationTime / moveDelay);
            //    var animationCurveValue = _animationCurve.Evaluate(percent);

            //    button.GetComponent<RectTransform>().localScale = Vector3.one * animationCurveValue;

            //    yield return null;
            //}

            //button.interactable = true;

            // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            currentCoroutine = null;
        }

        public void TurnOffLight()
        {
            StartCoroutine(TurnOff_Light());
        }

        IEnumerator TurnOff_Light()
        {
            yield return new WaitForSeconds(1f);
            light.intensity = 0;
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return null;
        }

        public void TurnOnParticles()
        {
            for (int i = 0; i < particleSystems.Count; i++)
            {
                particleSystems[i].Play();
            }
        }
    }
}