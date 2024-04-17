using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace INART.SlothyBakery.Core
{
    using CanvasAnimator;
    using SceneTransition;
    using Base;
    using SoundManager;

    public class GameManager : MonoBehaviour
    {
        private Coroutine currentCoroutine = null;

        [SerializeField] private MiniGameParent miniGame;
        [FormerlySerializedAs("testing")][SerializeField] private bool isTesting;
        [FormerlySerializedAs("instructionsOn")][SerializeField] private bool runAnimation = true;
        [FormerlySerializedAs("instructionsOn")][SerializeField] private bool turnOnInstructions = true;
        [FormerlySerializedAs("instructionsOn")][SerializeField] private bool turnOnQuestionButton = true;
        [SerializeField] private float canvasAnimationDuration;
        private void Awake()
        {
            // QualitySettings.vSyncCount = 1; // VSync must be disabled
            // Application.targetFrameRate = 60;
        }

        private void Start()
        {
            GameStates._instance.Set_GameState_Load();
            miniGame.Init();
            CanvasAnimator._instance.Test(isTesting);
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(StartGame(1f));
        }

        private IEnumerator StartGame(float _delayDuration)
        {
            //Fade In
            // SceneTransitionController._instance.FadeIn();
            yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_IN());
            yield return
                StartCoroutine(CanvasAnimator._instance.CanvasAnimation(canvasAnimationDuration, PhraseUtils.PhraseType.INTRO));

            if (runAnimation)
            {
                if (SceneUtils.IsCurrentSceneFirstLevel())
                    yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(true, turnOnInstructions, true));
                else
                    yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(true, turnOnInstructions, false));
            }
            yield return new WaitForSeconds(_delayDuration);

            yield return StartCoroutine(miniGame.TurnOnGame(.1f));
            // SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);

            if (SceneUtils.IsCurrentSceneFirstLevel())
                CanvasAnimator._instance.TurnButton(true);

            if (SceneUtils.IsCinematicScene())
            {
                yield return StartCoroutine(CanvasAnimator._instance.EnableTurnButton(true));
            }

            GameStates._instance.Set_GameState_Play();
            // SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
            currentCoroutine = null;
        }

        private IEnumerator ToNextScene(float _delay)
        {
            //Do something or run a shader
            // yield return new WaitForSeconds(_delay);
            // SceneTransitionController._instance.FadeOut();
            yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
            // yield return new WaitUntil(()=>SceneTransitionController._instance.EndAnimation);
            // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            currentCoroutine = null;
        }

        public void CallNextScene()
        {
            if (currentCoroutine == null) currentCoroutine = StartCoroutine(ToNextScene(3));
        }
    }
}