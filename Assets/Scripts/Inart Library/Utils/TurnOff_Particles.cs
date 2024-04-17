using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    public class TurnOff_Particles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;

        void Update()
        {
            if (!particles.IsAlive())
            {
                this.gameObject.SetActive(false);
            }
        }
    }   
}
