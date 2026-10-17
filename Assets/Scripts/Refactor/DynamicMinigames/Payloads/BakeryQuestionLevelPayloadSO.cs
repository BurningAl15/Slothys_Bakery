using System.Collections.Generic;
using Fatcat.Slothy.Core.Data;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.DynamicMinigames.Payloads
{
    [CreateAssetMenu(menuName = "FatCat/Bakery/Dynamic/Question Level", fileName = "BakeryQuestionLevel")]
    public sealed class BakeryQuestionLevelPayloadSO : ScriptableObject
    {
        public string promptKey;
        public string prompt = "Choose the correct answer";
        public List<QuizAnswerOption> options = new List<QuizAnswerOption>();
        public int correctAnswerIndex;

        public bool IsValid => options != null &&
                               options.Count > 0 &&
                               correctAnswerIndex >= 0 &&
                               correctAnswerIndex < options.Count;
    }
}
