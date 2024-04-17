using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace INART.SlothyBakery.SoundManager
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager _instance;

        public enum SoundType
        {
            Unlock,
            Success,
            Wrong,
            Selection1,
            Selection2,

        }

        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private AudioClip unlockClip;

        [FormerlySerializedAs("aciertoClip")] [SerializeField]
        private AudioClip successClip;

        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip selection1Clip;
        [SerializeField] private AudioClip selection2Clip;


        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if(_instance!=null)
                Destroy(this.gameObject);
        }

        public void Run_SFX(SoundType _soundType)
        {
            AudioClip tempClip = null;
            switch (_soundType)
            {
                case SoundType.Unlock:
                    tempClip = unlockClip;
                    break;

                case SoundType.Success:
                    tempClip = successClip;
                    break;

                case SoundType.Wrong:
                    tempClip = wrongClip;
                    break;

                case SoundType.Selection1:
                    tempClip = selection1Clip;
                    break;

                case SoundType.Selection2:
                    tempClip = selection2Clip;
                    break;
            }

            sfxAudioSource.PlayOneShot(tempClip);
        }
    }
}