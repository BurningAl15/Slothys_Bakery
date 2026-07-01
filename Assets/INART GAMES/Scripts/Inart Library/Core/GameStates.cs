using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Core
{
    public class GameStates : Singleton<GameStates>
    {
        public enum States
        {
            Load,
            Play,
            Restart,
            End
        }

        [SerializeField] private States state;
        public States State => state;

        protected override bool PersistAcrossScenes => false; // Scene-specific state

        public void Set_GameState_Load()
        {
            state = States.Load;
        }

        public void Set_GameState_Play()
        {
            state = States.Play;
        }
        
        public void Set_GameState_Restart()
        {
            state = States.Restart;
        }
        
        public void Set_GameState_End()
        {
            state = States.End;
        }

        public States GetGameState()
        {
            return state;
        }

        public bool IsGamePlaying()
        {
            return state == States.Play;
        }
    }
}
