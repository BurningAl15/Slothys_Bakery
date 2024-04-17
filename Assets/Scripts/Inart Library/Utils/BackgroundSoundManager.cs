using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace INART.SlothyBakery.SoundManager
{
    public class BackgroundSoundManager : MonoBehaviour
    {
        public static BackgroundSoundManager _instance;
        [SerializeField] private AudioClip background1;
        [SerializeField] private AudioClip background2;

        [SerializeField] private AudioSource backgroundSound1;
        [SerializeField] private AudioSource backgroundSound2;

        [SerializeField] private List<string> sceneNames = new List<string>();

        private Scene lastScene;
        private int sceneBlock;
        private bool changeScene;

        [SerializeField] private AudioMixer mixer;

        [SerializeField] private bool isTesting;

        private Coroutine currentCoroutine = null;

        public void SetBackgroundMusicVolume(float _soundLevel)
        {
            //print(_soundLevel);
            mixer.SetFloat("MusicVol", Mathf.Log10(_soundLevel) * 20);
        }

        public void SetBSFXMusicVolume(float _soundLevel)
        {
            //print(_soundLevel);
            mixer.SetFloat("SFXVol", Mathf.Log10(_soundLevel) * 20);
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);

            if (_instance == null)
                _instance = this;
            else if (_instance != null)
                Destroy(this.gameObject);

            if (!isTesting)
            {
                if (SceneManager.GetActiveScene().name == sceneNames[0] || SceneManager.GetActiveScene().name == sceneNames[1])
                {
                    backgroundSound1.clip = background1;
                    backgroundSound2.clip = background2;
                    sceneBlock = 0;
                    backgroundSound1.Play();
                }
            }
            else
            {
                backgroundSound1.clip = background1;
                backgroundSound2.clip = background2;
                sceneBlock = 1;
                backgroundSound1.Play();
            }
        }

        private void Update()
        {
            if (lastScene != SceneManager.GetActiveScene())
            {
                if (SceneManager.GetActiveScene().name == sceneNames[2]
                    || SceneManager.GetActiveScene().name == sceneNames[3]
                    || SceneManager.GetActiveScene().name == sceneNames[4]
                    && sceneBlock == 1)
                {
                    if (currentCoroutine == null)
                        currentCoroutine = StartCoroutine(FadeBetweenAudios(0, 0.4f));
                }
                else if (SceneManager.GetActiveScene().name == sceneNames[1]
                         || SceneManager.GetActiveScene().name == sceneNames[5]
                         && sceneBlock == 0)
                {
                    //print("Entering");
                    if (currentCoroutine == null)
                        currentCoroutine = StartCoroutine(FadeBetweenAudios(1, 0.4f));
                }
            }

            lastScene = SceneManager.GetActiveScene();
        }

        private IEnumerator FadeBetweenAudios(int _type, float _time)
        {
            var timeScaler = 1f;
            var scaledTime = .05f * timeScaler;

            if (_type == 0)
            {
#if UNITY_EDITOR
                Debug.Log("BG 1!");
#endif

                backgroundSound2.volume = 0;
                backgroundSound2.Play();

                for (float i = 0; i <= _time; i += scaledTime)
                {
                    backgroundSound2.volume = (i / _time) * .25f;
                    backgroundSound1.volume = (_time - (i / _time)) * .2f;
                    yield return null;
                }

                sceneBlock = 0;
                print(sceneBlock);
                backgroundSound1.Stop();
            }
            else if (_type == 1)
            {
#if UNITY_EDITOR
                Debug.Log("BG 2!!!!");
#endif

                backgroundSound1.volume = 0;
                backgroundSound1.Play();

                for (float i = 0; i <= _time; i += scaledTime)
                {
                    backgroundSound1.volume = (i / _time);
                    backgroundSound2.volume = (_time - (i / _time)) * .2f;
                    yield return null;
                }

                sceneBlock = 1;
                //print(sceneBlock);
                backgroundSound2.Stop();
            }

            currentCoroutine = null;
            //print("Current Coroutine CLeaned");
        }
    }
}