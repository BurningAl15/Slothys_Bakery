using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public enum Sign
{
    Greater,Equals,Less
}

public enum SignState
{
    Placed,Active,OnPlate
}

public class SignUtils : MonoBehaviour
{
    public static float collisionRadius = .75f;
    public static float checkEmptyRadius = 1.25f;
    // public static float 

    public static float minScale = 1f;
    public static float maxScale = 1.25f;
    public static float timeStep = 2f;

    public static float moveSpeed = 10f;

    public static int resetLayer = 8;
    public static int placedLayer = 10;
    
    public static void SetSignState_Placed(SignSelectable _selectable)
    {
        _selectable.State = SignState.Placed;
    }
    public static void SetSignState_Active(SignSelectable _selectable)
    {
        _selectable.State = SignState.Active;
    }
    public static void SetSignState_OnPlate(SignSelectable _selectable)
    {
        _selectable.State = SignState.OnPlate;
    }
}
