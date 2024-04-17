using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;

    [Serializable]
    public class Minigame1_Stage
    {
        // public GameObject stageContainer;
        [SerializeField] private string stageNumber;
        public Minigame1_StageRenderer stage;
        public CuttableObject stage_CuttableObject1;
        public CuttableObject stage_CuttableObject2;

        public int signIndex;
    }

    [Serializable]
    public class Minigame1_Plate
    {
        public List<Transform> plates = new List<Transform>();
        public List<Transform> positions = new List<Transform>();
        public List<string> fractions = new List<string>();
    }

    public class MiniGame1 : MiniGameParent
    {
        [SerializeField] private List<Sprite> signList = new List<Sprite>();

        [SerializeField] private SpriteRenderer equalsSprite;
        [SerializeField] private List<Minigame1_Stage> minigameStages = new List<Minigame1_Stage>();
        [SerializeField] private int minigame1_Index = 0;

        [SerializeField] private Minigame1_Plate leftPlate;
        [SerializeField] private Minigame1_Plate rightPlate;

        private Coroutine currentCoroutine = null;
        private bool next = false;
        [SerializeField] private List<string> effects = new List<string>();

        private void Update()
        {
            if (GameStates._instance.GetGameState() == GameStates.GameState.PLAY)
                if (minigame1_Index < minigameStages.Count)
                {
                    if (minigameStages[minigame1_Index].stage_CuttableObject1.HasFinished() && !next)
                    {
#if UNITY_EDITOR
                        //Just to check if this stuff runs once per frame, if runs more than once per frame
                        //The coroutine below could run lot's of times and break or something
                        print("A");
#endif
                        //Check me, i could give problems but for now i work well :V
                        StartCoroutine(ProgressionCanvas._instance.FillBar(minigame1_Index,
                            ProgressionCanvas.ProgressionType.Two));

                        minigameStages[minigame1_Index].stage_CuttableObject2.Activate();
                        next = true;
                    }
                    else if (minigameStages[minigame1_Index].stage_CuttableObject2.HasFinished() && next &&
                             currentCoroutine == null)
                    {
#if UNITY_EDITOR
                        print("B");
#endif
                        if (currentCoroutine == null)
                            currentCoroutine = StartCoroutine(Change(1f));
                    }
                }
        }

        public override void Init()
        {
            SetColorToEquals(0);
            equalsSprite.sprite = signList[minigameStages[minigame1_Index].signIndex];

            //Deactivate everything
            for (var i = 0; i < minigameStages.Count; i++)
            {
                minigameStages[i].stage_CuttableObject1.SetFractionText(leftPlate.fractions[i], effects);
                minigameStages[i].stage_CuttableObject2.SetFractionText(rightPlate.fractions[i], effects);

                minigameStages[i].stage_CuttableObject1.Deactivate();
                minigameStages[i].stage_CuttableObject2.Deactivate();
            }

            ResetAllPositions();

            for (var i = 0; i < minigameStages.Count; i++) minigameStages[i].stage.TurnOffSprites();
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            yield return new WaitForSeconds(.2f);

            //Initialize First
            minigameStages[minigame1_Index].stage.TurnOnAllSprites();
            minigameStages[minigame1_Index].stage.TurnOnSprites();

            float movement = 0;
            while (movement <= 1)
            {
                leftPlate.plates[minigame1_Index].position = Vector3.Lerp(leftPlate.positions[0].position,
                    leftPlate.positions[1].position, movement);
                rightPlate.plates[minigame1_Index].position = Vector3.Lerp(rightPlate.positions[0].position,
                    rightPlate.positions[1].position, movement);
                movement += Time.deltaTime;
                yield return null;
            }

            minigameStages[minigame1_Index].stage_CuttableObject1.Activate();

            currentCoroutine = null;
        }

        protected override IEnumerator Change(float _delay)
        {
            var maxTime = _delay;
            float tempDelay = 0;

            yield return StartCoroutine(
                ProgressionCanvas._instance.FillBar(minigame1_Index, ProgressionCanvas.ProgressionType.Two));
            yield return StartCoroutine(ProgressionCanvas._instance.UnlockStuff(minigame1_Index));

            equalsSprite.sprite = signList[minigameStages[minigame1_Index].signIndex];

            //We Fade, the equals sign so we can see it
            while (tempDelay <= maxTime)
            {
                tempDelay += Time.fixedDeltaTime;
                SetColorToEquals(tempDelay / maxTime);

                yield return Time.fixedDeltaTime;
            }

            yield return new WaitForSeconds(1f);
            yield return new WaitForSeconds(_delay);

            tempDelay = 0;

            while (tempDelay <= maxTime)
            {
                tempDelay += Time.fixedDeltaTime;
                leftPlate.plates[minigame1_Index].position = Vector3.Lerp(leftPlate.positions[1].position,
                    leftPlate.positions[2].position, tempDelay);
                rightPlate.plates[minigame1_Index].position = Vector3.Lerp(rightPlate.positions[1].position,
                    rightPlate.positions[2].position, tempDelay);
                SetColorToEquals(1 - tempDelay / maxTime);

                yield return null;
            }

            minigameStages[minigame1_Index].stage.TurnSpritesAlpha_WithParameter(0, false);
            minigameStages[minigame1_Index].stage.TurnOffSprites();

            // ResetPositions();
            //Warning(Aldhair): LolSDK
            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SubmitProgress(0, minigame1_Index + 1, 18);
            // #endif
            //Main Loop
            if (minigame1_Index < minigameStages.Count - 1)
            {
                minigame1_Index++;

                if (minigame1_Index == 1)
                {
                    yield return StartCoroutine(
                        CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));
                }
                // #if UNITY_EDITOR
                //                 print("Index: " + minigame1_Index);
                // #endif
                _delay = maxTime;
                minigameStages[minigame1_Index].stage.TurnOnAllSprites();
                minigameStages[minigame1_Index].stage.TurnSpritesAlpha_WithParameter(1, true);
                while (_delay >= 0)
                {
                    _delay -= Time.fixedDeltaTime;
                    // SetColorToEquals(_delay/maxTime);
                    leftPlate.plates[minigame1_Index].position = Vector3.Lerp(leftPlate.positions[0].position,
                        leftPlate.positions[1].position, 1 - _delay / maxTime);
                    rightPlate.plates[minigame1_Index].position = Vector3.Lerp(rightPlate.positions[0].position,
                        rightPlate.positions[1].position, 1 - _delay / maxTime);

                    yield return Time.fixedDeltaTime;
                }

                minigameStages[minigame1_Index].stage_CuttableObject1.Activate();
            }
            //The last one
            else
            {
                ProgressionCanvas._instance.RunPartyParticles();
                yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }

            next = false;
            currentCoroutine = null;
        }

        #region Utils

        private void SetColorToEquals(float _value)
        {
            var tempColor = equalsSprite.color;
            equalsSprite.color = new Color(tempColor.r, tempColor.g, tempColor.b, _value);
        }

        void ResetAllPositions()
        {
            for (int i = 0; i < leftPlate.plates.Count; i++)
            {
                leftPlate.plates[i].position = leftPlate.positions[0].position;
                rightPlate.plates[i].position = rightPlate.positions[0].position;
            }
        }

        #endregion
    }
}