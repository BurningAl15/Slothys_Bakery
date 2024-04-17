using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class RenderableObject
{
    public bool shouldRenderOnStart = true;
    public bool isSpriteRenderer = true;
    public SpriteRenderer spriteRenderer;
    public TextMeshPro textMesh;
}

public class Minigame1_StageRenderer : MonoBehaviour
{
    //In case you just need to turn on all the elements you want from the start
    // [SerializeField] private List<SpriteRenderer> sprites = new List<SpriteRenderer>();

    //In case you just need to turn on some elements you want to choose from the start
    [SerializeField] private List<RenderableObject> spriteObjects = new List<RenderableObject>();

    private Coroutine currentCoroutine = null;

    public void TurnOffSprites()
    {
        gameObject.SetActive(false);

        // for (var i = 0; i < spriteObjects.Count; i++)
        // {
        //     if (spriteObjects[i].isSpriteRenderer)
        //         SetColor_Sprites(0, spriteObjects[i].spriteRenderer);
        //     else
        //         SetColor_TextMesh(0, spriteObjects[i].textMesh);
        // }
    }


    public void TurnOnAllSprites()
    {
        gameObject.SetActive(true);
    }

    public void TurnOnSprites()
    {
        for (var i = 0; i < spriteObjects.Count; i++)
        {
            if (spriteObjects[i].shouldRenderOnStart)
            {
                // SetColor_Sprites(1,spriteObjects[i].spriteRenderer);

                if (spriteObjects[i].isSpriteRenderer)
                    SetColor_Sprites(1, spriteObjects[i].spriteRenderer);
                else
                    SetColor_TextMesh(1, spriteObjects[i].textMesh);
            }
        }
    }

    #region Utils

    private void SetColor_Sprites(float _value, SpriteRenderer _sprite)
    {
        var tempColor = _sprite.color;
        _sprite.color = new Color(tempColor.r, tempColor.g, tempColor.b, _value);
    }

    private void SetColor_TextMesh(float _value, TextMeshPro _sprite)
    {
        var tempColor = _sprite.color;
        _sprite.color = new Color(tempColor.r, tempColor.g, tempColor.b, _value);
    }

    public void TurnSpritesAlpha_WithParameter(float _alpha, bool _isTurn)
    {
        for (var i = 0; i < spriteObjects.Count; i++)
        {
            if (_isTurn)
            {
                if (spriteObjects[i].shouldRenderOnStart)
                {
                    // SetColor_Sprites(_alpha,spriteObjects[i].spriteRenderer);
                    if (spriteObjects[i].isSpriteRenderer)
                        SetColor_Sprites(_alpha, spriteObjects[i].spriteRenderer);
                    else
                        SetColor_TextMesh(_alpha, spriteObjects[i].textMesh);
                }
            }
            else
            {
                // SetColor_Sprites(_alpha,spriteObjects[i].spriteRenderer);
                if (spriteObjects[i].isSpriteRenderer)
                    SetColor_Sprites(_alpha, spriteObjects[i].spriteRenderer);
                else
                    SetColor_TextMesh(_alpha, spriteObjects[i].textMesh);
            }
        }
    }

    #endregion

    #region Testing without the realistic data

    // public void TurnOffSprites(float _delay)
    // {
    //     if (currentCoroutine == null)
    //         currentCoroutine = StartCoroutine(Turn_Off(_delay));
    // }
    //
    // private IEnumerator Turn_Off(float _delay)
    // {
    //     print("Turning Off");
    //     var maxTime = _delay;
    //     while (_delay > 0)
    //     {
    //         _delay -= Time.fixedDeltaTime;
    //         
    //         for (var i = 0; i < sprites.Count; i++)
    //             SetColor_Sprites(_delay / maxTime, sprites[i]);
    //
    //         yield return Time.fixedDeltaTime;
    //     }
    //
    //     currentCoroutine = null;
    // }
    //
    // public void TurnOnSprites(float _delay)
    // {
    //     if (currentCoroutine == null)
    //         currentCoroutine = StartCoroutine(Turn_On(_delay));
    // }
    //
    // private IEnumerator Turn_On(float _delay)
    // {
    //     print("Turning On");
    //     float counter = 0;
    //     var maxTime = _delay;
    //     while (counter <= maxTime)
    //     {
    //         counter += Time.fixedDeltaTime;
    //         for (var i = 0; i < sprites.Count; i++) SetColor_Sprites(counter / maxTime, sprites[i]);
    //
    //         yield return Time.fixedDeltaTime;
    //     }
    //
    //     currentCoroutine = null;
    // }

    #endregion
}