using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Minigame2_SpriteControl : MonoBehaviour
{
    [FormerlySerializedAs("_spriteRenderers_Left")] [SerializeField] private List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
    private List<float> _spriteRendererColors = new List<float>();

    private Coroutine currentCoroutine = null;
    
    public void Init()
    {
        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            _spriteRendererColors.Add(_spriteRenderers[i].GetAlpha());
            _spriteRenderers[i].ChangeAlpha(0);
        }
    }

    public void TurnOnSprites()
    {
        if (currentCoroutine == null)
            currentCoroutine = StartCoroutine(ChangeAlpha());
    }

    IEnumerator ChangeAlpha()
    {
        float currentTime = 0;
        float maxTime = .1f;
        while (currentTime<=maxTime)
        {
            for (int i = 0; i < _spriteRenderers.Count; i++)
                _spriteRenderers[i].ChangeAlpha(Mathf.Lerp(0,_spriteRendererColors[i],currentTime/maxTime));
       
            yield return null;
            currentTime += 0.01f;
        }
        currentCoroutine = null;
    }
}
