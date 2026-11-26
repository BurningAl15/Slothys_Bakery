using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using INART.SlothyBakery.Core;

public class SignManager : Singleton<SignManager>
{
    [SerializeField] private List<SignSelectable> _selectables = new List<SignSelectable>();
    [SerializeField] private List<Vector3> initialPositions = new List<Vector3>();
    [SerializeField] private Transform targetPos;
    public Camera main;
    public LayerMask collisionLayer;
    public LayerMask checkEmptyLayer;

    private Dictionary<SignSelectable, Vector3> initialPosDictionary = new Dictionary<SignSelectable, Vector3>();
    public bool version2;

    protected override bool PersistAcrossScenes => false; // Scene-specific
    
    protected override void OnAwake()
    {
        main = Camera.main;
    }

    public void Initialize()
    {
        var previousPositions = new Dictionary<SignSelectable, Vector3>(initialPosDictionary);
        initialPositions.Clear();
        initialPosDictionary.Clear();
        for (int i = 0; _selectables != null && i < _selectables.Count; i++)
        {
            if (_selectables[i] == null) continue;
            _selectables[i].Initialize();

            if (_selectables[i].SignPos == null) continue;
            Vector3 initialPosition = previousPositions.TryGetValue(_selectables[i], out Vector3 savedPosition)
                ? savedPosition
                : _selectables[i].SignPos.position;
            initialPositions.Add(initialPosition);
            initialPosDictionary[_selectables[i]] = initialPosition;
        }

    }

    public void SetSelectable_Active(SignSelectable _selectable)
    {
        if (_selectable == null) return;
        for (int i = 0; _selectables != null && i < _selectables.Count; i++)
        {
            if (_selectables[i] == null) continue;
            if (version2)
            {
                if (_selectables[i].State != SignState.OnPlate)
                    SignUtils.SetSignState_Placed(_selectables[i]);
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
        foreach (var pair in initialPosDictionary)
            if (pair.Key != null && pair.Key.Sign == _selectable)
                return pair.Value;

        return targetPos != null ? targetPos.position : Vector3.zero;
    }

    public Vector3 GetInitialPos(SignSelectable selectable)
    {
        if (selectable != null && initialPosDictionary.TryGetValue(selectable, out Vector3 position))
            return position;

        return selectable != null ? selectable.transform.position : (targetPos != null ? targetPos.position : Vector3.zero);
    }

    public void ResetAll()
    {
        for (int i = 0; _selectables != null && i < _selectables.Count; i++)
        {
            if (_selectables[i] == null) continue;
            _selectables[i]._Reset(_selectables[i].Sign);
        }
    }

    public void ResetSelectable(SignSelectable _selectable)
    {
        if (_selectable != null) _selectable._Reset(_selectable.Sign);
    }

    public void DisableColliders()
    {
        for (int i = 0; _selectables != null && i < _selectables.Count; i++)
        {
            if (_selectables[i] == null) continue;
            _selectables[i].Collider_Deactivate();
        }
    }
    
    public void EnableColliders()
    {
        for (int i = 0; _selectables != null && i < _selectables.Count; i++)
        {
            if (_selectables[i] == null) continue;
            _selectables[i].Collider_Activate();
        }
    }
}
