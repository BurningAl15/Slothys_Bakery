using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SignSelectable : MonoBehaviour
{
    [SerializeField] private Transform signPos;
    public Transform SignPos => signPos;
    
    [SerializeField] private Sign _sign;
    [SerializeField] private SignState _state;

    private Coroutine currentCoroutine = null;

    public Sign Sign
    {
        get => _sign;
        set => _sign = value;
    }

    public SignState State
    {
        get => _state;
        set => _state = value;
    }

    private Vector3 screenSpace;
    private Vector3 offset;

    [SerializeField] private Collider2D collider;
    
    public void Collider_Deactivate()
    {
        collider.enabled = false;
    }
    
    public void Collider_Activate()
    {
        collider.enabled = true;
    }
    
    public void Initialize()
    {
        signPos = transform;
        collider = GetComponent<Collider2D>();
    }
    
    private void OnMouseDown()
    {
        if (_state == SignState.Placed)
        {
            SignManager._instance.SetSelectable_Active(this);
            screenSpace = SignManager._instance.main.WorldToScreenPoint(signPos.position);
            offset = signPos.position -
                     SignManager._instance.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                         screenSpace.z));
            signPos.localScale = Vector2.one * SignUtils.maxScale;
        }
    }

    private void OnMouseDrag()
    {
        if (_state == SignState.Active)
        {
            var currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);
            
            var currentPosition = SignManager._instance.main.ScreenToWorldPoint(currentScreenSpace) + offset;

            signPos.position = currentPosition;
        }
    }
    
    private void OnMouseUp()
    {
        if (_state == SignState.Active)
        {
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(Checking(OverlapChecking()));
        }
        else if (_state == SignState.OnPlate)
        {
            //Reset by coroutine
            signPos.localScale = Vector2.one * SignUtils.maxScale;
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(Reset(SignManager._instance.GetInitialPos(Sign)));
        }
    }

    public void _Reset(Sign _sign)
    {
        if (currentCoroutine == null)
            currentCoroutine = StartCoroutine(Reset(SignManager._instance.GetInitialPos(_sign)));
    }
    
    Vector3 OverlapChecking()
    {
        Collider2D tempCollider = Physics2D.OverlapCircle(signPos.position, SignUtils.collisionRadius, SignManager._instance.collisionLayer);
        if (tempCollider)
            return tempCollider.transform.position;

        return signPos.transform.position;
    }

    bool OverlapChecking_ResetCheck()
    {
        return Physics2D.OverlapCircle(signPos.position, SignUtils.collisionRadius, SignManager._instance.collisionLayer);
    }

    void IsFull()
    {
        Collider2D temp = Physics2D.OverlapCircle(signPos.position, SignUtils.checkEmptyRadius,
            SignManager._instance.checkEmptyLayer);

        if (temp != null)
        {
            SignSelectable tempSelectable = temp.gameObject.GetComponent<SignSelectable>();
            SignManager._instance.ResetSelectable(tempSelectable);
        }
    }

    IEnumerator Checking(Vector3 targetPos)
    {
        if(SignManager._instance.version2)
            IsFull();
        
        yield return Translate(targetPos);
        yield return Scale(true);

        ValueManager._instance.SetValue(Sign);
        // SoundManager._instance.Run_SFX(SoundManager.SoundType.Selection1);
        
        if (OverlapChecking_ResetCheck())
        {
            _state = SignState.OnPlate;
            gameObject.layer = SignUtils.placedLayer;
        }
        else
        {
            yield return Translate(SignManager._instance.GetInitialPos(_sign));
            _state = SignState.Placed;
            gameObject.layer = SignUtils.resetLayer;
        }
        
        if(currentCoroutine != null)
            currentCoroutine = null;
    }

    IEnumerator Reset(Vector3 targetPos)
    {
        yield return Translate(targetPos);
        yield return Scale(true);

        yield return null;
        _state = SignState.Placed;
        gameObject.layer = SignUtils.resetLayer;

        currentCoroutine = null;
    }

    IEnumerator Translate(Vector3 targetPos)
    {
        while ((targetPos - signPos.position).sqrMagnitude > Mathf.Epsilon)
        {
            signPos.position = Vector3.MoveTowards(signPos.position, targetPos, SignUtils.moveSpeed * Time.deltaTime);
            yield return null;
        } 
        signPos.position = targetPos;
    }
    
    IEnumerator Scale(bool fromMaxToMin)
    {
        float scale = fromMaxToMin ? SignUtils.maxScale : SignUtils.minScale;
        signPos.localScale = Vector3.one * scale;

        float timeStep = Time.deltaTime * SignUtils.timeStep;
        
        if (fromMaxToMin)
        {
            while (scale > SignUtils.minScale)
            {
                signPos.localScale = Vector3.one * scale;
                scale -= timeStep;
                yield return null;
            }

            scale = SignUtils.minScale;
        }
        else
        {
            while (scale < SignUtils.maxScale)
            {
                signPos.localScale = Vector3.one * scale;
                scale += timeStep;
                yield return null;
            }

            scale = SignUtils.maxScale;
        }

        signPos.localScale = Vector3.one * scale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1,0,0);
        Gizmos.DrawWireSphere(transform.position,SignUtils.collisionRadius);
        
        Gizmos.color = new Color(0,0,1);
        Gizmos.DrawWireSphere(transform.position,SignUtils.checkEmptyRadius);

    }
}
