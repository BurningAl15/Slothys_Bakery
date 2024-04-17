using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace INART.SlothyBakery.Core
{
    public class GameStates : MonoBehaviour
    {
        public static GameStates _instance;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);
        }

        public enum GameState
        {
            LOAD,PLAY,RESTART,END
        }
        public GameState _gameStates;

        public void Set_GameState_Load()
        {
            _gameStates = GameState.LOAD;
        }
        public void Set_GameState_Play()
        {
            _gameStates = GameState.PLAY;
        }
        public void Set_GameState_Restart()
        {
            _gameStates = GameState.RESTART;
        }
        public void Set_GameState_End()
        {
            _gameStates = GameState.END;
        }

        public GameState GetGameState()
        {
            return _gameStates;
        }

        public bool IsGamePlaying()
        {
            return _gameStates == GameState.PLAY ? true : false;
        }
    }
}
