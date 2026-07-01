using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace INART.SlothyBakery.SceneTransition
{
    using Core;

    /// <summary>
    /// Controls scene transition effects using shader-based fade animations.
    /// Provides smooth fade in/out transitions between scenes.
    /// </summary>
    public class SceneTransitionController : Singleton<SceneTransitionController>
    {
        #region Serialized Fields
        [SerializeField] private float transitionSpeed = 2f;
        [SerializeField] private float maxTimer;
        [SerializeField] private AnimationCurve _animationCurve;
        #endregion

        #region Private Fields
        private Image uiImage;
        private float floorValue = 0;
        private float currentValue = 0;
        private Coroutine currentCoroutine = null;
        private bool endAnimation = true;
        #endregion

        #region Shader Property IDs
        private static readonly int EdgeSmoothing = Shader.PropertyToID("_EdgeSmoothing");
        private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
        #endregion

        #region Properties
        /// <summary>
        /// Returns true when the transition animation has completed.
        /// </summary>
        public bool EndAnimation => endAnimation;
        #endregion

        #region Singleton Configuration
        protected override bool PersistAcrossScenes => true;
        #endregion

        #region Unity Lifecycle
        protected override void OnAwake()
        {
            uiImage = GetComponent<Image>();

            floorValue = uiImage.material.GetFloat(EdgeSmoothing);
            float clearValue = 1 + floorValue;
            uiImage.material.SetFloat(Cutoff, clearValue);
            currentValue = uiImage.material.GetFloat(Cutoff);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Fades from black to clear (scene becomes visible).
        /// </summary>
        public void FadeIn()
        {
            endAnimation = false;
            currentCoroutine ??= StartCoroutine(FadeEffect_IN());
        }

        /// <summary>
        /// Fades from clear to black (scene becomes hidden).
        /// </summary>
        public void FadeOut()
        {
            endAnimation = false;
            currentCoroutine ??= StartCoroutine(FadeEffect_OUT());
        }

        /// <summary>
        /// Coroutine for fade out effect.
        /// </summary>
        public IEnumerator FadeEffect_OUT()
        {
            var step = transitionSpeed * Time.deltaTime;

            var init = 1 + floorValue; //Color
            var end = -floorValue; //Black

            uiImage.material.SetFloat(Cutoff, init);

            yield return new WaitForSeconds(.5f);

            float timer = maxTimer;
            
            while (timer >= 0)
            {
                timer -= Time.deltaTime * transitionSpeed;
                uiImage.material.SetFloat(Cutoff, Mathf.InverseLerp(end, init, _animationCurve.Evaluate(timer)));
                yield return Time.deltaTime;
            }
            
            uiImage.material.SetFloat(Cutoff, end);
            currentCoroutine = null;
            endAnimation = true;
        }

        /// <summary>
        /// Coroutine for fade in effect.
        /// </summary>
        public IEnumerator FadeEffect_IN()
        {
            var step = transitionSpeed * Time.deltaTime;

            var init = -floorValue;
            var end = 1 + floorValue;

            uiImage.material.SetFloat(Cutoff, init);

            yield return new WaitForSeconds(.5f);

            float timer = 0;
            
            while (timer <= maxTimer)
            {
                timer += Time.deltaTime * transitionSpeed;
                uiImage.material.SetFloat(Cutoff, Mathf.InverseLerp( init,end, _animationCurve.Evaluate(timer)));
                yield return Time.deltaTime;
            }
            
            uiImage.material.SetFloat(Cutoff, end);
            currentCoroutine = null;
            endAnimation = true;
        }
        #endregion
    }
}