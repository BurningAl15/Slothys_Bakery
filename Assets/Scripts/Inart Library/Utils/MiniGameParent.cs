using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    public class MiniGameParent : MonoBehaviour
    {
        /// <summary>
        /// Set here everything you want to initialize in the start method
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Set everything that will be animated in the beginning
        /// </summary>
        /// <param name="_delay"></param>
        /// <returns></returns>
        public virtual IEnumerator TurnOnGame(float _delay)
        {
            yield return null;
        }

        /// <summary>
        /// Set everything that will change every time that a round finishes
        /// </summary>
        /// <param name="_delay"></param>
        /// <returns></returns>
        protected virtual IEnumerator Change(float _delay)
        {
            yield return null;
        }
    }
}