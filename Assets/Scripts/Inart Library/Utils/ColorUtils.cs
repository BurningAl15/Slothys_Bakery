using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorUtils
{
    public static void ChangeAlpha(this SpriteRenderer _spriteRenderer, float _alphaValue)
    {
        Color tempColor = _spriteRenderer.color;
        _spriteRenderer.color = new Color(tempColor.r, tempColor.g, tempColor.b, _alphaValue);
    }

    public static float GetAlpha(this SpriteRenderer _spriteRenderer)
    {
        return _spriteRenderer.color.a;
    }
}
