using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeModules : MonoBehaviour
{
    void Start()
    {
        SignManager.Instance.Initialize();
    }

  
}
