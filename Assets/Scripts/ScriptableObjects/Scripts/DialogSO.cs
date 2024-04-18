using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.CanvasAnimator
{
    [CreateAssetMenu(fileName = "Dialog SO", menuName = "ScriptableObjects/DialogSO", order = 1)]
    public class DialogSO : ScriptableObject
    {
        [SerializeField] private PhraseUtils.PhraseType phraseType;
        public PhraseUtils.PhraseType PhraseType => phraseType;
        [SerializeField] private List<string> phrases = new();
        public List<string> Phrases => phrases;
    }
}

