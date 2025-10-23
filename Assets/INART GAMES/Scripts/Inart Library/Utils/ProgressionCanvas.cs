using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace INART.SlothyBakery.ProgressionCanvas
{
    using SoundManager;
    public class ProgressionCanvas : MonoBehaviour
    {
        public static ProgressionCanvas _instance;
        [SerializeField] private List<Image> Bars = new List<Image>();
        [SerializeField] private List<Image> Locks = new List<Image>();
        [SerializeField] private Sprite unlockSprite;
        [SerializeField] private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

        [SerializeField] private ParticleSystem partyParticles;
        
        public enum ProgressionType
        {
            One,
            Two,
            Three
        }

        private float fillBar = 0;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// Should be generalized but for the first game, i'm using two scenaries, cut two breads
        /// This should be called every time you finish the cut, not when you finish a line, because
        /// We are couting the success in the exercise, not just a part of the exercise
        /// </summary>
        /// <param name="index">The index of the bar that should be painted</param>
        /// <returns></returns>
        public IEnumerator FillBar(int index, ProgressionType _progression)
        {
            var i = .5f;

            switch (_progression)
            {
                case ProgressionType.One:
                    i = 1f;
                    break;
                case ProgressionType.Two:
                    i = .5f;
                    break;
                case ProgressionType.Three:
                    i = .334f;
                    break;
            }
            // fillSoundSource.Play();
            var topLimit = fillBar + i;
            while (fillBar < topLimit)
            {
                fillBar += Time.fixedDeltaTime;
                Bars[index].fillAmount = fillBar;
                yield return null;
            }

            if (fillBar >= 1)
            {
                particleSystems[index].Play();
            }
            StopCoroutine("FillBar");
        }

        /// <summary>
        /// Should be generalized but for the first game, i'm using two scenaries, cut two breads
        /// This should be called when you fail a part of the exercise or if the whole job is badly done
        /// Will clear the whole bar so the mechanic should reflect that too.
        /// </summary>
        /// <param name="index">The index of the bar that should be return to 0</param>
        /// <returns></returns>
        public IEnumerator CleanBar(int index)
        {
            while (fillBar > 0)
            {
                fillBar -= Time.fixedDeltaTime;
                Bars[index].fillAmount = fillBar;
                yield return null;
            }

            yield return null;
            StopCoroutine("CleanBar");
        }

        /// <summary>
        /// Animates the whole Unlock Stuff, should be called when you success in the last part of the
        /// exercise, run first the FillBar coroutine, and then this one
        /// ex. yield return StartCoroutine(ProgressionCanvas._instance.FillBar(index));
        /// ex. yield return StartCoroutine(ProgressionCanvas._instance.UnlockStuff(index));
        /// </summary>
        /// <param name="index">The index of the bar that should be painted</param>
        /// <returns></returns>
        public IEnumerator UnlockStuff(int _index)
        {
            fillBar = 0;
            Locks[_index].GetComponent<RectTransform>().localScale = 1.5f * Vector3.one;
            Locks[_index].sprite = unlockSprite;
            var scaledTime = Time.fixedDeltaTime * 2;
            SoundManager._instance.Run_SFX(SoundManager.SoundType.Unlock);

            for (float i = 0; i <= 1; i += scaledTime)
            {
                Locks[_index].GetComponent<RectTransform>().localScale = Vector3.Lerp(1.5f * Vector3.one, Vector3.one,
                    Mathf.InverseLerp(0, 1, i * 2));
                yield return null;
            }

            StopCoroutine("UnlockStuff");
        }

        public void RunPartyParticles()
        {
            partyParticles.Play();
        }
    }
}