using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    using SoundManager;
    public class SoundSliders : MonoBehaviour
    {
        public void SetBackgroundMusicVolume(float _soundLevel)
        {
            BackgroundSoundManager.Instance.SetBackgroundMusicVolume(_soundLevel);
        }
   
        public void SetBsfxMusicVolume(float _soundLevel)
        {
            BackgroundSoundManager.Instance.SetBsfxMusicVolume(_soundLevel);
        }
    }
}
