using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueManager : MonoBehaviour
{
    public static ValueManager _instance;

    [SerializeField] private int value = -1;
   
    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if(_instance!=null)
            Destroy(this.gameObject);
    }

    public void ResetValue()
    {
        value = -1;
    }

    public void SetValue(Sign _sign)
    {
        switch (_sign)
        {
            case Sign.Equals:
                value = 1;
                break;
            case Sign.Less:
                value = 0;
                break;
            case Sign.Greater:
                value = 2;
                break;
        }
    }

    public int GetValue()
    {
        return value;
    }
}
