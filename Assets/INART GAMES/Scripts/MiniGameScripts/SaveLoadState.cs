using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    [System.Serializable]
    public class TrueBakeryData
    {
        public int actual_state = 0;
    }

    public class SaveLoadState : MonoBehaviour
    {
        public static SaveLoadState instance;
        [SerializeField, Header("Initial State Data")] TrueBakeryData trueBakeryData;
        void Awake()
        {
            instance = this;
        }
        // Start is called before the first frame update
        public int GetActualState()
        {
            return trueBakeryData.actual_state;
        }
        public void SetActualState(int state)
        {
            trueBakeryData.actual_state = state;
        }
        public void Save()
        {
            // LOLSDK.Instance.SaveState(trueBakeryData);
        }
        public void OnLoad(TrueBakeryData loadedBakeryData)
        {
            trueBakeryData = loadedBakeryData;
            int aux_current_progress = 0;
            switch (trueBakeryData.actual_state)
            {
                case 0:
                    aux_current_progress = 0;
                    break;
                case 1:
                    aux_current_progress = 6;
                    break;
                case 2:
                    aux_current_progress = 12;
                    break;
                default:
                    break;
            }
            // LOLSDK.Instance.SubmitProgress(0, aux_current_progress, 18);
        }
        public static void StateButtonInitialize<T>(Button newGameButton, Button continueButton, System.Action<T> callback)
                where T : class
        {

            // Hide while checking for data.
            newGameButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            // Check for valid state data, from server or fallback local ( PlayerPrefs )
            // LOLSDK.Instance.LoadState<T>(state =>
            // {
            //     if (state != null)
            //     {
            //     // Hook up and show continue only if valid data exists.
            //     //continueButton.onClick.AddListener(() =>
            //     //    {
            //             //newGameButton.gameObject.SetActive(false);
            //            // continueButton.gameObject.SetActive(false);
            //         // Broadcast saved progress back to the teacher app.
            //         callback(state.data);
            //         //LOLSDK.Instance.SubmitProgress(0, state.currentProgress, 18);
            //     //});

            //         continueButton.gameObject.SetActive(true);
            //     }

            //     newGameButton.gameObject.SetActive(true);
            // });
        }
    }
}