using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class FractionExtensionMethods
{
    public static void Alpha(this SpriteRenderer spriteRenderer, float alpha)
    {
        Color temp = spriteRenderer.color;
        spriteRenderer.color = new Color(temp.r, temp.g, temp.b, alpha);
    }
    
    public static void Alpha(this TextMeshProUGUI text, float alpha)
    {
        text.alpha = alpha;
    }
    
    public static void Alpha(this TextMeshPro text, float alpha)
    {
        text.alpha = alpha;
    }

    public static void Lerp(this Transform platePos, List<Transform> lerpPoints,int lerpInitialIndex, float lerpValue)
    {
        platePos.position = Vector3.Lerp(lerpPoints[lerpInitialIndex].position,
            lerpPoints[lerpInitialIndex + 1].position, lerpValue);
    }
    public static void Lerp(this Transform platePos, List<Vector3> lerpPoints,int lerpInitialIndex, float lerpValue)
    {
        platePos.position = Vector3.Lerp(lerpPoints[lerpInitialIndex],
            lerpPoints[lerpInitialIndex + 1], lerpValue);
    }
    
    public static void Lerp(this Transform platePos, Transform initialPos, Transform endPos, float lerpValue)
    {
        platePos.position = Vector3.Lerp(initialPos.position,
            endPos.position, lerpValue);
    }
    public static void Lerp(this RectTransform platePos, Vector3 initialPos, Vector3 endPos, float lerpValue)
    {
        platePos.localPosition = Vector3.Lerp(initialPos,
            endPos, lerpValue);
    }
}
