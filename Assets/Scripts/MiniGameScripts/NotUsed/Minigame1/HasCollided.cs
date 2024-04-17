using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    using DesignPatterns;

    public class HasCollided : MonoBehaviour
    {
        public bool hasCollided = false;

        [Tooltip("Will get the element in runtime")] [SerializeField]
        private LineManager lineManager;

        public bool isMain = false;

        private void OnEnable()
        {
            lineManager = GetComponentInParent<LineManager>();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.tag == "Cut" && !hasCollided)
            {
                lineManager.IncreaseCutAmount();
                // print(lineManager.GetCutAmount());
                other.gameObject.GetComponent<CreateCuts>().RunSound();

                var newEnemy = GameObjectPool.instance.GetPooledObject("Particles", 0);
                if (newEnemy != null)
                {
                    var enemyRange = other.transform.position;
                    newEnemy.transform.position = enemyRange;
                    newEnemy.transform.rotation = Quaternion.identity;
                    newEnemy.SetActive(true);
                }

                GetComponent<Collider2D>().enabled = false;
                hasCollided = true;
            }
        }

        public void Reset()
        {
            lineManager.DecreaseCutAmount();
            GetComponent<Collider2D>().enabled = true;
            hasCollided = false;
        }
    }
}