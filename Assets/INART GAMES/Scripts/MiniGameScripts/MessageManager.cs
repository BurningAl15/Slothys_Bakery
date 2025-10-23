using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    public static MessageManager _instance;

    [SerializeField] private RectTransform message;
    private Image messageImage;
    [SerializeField] private AnimationCurve _animationCurve;

    private Coroutine currentCoroutine = null;

    [SerializeField] private float maxSize;

    [SerializeField] private Sprite badMessage, goodMessage;
    
    void Awake()
    {
        if (_instance == null)
            _instance = this;

        messageImage = message.GetComponent<Image>();
        message.localScale = Vector3.one * 0;
    }

    public void Animate_Message(bool isBadMessage)
    {
        if (currentCoroutine == null)
            currentCoroutine = StartCoroutine(AnimateMessage(isBadMessage));
    }
    
    IEnumerator AnimateMessage(bool isBadMessage)
    {
        messageImage.sprite = !isBadMessage ? badMessage : goodMessage;
        messageImage.SetNativeSize();
        
        float value = 0;
        float maxTime = .1f;
        float evaluate = 0;
        
        while (value<maxTime)
        {
            value += Time.fixedDeltaTime;
            evaluate = _animationCurve.Evaluate(Mathf.Clamp01(value / maxTime));
            message.localScale = Vector2.Lerp(Vector3.one*0, Vector3.one*maxSize, evaluate);
            yield return null;
        }
        message.localScale = Vector2.Lerp(Vector3.one*0, Vector3.one*maxSize, 1);

        yield return new WaitForSeconds(.5f);
        
        while (value>0)
        {
            value -= Time.fixedDeltaTime;
            evaluate = _animationCurve.Evaluate(Mathf.Clamp01(value / maxTime));
            message.localScale = Vector2.Lerp(Vector3.one * 0, Vector3.one * maxSize, evaluate); 
            yield return null;
        }

        message.localScale = Vector2.Lerp(Vector3.one * 0, Vector3.one * maxSize, 0);

        currentCoroutine = null;
    }
}
 
