using UnityEngine;

namespace INART.SlothyBakery.Base
{
    /// <summary>
    /// Centralized game configuration ScriptableObject.
    /// Allows designers to tweak values without touching code.
    /// Can be toggled on/off via custom editor for legacy support.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Animation Settings")]
        [Tooltip("Duration for canvas fade animations")]
        [Range(0.1f, 3f)]
        public float canvasAnimationDuration = 1f;

        [Tooltip("Time scale multiplier for animations")]
        [Range(0.1f, 2f)]
        public float timeScaler = 1f;

        [Header("Dialogue Settings")]
        [Tooltip("Delay between each letter in typewriter effect")]
        [Range(0.01f, 0.2f)]
        public float waitUntilNextLetter = 0.05f;

        [Tooltip("Wait time before transitioning to next phrase")]
        [Range(0.1f, 2f)]
        public float waitUntilPrevious = 0.5f;

        [Header("Minigame Settings")]
        [Tooltip("Maximum time for fraction menu animations")]
        [Range(0.1f, 2f)]
        public float fractionMenuMaxTime = 0.5f;

        [Header("Audio Settings")]
        [Tooltip("Default music volume (0-1)")]
        [Range(0f, 1f)]
        public float defaultMusicVolume = 0.7f;

        [Tooltip("Default SFX volume (0-1)")]
        [Range(0f, 1f)]
        public float defaultSFXVolume = 0.8f;

        [Header("Progression Settings")]
        [Tooltip("Total number of levels in story mode")]
        public int totalStoryLevels = 18;

        [Tooltip("Number of daily challenges per day")]
        public int dailyChallengesPerDay = 3;

        #region Singleton Access
        private static GameConfig _instance;

        /// <summary>
        /// Gets the active GameConfig instance from Resources.
        /// Falls back to default values if not found.
        /// </summary>
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning("[GameConfig] No GameConfig found in Resources folder. Using default values.");
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}
