using UnityEngine;

namespace INART.SlothyBakery.DailyChallenge
{
    /// <summary>
    /// Defines one type of daily challenge.
    /// Create via: Assets → Create → INART → Daily Challenge Definition
    ///
    /// Each game has its own set of challenge definitions assigned
    /// to DailyChallengeManager in the Inspector.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DailyChallenge_",
        menuName = "INART/Daily Challenge Definition")]
    public class DailyChallengeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Internal key, used in analytics and save data. snake_case, no spaces.")]
        public string challengeKey = "complete_rounds";

        [Tooltip("Short title shown in the UI.")]
        public string title = "Completa rondas";

        [Tooltip("Description shown in the UI. Use {target} as placeholder for the number.")]
        [TextArea(2, 4)]
        public string description = "Completa {target} rondas hoy";

        [Header("Goal")]
        [Tooltip("How many units the player must accumulate to finish this challenge.")]
        public int targetValue = 3;

        [Header("Reward")]
        public RewardType rewardType = RewardType.Badge;

        [Tooltip("Icon shown when the challenge is completed (optional).")]
        public Sprite rewardIcon;
    }

    public enum RewardType
    {
        Badge,
        Sticker,
        HintUnlock,
        None
    }

    /// <summary>
    /// Runtime state for today's active challenge.
    /// Serialized to PlayerPrefs as JSON.
    /// </summary>
    [System.Serializable]
    public class DailyChallengeState
    {
        /// <summary>ISO-8601 date string of when this challenge was generated (UTC).</summary>
        public string dateUtc       = "";
        /// <summary>challengeKey from DailyChallengeDefinition.</summary>
        public string challengeKey  = "";
        /// <summary>Target value copied from the definition at generation time.</summary>
        public int    targetValue   = 0;
        /// <summary>Current progress toward the target.</summary>
        public int    progress      = 0;
        /// <summary>True once progress >= targetValue.</summary>
        public bool   isCompleted   = false;
        /// <summary>True once the reward has been claimed.</summary>
        public bool   rewardClaimed = false;
    }
}
