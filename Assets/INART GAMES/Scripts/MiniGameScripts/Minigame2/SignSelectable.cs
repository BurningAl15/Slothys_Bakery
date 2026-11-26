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
        if (collider != null) collider.enabled = false;
    }
    
    public void Collider_Activate()
    {
        if (collider != null) collider.enabled = true;
    }
    
    public void Initialize()
    {
        signPos = transform;
        collider = GetComponent<Collider2D>();
    }
    
    private void OnMouseDown()
    {
        if (_state == SignState.Placed && SignManager.Instance != null && SignManager.Instance.main != null)
        {
            SignManager.Instance.SetSelectable_Active(this);
            screenSpace = SignManager.Instance.main.WorldToScreenPoint(signPos.position);
            offset = signPos.position -
                     SignManager.Instance.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                         screenSpace.z));
            signPos.localScale = Vector2.one * SignUtils.maxScale;
        }
    }

    private void OnMouseDrag()
    {
        if (_state == SignState.Active && SignManager.Instance != null && SignManager.Instance.main != null)
        {
            var currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);
            
            var currentPosition = SignManager.Instance.main.ScreenToWorldPoint(currentScreenSpace) + offset;

            signPos.position = currentPosition;
        }
    }
    
    private void OnMouseUp()
    {
        if (_state == SignState.Active && SignManager.Instance != null)
        {
            if (currentCoroutine == null)
            {
                Physics2D.SyncTransforms();
                currentCoroutine = StartCoroutine(Checking(OverlapChecking()));
            }
        }
        else if (_state == SignState.OnPlate)
        {
            //Reset by coroutine
            signPos.localScale = Vector2.one * SignUtils.maxScale;
            if (currentCoroutine == null && SignManager.Instance != null)
                currentCoroutine = StartCoroutine(Reset(SignManager.Instance.GetInitialPos(this)));
        }
    }

    public void _Reset(Sign _sign)
    {
        if (currentCoroutine == null && SignManager.Instance != null)
            currentCoroutine = StartCoroutine(Reset(SignManager.Instance.GetInitialPos(this)));
    }
    
    Vector3 OverlapChecking()
    {
        Collider2D tempCollider = FindDropTarget();
        if (tempCollider)
            return tempCollider.transform.position;

        return signPos.transform.position;
    }

    bool OverlapChecking_ResetCheck()
    {
        return FindDropTarget() != null;
    }

    private Collider2D FindDropTarget()
    {
        if (SignManager.Instance == null || collider == null) return null;

        Physics2D.SyncTransforms();
        Bounds itemBounds = collider.bounds;
        Vector2 margin = Vector2.one * SignUtils.collisionRadius;
        Collider2D[] candidates = Physics2D.OverlapAreaAll((Vector2)itemBounds.min - margin,
            (Vector2)itemBounds.max + margin, SignManager.Instance.collisionLayer);

        for (int i = 0; i < candidates.Length; i++)
        {
            Collider2D candidate = candidates[i];
            if (candidate == null || candidate == collider) continue;
            if (candidate.bounds.Intersects(itemBounds)) return candidate;
        }

        return null;
    }

    void IsFull()
    {
        Collider2D temp = Physics2D.OverlapCircle(signPos.position, SignUtils.checkEmptyRadius,
            SignManager.Instance.checkEmptyLayer);

        if (temp != null)
        {
            SignSelectable tempSelectable = temp.gameObject.GetComponent<SignSelectable>();
            if (tempSelectable != null && tempSelectable != this)
                SignManager.Instance.ResetSelectable(tempSelectable);
        }
    }

    IEnumerator Checking(Vector3 targetPos)
    {
        if(SignManager.Instance.version2)
            IsFull();
        
        yield return Translate(targetPos);
        yield return Scale(true);

        if (ValueManager._instance != null) ValueManager._instance.SetValue(Sign);
        // SoundManager.Instance.Run_SFX(SoundManager.SoundType.Selection1);
        
        if (OverlapChecking_ResetCheck())
        {
            _state = SignState.OnPlate;
            gameObject.layer = SignUtils.placedLayer;
        }
        else
        {
            yield return Translate(SignManager.Instance.GetInitialPos(this));
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
