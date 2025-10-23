using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HintManager : MonoBehaviour
{
    public static HintManager _instance;

    [SerializeField] Vector2 initialPoint, endPoint;
    [SerializeField] AnimationCurve animCurve;
    [SerializeField] float maxTime;

    [SerializeField] RectTransform _messageTransform;
    Coroutine currentCoroutine = null;
    bool isOn = false;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        _messageTransform.localPosition = initialPoint;

    }

    public void TurnOn()
    {
        if (!isOn)
        {
            isOn = true;
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(MoveCoroutine());
        }
    }

    public void TurnOff()
    {
        if (isOn)
        {
            isOn = false;
            if (currentCoroutine == null)
                currentCoroutine = StartCoroutine(MoveCoroutine());
        }
    }

    IEnumerator MoveCoroutine()
    {
        float value = 0;
        Vector2 _initial = isOn ? initialPoint : endPoint;
        Vector2 _end = isOn ? endPoint : initialPoint;
        while (value <= maxTime)
        {
            _messageTransform.localPosition = Vector2.Lerp(_initial, _end, animCurve.Evaluate(value / maxTime));
            value += Time.deltaTime;
            yield return null;
        }
        _messageTransform.localPosition = _end;

        currentCoroutine = null;
    }
}