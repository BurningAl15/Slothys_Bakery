using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using INART.SlothyBakery.SceneTransition;

public class ToNextScene : MonoBehaviour
{
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            SceneUtils.ToNextScene();
    }
}
