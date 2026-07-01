namespace INART.SlothyBakery.Base
{
    /// <summary>
    /// Centralized enum definitions for the game.
    /// Keeps all enums in one place for easy reference and maintenance.
    /// </summary>
    public static class GameEnums
    {
        #region Sign Enums
        /// <summary>
        /// Mathematical comparison signs.
        /// </summary>
        public enum Sign
        {
            Less = 0,
            Greater = 1,
            Equal = 2
        }

        /// <summary>
        /// State of a sign UI element.
        /// </summary>
        public enum SignState
        {
            Placed,     // Sign is in its default position
            Active,     // Sign is being dragged/selected
            OnPlate     // Sign is placed on the answer plate
        }
        #endregion

        #region Game State Enums
        /// <summary>
        /// Overall game states (defined in GameStates class as well).
        /// </summary>
        public enum GameState
        {
            Load,
            Play,
            Restart,
            End
        }
        #endregion

        #region Minigame Enums
        /// <summary>
        /// Types of minigames available.
        /// </summary>
        public enum MinigameType
        {
            FractionComparison = 1,
            SignSelection = 2,
            FractionEquivalence = 3
        }
        #endregion

        #region Progression Enums
        /// <summary>
        /// How much progress each action contributes (defined in ProgressionCanvas as well).
        /// </summary>
        public enum ProgressionType
        {
            One,    // 100% progress
            Two,    // 50% progress
            Three   // 33.4% progress
        }
        #endregion

        #region Audio Enums
        /// <summary>
        /// Sound effect types (defined in SoundManager as well).
        /// </summary>
        public enum SoundType
        {
            Unlock,
            Success,
            Wrong,
            Selection1,
            Selection2
        }
        #endregion

        #region Dialogue Enums
        /// <summary>
        /// Types of dialogue phrases (defined in PhraseUtils as well).
        /// </summary>
        public enum PhraseType
        {
            INTRO,
            CONGRAT,
            END
        }
        #endregion
    }
}
