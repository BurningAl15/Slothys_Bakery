using System.Collections.Generic;
using Fatcat.Slothy.Bakery.DynamicMinigames.Payloads;
using Fatcat.Slothy.Core.Adapters;
using Fatcat.Slothy.Core.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.DynamicMinigames.Controllers
{
    /// <summary>
    /// Additive Bakery consumer of Slothy Core. Existing Bakery scenes remain unchanged.
    /// </summary>
    public sealed class BakeryQuestionController : MinigameAdapterBase
    {
        [SerializeField] private Text promptLabel;
        [SerializeField] private List<Button> optionButtons = new List<Button>();

        private BakeryQuestionLevelPayloadSO payload;
        private int mistakes;
        private bool answered;

        private void Awake()
        {
            for (var i = 0; i < optionButtons.Count; i++)
            {
                var index = i;
                if (optionButtons[i] != null)
                    optionButtons[i].onClick.AddListener(() => SelectOption(index));
            }
        }

        protected override void OnLevelStarted(LevelContext context)
        {
            payload = context.Definition != null
                ? context.Definition.payload as BakeryQuestionLevelPayloadSO
                : null;
            mistakes = 0;
            answered = false;

            if (payload == null || !payload.IsValid)
            {
                Debug.LogError($"[SlothyBakery] Invalid question payload for '{context.Definition?.levelId}'.");
                SetButtons(false);
                return;
            }

            if (promptLabel != null)
                promptLabel.text = string.IsNullOrEmpty(payload.promptKey) ? payload.prompt : payload.promptKey;

            for (var i = 0; i < optionButtons.Count; i++)
            {
                var button = optionButtons[i];
                if (button == null) continue;
                var visible = i < payload.options.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = string.IsNullOrEmpty(payload.options[i].textKey)
                        ? payload.options[i].text
                        : payload.options[i].textKey;
            }

            SetButtons(true);
        }

        public void SelectOption(int index)
        {
            if (!IsRunning || answered || payload == null || index < 0 || index >= payload.options.Count)
                return;

            if (index != payload.correctAnswerIndex)
            {
                mistakes++;
                if (index < optionButtons.Count && optionButtons[index] != null)
                    optionButtons[index].interactable = false;
                return;
            }

            answered = true;
            SetButtons(false);
            Complete(new MinigameResult
            {
                completed = true,
                score = Mathf.Max(0, 100 - mistakes * 10),
                stars = mistakes == 0 ? 3 : mistakes == 1 ? 2 : 1,
                attempts = mistakes + 1,
                mistakes = mistakes,
                accuracy = 1f / (mistakes + 1f)
            });
        }

        protected override void OnCleanedUp()
        {
            SetButtons(false);
            payload = null;
            mistakes = 0;
            answered = false;
        }

        private void SetButtons(bool interactable)
        {
            for (var i = 0; i < optionButtons.Count; i++)
                if (optionButtons[i] != null) optionButtons[i].interactable = interactable;
        }
    }
}
