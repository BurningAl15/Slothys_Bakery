using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ExplanationManager : MonoBehaviour
{
   public static ExplanationManager _instance;

   [SerializeField] private CanvasGroup wholeGroup;

   #region Title

   [Header("Title")]
   [SerializeField] private CanvasGroup title;
   [SerializeField] private CanvasGroup group1;
   [SerializeField] private CanvasGroup group2;

   #endregion
   
   #region Explanation 1

   [Header("Explanation 1")] 
   [SerializeField]private CanvasGroup title_explanation_1;
   [SerializeField] private CanvasGroup explanation1_1_fraction;
   [SerializeField] private CanvasGroup simbol_1;
   [SerializeField] private CanvasGroup explanation1_2_fraction;

   #endregion

   #region Explanation 2

   [Header("Explanation 2")] 
   [SerializeField] private CanvasGroup title_explanation_2; 
   [SerializeField] private CanvasGroup explanation2_1_fraction; 
   [SerializeField] private CanvasGroup simbol_2; 
   [SerializeField] private CanvasGroup explanation2_2_fraction; 
   [SerializeField] private CanvasGroup arrow; 

   [Space]
   [SerializeField] private CanvasGroup explanation2_3_fraction; 
   [SerializeField] private CanvasGroup simbol_3; 
   [SerializeField] private CanvasGroup explanation2_4_fraction; 
   [SerializeField] private CanvasGroup simbol_4; 
   [SerializeField] private CanvasGroup explanation2_5_fraction; 
  
   [Space]
   [SerializeField] private CanvasGroup title_explanation_3; 
   [SerializeField] private CanvasGroup explanation2_6_fraction; 
   [SerializeField] private CanvasGroup simbol_5; 
   [SerializeField] private CanvasGroup explanation2_7_fraction;

   #endregion

   [SerializeField] private AnimationCurve _animationCurve;
   float maxTime = .2f;

   [SerializeField] private Button understoodButton;

   [SerializeField] private TextMeshProUGUI denominator, numerator;
   [SerializeField] private Image fraction;
   
   [SerializeField] private Color hightLightColor;
   
   private Coroutine currentCoroutine = null;
   
   private void Awake()
   {
      if (_instance == null)
         _instance = this;
      else
         Destroy(this.gameObject);
      InitializeAll();
   }

   public IEnumerator Instruction()
   {
      yield return TurnOn(wholeGroup);
      yield return TurnOn(title);
      
      //Turn On Instructions 1
      yield return TurnOn(group1);
      yield return TurnOn(title_explanation_1);

      yield return TurnOn(explanation1_1_fraction,explanation1_2_fraction);
      yield return new WaitForSeconds(1f);
      
      yield return TurnOn(simbol_1);
      yield return new WaitForSeconds(2f);
      
      // yield return TurnOff(group1);
      //
      // yield return new WaitForSeconds(1f);
      //Turn On Instructions 2
      yield return TurnOn(group2);

      yield return TurnOn(title_explanation_2);
      yield return TurnOn(explanation2_1_fraction,explanation2_2_fraction);
      yield return new WaitForSeconds(1f);

      yield return TurnOn(simbol_2);
      yield return TurnOn(arrow);
      yield return LerpColor(fraction, denominator, numerator);

      yield return TurnOn(explanation2_3_fraction);
      yield return TurnOn(simbol_3);
      yield return TurnOn(explanation2_4_fraction);
      yield return TurnOn(simbol_4);
      yield return TurnOn(explanation2_5_fraction);

      yield return TurnOn(title_explanation_3);
      yield return TurnOn(explanation2_6_fraction);
      yield return TurnOn(simbol_5);
      yield return TurnOn(explanation2_7_fraction);

      yield return new WaitForSeconds(2f);
      // yield return TurnOff(group2);
      CanvasGroup tempCanvas=understoodButton.GetComponent<CanvasGroup>();
      yield return TurnOn(tempCanvas);
      understoodButton.interactable = true;
   }

   public void TurnOff()
   {
      wholeGroup.alpha = 0;
   }
   
   IEnumerator LerpColor(Image img, TextMeshProUGUI _temp1, TextMeshProUGUI _temp2)
   {
      float value = 0;
      float evaluation = 0;
      Color initialColor = img.color; 
      while (value<maxTime)
      {
         evaluation = _animationCurve.Evaluate(value / maxTime);
         img.color = Color.Lerp(initialColor,hightLightColor,evaluation);
         _temp1.color=Color.Lerp(initialColor,hightLightColor,evaluation);
         _temp2.color=Color.Lerp(initialColor,hightLightColor,evaluation);
         value += Time.deltaTime;
         yield return null;
      }
      img.color = Color.Lerp(initialColor,hightLightColor,1);
      yield return new WaitForSeconds(.15f);
   }
   
   IEnumerator TurnOn(CanvasGroup _canvasGroup)
   {
      float value = 0;
      float evaluation = 0;
      while (value<maxTime)
      {
         evaluation = _animationCurve.Evaluate(value / maxTime);
         _canvasGroup.alpha = evaluation;
         value += Time.deltaTime;
         yield return null;
      }
      _canvasGroup.alpha = 1;
      yield return new WaitForSeconds(.15f);
   }
   IEnumerator TurnOn(CanvasGroup _canvasGroup,CanvasGroup _canvasGroup1)
   {
      float value = 0;
      float evaluation = 0;
      while (value<maxTime)
      {
         evaluation = _animationCurve.Evaluate(value / maxTime);
         _canvasGroup.alpha = evaluation;
         _canvasGroup1.alpha = evaluation;

         value += Time.deltaTime;
         yield return null;
      }
      
      _canvasGroup.alpha = 1;
      _canvasGroup1.alpha = 1;

      yield return new WaitForSeconds(.15f);
   }
   
   IEnumerator TurnOff(CanvasGroup _canvasGroup)
   {
      float value = maxTime;
      float evaluation = 0;
      while (value>0)
      {
         evaluation = _animationCurve.Evaluate(value / maxTime);
         _canvasGroup.alpha = evaluation;
         value -= Time.deltaTime;
         yield return null;
      }
      _canvasGroup.alpha = 0;
      yield return new WaitForSeconds(.15f);
   }

   public void InitializeAll()
   {
      wholeGroup.alpha = 0;

      #region Title
      
      title.alpha = 0;
      group1.alpha = 0;
      group2.alpha = 0;

      #endregion
      
      #region Explanation 1

      title_explanation_1.alpha = 0;
      explanation1_1_fraction.alpha = 0;
      simbol_1.alpha = 0;
      explanation1_2_fraction.alpha = 0;

      #endregion
      
      #region Explanation 2

      title_explanation_2.alpha = 0;
      explanation2_1_fraction.alpha = 0;
      simbol_2.alpha = 0;
      explanation2_2_fraction.alpha = 0;
      arrow.alpha = 0;
      
      //SubSection 1

      explanation2_3_fraction.alpha = 0;
      simbol_3.alpha = 0;
      explanation2_4_fraction.alpha = 0;
      simbol_4.alpha = 0;
      explanation2_5_fraction.alpha = 0;

      //SubSection 2
      title_explanation_3.alpha = 0;
      explanation2_6_fraction.alpha = 0;
      simbol_5.alpha = 0;
      explanation2_7_fraction.alpha = 0;

      #endregion

      understoodButton.GetComponent<CanvasGroup>().alpha = 0;
      understoodButton.interactable = false;
   }

   public void TurnOffButton()
   {
      if (currentCoroutine == null)
         currentCoroutine = StartCoroutine(TurnOff_Button());
   }

   IEnumerator TurnOff_Button()
   {
      yield return new WaitForSeconds(.25f);
      CanvasGroup tempCanvas=understoodButton.GetComponent<CanvasGroup>();
      yield return TurnOff(tempCanvas);
      understoodButton.interactable = false;
      understoodButton.gameObject.SetActive(false);
      currentCoroutine = null;
   }
}
