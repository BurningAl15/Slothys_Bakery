using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    public class LineManager : MonoBehaviour
    {
        [Tooltip("Will get the element in runtime")] [SerializeField]
        private SpriteRenderer spriteRenderer;

        private bool turnOff = false;

        [SerializeField] private List<HasCollided> colliders = new List<HasCollided>();
        private int cutAmount = 0;

        [SerializeField] private CuttableObject cuttableObject;
        private int collidersQuantity;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (colliders.Count <= 0)
            {
                HasCollided[] temp = GetComponentsInChildren<HasCollided>();
                for (int i = 0; i < temp.Length; i++)
                {
                    colliders.Add(temp[i]);
                }
            }

            if (colliders[0] == null)
            {
                colliders.Clear();
                HasCollided[] temp = GetComponentsInChildren<HasCollided>();
                for (int i = 0; i < temp.Length; i++)
                {
                    colliders.Add(temp[i]);
                }
            }

            if (cuttableObject == null)
            {
                cuttableObject = GetComponentInParent<CuttableObject>();
            }

            collidersQuantity = colliders.Count;
        }

        private bool callOnce = false;

        private void Update()
        {
            if (!turnOff)
            {
                if (!CreateCuts._instance.IsCutting)
                {
                    callOnce = false;
                    if (cutAmount >= 0 && cutAmount <= 4 && !callOnce)
                    {
                        for (int i = 0; i < collidersQuantity; i++)
                        {
                            if (colliders[i].hasCollided)
                                colliders[i].Reset();
                        }

                        callOnce = true;
                    }
                }
                // else if(CreateCuts._instance.IsCutting && callOnce)
                // {
                //     callOnce = false;
                // }

                if (cutAmount >= collidersQuantity)
                {
                    cuttableObject.IncreaseLinesAmount();
                    var tempColor = spriteRenderer.color;
                    spriteRenderer.color = new Color(tempColor.r, tempColor.g, tempColor.b, 0);
                    turnOff = true;
                }
            }
        }

        #region Cut Amount Utils

        public void IncreaseCutAmount()
        {
            cutAmount++;
        }

        public void DecreaseCutAmount()
        {
            cutAmount--;
        }

        public int GetCutAmount()
        {
            return cutAmount;
        }

        #endregion

        public bool Get_TurnOff()
        {
            return turnOff;
        }
    }
}
