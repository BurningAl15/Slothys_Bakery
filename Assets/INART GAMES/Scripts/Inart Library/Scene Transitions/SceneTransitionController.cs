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
        private static Image _overlayFadeImage;
        private static GameObject _overlayCanvasGo;
        private Coroutine currentCoroutine = null;
        private bool endAnimation = true;
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
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
                DontDestroyOnLoad(gameObject);
            }
            uiImage = GetComponent<Image>();
            if (uiImage != null)
            {
                uiImage.enabled = false;
                uiImage.raycastTarget = false;
            }
            EnsureOverlay();
        }

        private void EnsureOverlay()
        {
            if (_overlayFadeImage != null) return;

            _overlayCanvasGo = new GameObject("BakeryFadeOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            DontDestroyOnLoad(_overlayCanvasGo);
            var c = _overlayCanvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999;

            var imgGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(_overlayCanvasGo.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _overlayFadeImage = imgGo.GetComponent<Image>();
            _overlayFadeImage.color = new Color(0f, 0f, 0f, 0f);
            _overlayFadeImage.raycastTarget = false;
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
            EnsureOverlay();
            if (_overlayFadeImage == null || transitionSpeed <= 0f)
            {
                endAnimation = true;
                yield break;
            }

            _overlayFadeImage.raycastTarget = true;
            float dur = maxTimer > 0f ? Mathf.Clamp(maxTimer / transitionSpeed, 0.15f, 0.55f) : 0.35f;
            yield return null;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float a = Mathf.Clamp01(elapsed / dur);
                _overlayFadeImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _overlayFadeImage.color = new Color(0f, 0f, 0f, 1f);
            currentCoroutine = null;
            endAnimation = true;
        }

        /// <summary>
        /// Coroutine for fade in effect.
        /// </summary>
        public IEnumerator FadeEffect_IN()
        {
            EnsureOverlay();
            if (_overlayFadeImage == null || transitionSpeed <= 0f)
            {
                endAnimation = true;
                yield break;
            }

            float dur = maxTimer > 0f ? Mathf.Clamp(maxTimer / transitionSpeed, 0.15f, 0.55f) : 0.35f;
            yield return null;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float a = 1f - Mathf.Clamp01(elapsed / dur);
                _overlayFadeImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _overlayFadeImage.color = new Color(0f, 0f, 0f, 0f);
            _overlayFadeImage.raycastTarget = false;
            currentCoroutine = null;
            endAnimation = true;
        }
        #endregion
    }
}