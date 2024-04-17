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
            BackgroundSoundManager._instance.SetBackgroundMusicVolume(_soundLevel);
        }
   
        public void SetBSFXMusicVolume(float _soundLevel)
        {
            BackgroundSoundManager._instance.SetBSFXMusicVolume(_soundLevel);
        }
    }
}
