using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace INART.SlothyBakery.SceneTransition
{
    public class SceneTransitionController : MonoBehaviour
    {
        public static SceneTransitionController _instance;

        private Image uiImage;

        public float transitionSpeed = 2f;

        private float floorValue = 0;
        private float currentValue = 0;

        private Coroutine currentCoroutine = null;

        private bool endAnimation = true;

        public bool EndAnimation => endAnimation;

        [SerializeField] private float maxTimer;

        [SerializeField] private AnimationCurve _animationCurve;
        private static readonly int EdgeSmoothing = Shader.PropertyToID("_EdgeSmoothing");
        private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);

            uiImage = GetComponent<Image>();

            floorValue = uiImage.material.GetFloat(EdgeSmoothing);
            float clearValue = 1 + floorValue;
            uiImage.material.SetFloat(Cutoff, clearValue);
            currentValue = uiImage.material.GetFloat(Cutoff);
        }

        //From Black To Color
        public void FadeIn()
        {
            endAnimation = false;
            currentCoroutine ??= StartCoroutine(FadeEffect_IN());
        }

        //From Color to Black
        public void FadeOut()
        {
            endAnimation = false;
            currentCoroutine ??= StartCoroutine(FadeEffect_OUT());
        }

        public IEnumerator FadeEffect_OUT()
        {
            var step = transitionSpeed * Time.deltaTime;

            var init = 1 + floorValue; //Color
            var end = -floorValue; //Black

            uiImage.material.SetFloat(Cutoff, init);
            // currentValue = init;

            yield return new WaitForSeconds(.5f);

            // while (currentValue >= end)
            // {
            //     uiImage.material.SetFloat("_Cutoff", Mathf.InverseLerp(end, init, currentValue));
            //     currentValue -= step;
            //     yield return step;
            // }
            
            float timer = maxTimer;
            
            while (timer >= 0)
            {
                timer -= Time.deltaTime * transitionSpeed;
                uiImage.material.SetFloat(Cutoff, Mathf.InverseLerp(end, init, _animationCurve.Evaluate(timer)));
                yield return Time.deltaTime;
            }
            
            // currentValue = end;
            uiImage.material.SetFloat(Cutoff, end);
            currentCoroutine = null;
            endAnimation = true;
        }

        public IEnumerator FadeEffect_IN()
        {
            var step = transitionSpeed * Time.deltaTime;

            var init = -floorValue;
            var end = 1 + floorValue;

            uiImage.material.SetFloat(Cutoff, init);
            // currentValue = init;

            yield return new WaitForSeconds(.5f);

            // while (currentValue <= end)
            // {
            //     uiImage.material.SetFloat("_Cutoff", Mathf.InverseLerp(init, end, currentValue));
            //     currentValue += step;
            //     yield return step;
            // }

            float timer = 0;
            
            while (timer <= maxTimer)
            {
                timer += Time.deltaTime * transitionSpeed;
                uiImage.material.SetFloat(Cutoff, Mathf.InverseLerp( init,end, _animationCurve.Evaluate(timer)));
                yield return Time.deltaTime;
            }
            
            // currentValue = end;
            uiImage.material.SetFloat(Cutoff, end);
            currentCoroutine = null;
            endAnimation = true;
        }
    }
}