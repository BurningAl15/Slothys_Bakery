using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FractionValue : MonoBehaviour
{
   [SerializeField] private TextMeshPro numeratorText;
   [SerializeField] private TextMeshPro denominatorText;
   [SerializeField] private SpriteRenderer fractionBar;
    
   private int numerator;
   private int denominator;

   public int Numerator => numerator;
   public int Denominator => denominator;

   private Color tempColor;

   private int multiplyWith;
   public int MultiplyVal => multiplyWith;

   private void Awake()
   {
       tempColor = fractionBar.color;
   }

   public void InitializeFraction(int _numerator,int _denominator)
   {
       numerator = _numerator;
       denominator = _denominator;
       UpdateText();
       UpdateAlpha(0);
   }
   
   public void MultiplyWith(int _multiply)
   {
       multiplyWith = _multiply;
       int numeratorTemp = numerator * multiplyWith;
       int denominatorTemp = denominator * multiplyWith;

       UpdateText(numeratorTemp, denominatorTemp);
   }

   void UpdateText()
   {
       numeratorText.text = "" + numerator;
       denominatorText.text = "" + denominator;
   }
   
   void UpdateText(int _numerator,int _denominator)
   {
       numeratorText.text = "" + _numerator;
       denominatorText.text = "" + _denominator;
   }

   public void UpdateAlpha(float _alpha)
   {
       numeratorText.alpha = _alpha;
       denominatorText.alpha = _alpha;
       fractionBar.color = new Color(tempColor.r, tempColor.g, tempColor.b, _alpha);
   }

   public void UpdateColor(Color _color)
   {
       float h;
       float s;
       float v;

       Color temp=_color;
       Color.RGBToHSV(temp, out h,out s,out v);

       // print("Step 1) H: " + h + "- S: " + s + "- V: " + v);
       
       s += ConvertValue_100(37f);
       v += ConvertValue_100(-13);

       // print("Step 2) H: " + h + "- S: " + s + "- V: " + v);
       
       temp = Color.HSVToRGB(h, s, v);
       
       numeratorText.color = temp;
       denominatorText.color = temp;
       fractionBar.color = _color;
   }


   float ConvertValue_100(float _value)
   {
       // return Mathf.Lerp(0f, 255f, _value) / 255f;
       return _value / 100f;
   }
}
