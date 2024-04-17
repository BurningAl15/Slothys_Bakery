using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
   public class CounterManager : MonoBehaviour
   {
      public static CounterManager _instance;

      [SerializeField] private int counter = 0;

      private void Awake()
      {
         if (_instance == null)
            _instance = this;
         else if (_instance != null)
            Destroy(this.gameObject);
      }

      public void ResetCounter()
      {
         counter = 0;
      }

      public void AddCounter()
      {
         counter++;
      }

      public int GetCounterValue()
      {
         return counter;
      }
   }
}
