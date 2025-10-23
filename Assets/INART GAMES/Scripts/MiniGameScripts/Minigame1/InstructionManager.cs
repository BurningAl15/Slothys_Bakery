using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    private Coroutine currentCoroutine = null;
    [SerializeField] private AnimationCurve _animationCurve;

    private const float zero = 0;
    private const float max = .2f;


    private void Awake()
    {
        TurnOff();
    }

    void TurnOff()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    IEnumerator AnimateInstructions(bool isOn)
    {
        float evaluate = 0;
        float timer = isOn ? zero : max;
        float maxTime = isOn ? max : zero;

        if (isOn)
        {
            while (timer<maxTime)
            {
                timer += Time.fixedDeltaTime;
                evaluate = _animationCurve.Evaluate(Mathf.Clamp01(timer / maxTime));
                _canvasGroup.alpha = evaluate;
                yield return null;
            }
            // evaluate = 1;
            // _canvasGroup.alpha = evaluate;
            
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
        else
        {
            while (timer>maxTime)
            {
                timer -= Time.fixedDeltaTime;
                evaluate = _animationCurve.Evaluate(Mathf.Clamp01(timer / maxTime));
                _canvasGroup.alpha = evaluate;
                yield return null;
            }
            // evaluate = maxTime;
            // _canvasGroup.alpha = evaluate;
            
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        currentCoroutine = null;
    }

    public void OpenInstructions()
    {
        if (currentCoroutine == null)
            currentCoroutine = StartCoroutine(AnimateInstructions(true));
    }

    public void CloseInstructions()
    {
        if (currentCoroutine == null)
            currentCoroutine = StartCoroutine(AnimateInstructions(false));
    }
}
