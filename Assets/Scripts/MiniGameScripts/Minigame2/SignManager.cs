using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SignManager : MonoBehaviour
{
    public static SignManager _instance;

    [SerializeField] private List<SignSelectable> _selectables = new List<SignSelectable>();
    [SerializeField] private List<Vector3> initialPositions = new List<Vector3>();
    [SerializeField] private Transform targetPos;
    public Camera main;
    public LayerMask collisionLayer;
    public LayerMask checkEmptyLayer;

    private Dictionary<Sign, Vector3> initialPosDictionary = new Dictionary<Sign, Vector3>();

    public bool version2;
    
    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        main = Camera.main;
    }

    public void Initialize()
    {
        for(int i=0;i<_selectables.Count;i++)
            _selectables[i].Initialize();
        
        for(int i=0;i<_selectables.Count;i++)
            initialPositions.Add(_selectables[i].SignPos.position);

        for (int i = 0; i < _selectables.Count; i++)
            initialPosDictionary.Add(_selectables[i].Sign,initialPositions[i]);
    }

    public void SetSelectable_Active(SignSelectable _selectable)
    {
        for (int i = 0; i < _selectables.Count; i++)
        {
            if (version2)
            {
                if (_selectables[i].State != SignState.OnPlate)
                    SignUtils.SetSignState_Placed(_selectables[i]);
                // else
                    // _selectables[i]._Reset(_selectables[i].Sign);
            }
            else
            {
                if (_selectables[i].State != SignState.OnPlate)
                    SignUtils.SetSignState_Placed(_selectables[i]);
                else
                    _selectables[i]._Reset(_selectables[i].Sign);
            }
        }
        SignUtils.SetSignState_Active(_selectable);
    }

    public Vector3 GetInitialPos(Sign _selectable)
    {
        Vector3 temp;
        if (initialPosDictionary.TryGetValue(_selectable, out temp))
            return temp;

        return targetPos.position;
    }

    public void ResetAll()
    {
        // print("Called");
        for (int i = 0; i < _selectables.Count; i++)
        {
            // if (_selectables[i].State == SignState.OnPlate)
            _selectables[i]._Reset(_selectables[i].Sign);
        }
    }

    public void ResetSelectable(SignSelectable _selectable)
    {
        _selectable._Reset(_selectable.Sign);
    }

    public void DisableColliders()
    {
        for (int i = 0; i < _selectables.Count; i++)
            _selectables[i].Collider_Deactivate();
    }
    
    public void EnableColliders()
    {
        for (int i = 0; i < _selectables.Count; i++)
            _selectables[i].Collider_Activate();
    }
}
