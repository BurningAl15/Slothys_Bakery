using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;

    public enum IsComparable
    {
        Yes, No
    }

    public enum InstructiveState
    {
        Start,
        Wait,
        Finish
    }

    /*
     Greater
     Equals
     Less
     */

    [Serializable]
    public class SignRenderer
    {
        public Sprite _sprite;
        public Sign _sign;
    }

    [Serializable]
    public class FractionRenderer
    {
        public List<SpriteRenderer> _sprites = new List<SpriteRenderer>();
        public FractionValue _fraction;

        public Transform plate;

        public int numerator;
        public int denominator;

        public void Initialize()
        {
            _fraction.InitializeFraction(numerator, denominator);
            for (int i = 0; i < _sprites.Count; i++)
                _sprites[i].color = new Color(.25f, .25f, .25f, .25f);

            for (int i = 0; i < numerator; i++)
                _sprites[i].color = new Color(1, 1, 1, 1);
        }

        public void UpdateAlpha(float _alpha)
        {
            _fraction.UpdateAlpha(_alpha);
        }
    }

    [Serializable]
    public class CompareFraction
    {
        public Sign _value;
        public IsComparable _isComparable;
        public FractionRenderer _left;
        public FractionRenderer _right;

        public SpriteRenderer _sprite;
        [HideInInspector]
        public Transform _signPos;

        public void Initialize()
        {
            _signPos = _sprite.transform;
            _left.Initialize();
            _right.Initialize();
            FractionExtensionMethods.Alpha(_sprite, 1);
        }

        public void InitializeSignPos(Transform _pos)
        {
            _signPos.transform.position = _pos.position;
        }

        public void TurnOn_FractionAlpha()
        {
            _left._fraction.UpdateAlpha(1);
            _right._fraction.UpdateAlpha(1);
        }

        public void TurnOn_SignAlpha()
        {
            FractionExtensionMethods.Alpha(_sprite, 1);
        }

        public void UpdateAlpha_Sign(float alpha)
        {
            FractionExtensionMethods.Alpha(_sprite, alpha);
        }

        public void SetInitialColor(Color _color)
        {
            _left._fraction.UpdateColor(_color);
            _right._fraction.UpdateColor(_color);
        }

        public void SetSprite(Sprite sprite)
        {
            _sprite.sprite = sprite;
        }
    }

    public class Minigame1_B : MiniGameParent
    {
        [SerializeField] private List<SignRenderer> signRenderers = new List<SignRenderer>();
        [SerializeField] private List<CompareFraction> compareFractions = new List<CompareFraction>();

        public List<Transform> positions_Left = new List<Transform>();
        public List<Transform> positions_Right = new List<Transform>();
        public List<Transform> positions_Center = new List<Transform>();
        [SerializeField] private int minigame1_Index = 0;

        private Dictionary<Sign, Sprite> spriteDictionary = new Dictionary<Sign, Sprite>();

        private Coroutine currentCoroutine = null;
        private bool next = false;

        [SerializeField] private InstructiveState _instructive;

        [SerializeField] private Minigame1_Logic _logic;

        [SerializeField] private AnimationCurve _animationCurve;

        void Awake()
        {
            for (int i = 0; i < signRenderers.Count; i++)
                spriteDictionary.Add(signRenderers[i]._sign, signRenderers[i]._sprite);

            _instructive = InstructiveState.Start;
        }

        Sprite GetSprite(Sign _sign)
        {
            Sprite temp;
            if (spriteDictionary.TryGetValue(_sign, out temp))
            {
                return temp;
            }
            print("ERROR LOOKING FOR A SPRITE");
            return null;
        }

        void Update()
        {
            if (GameStates._instance.GetGameState() == GameStates.GameState.PLAY)
            {
                if (minigame1_Index < compareFractions.Count)
                {
                    // !next;
                    if (_instructive == InstructiveState.Wait)
                    {
#if UNITY_EDITOR
                        //Just to check if this stuff runs once per frame, if runs more than once per frame
                        //The coroutine below could run lot's of times and break or something
                        // print("Waiting for response - Runs Just In Editor");
#endif
                        // minigameStages[minigame1_Index].stage_CuttableObject2.Activate();
                        // next = true;
                        if (_logic.IsCorrect)
                            _instructive = InstructiveState.Finish;
                    }
                    else if (_instructive == InstructiveState.Finish && currentCoroutine == null)
                    {
#if UNITY_EDITOR
                        // print("Finish Feedback - Runs Just In Editor");
#endif
                        if (currentCoroutine == null)
                            currentCoroutine = StartCoroutine(Change(1f));
                    }
                }
            }
        }

        public override void Init()
        {
            for (int i = 0; i < compareFractions.Count; i++)
            {
                compareFractions[i].Initialize();
                compareFractions[i].SetSprite(GetSprite(compareFractions[i]._value));
                compareFractions[i].InitializeSignPos(positions_Center[0]);
                compareFractions[i].SetInitialColor(_logic.normal);
            }
            _logic.Init(_animationCurve);
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            yield return new WaitForSeconds(Utils_Minigame1_Settings.waitTime);

            compareFractions[minigame1_Index].TurnOn_FractionAlpha();

            _logic.InitializeDenominator(compareFractions[minigame1_Index]);

            float movement = 0;
            float evaluate;
            float maxTime = Utils_Minigame1_Settings.maxTime_Moving;

            while (movement < maxTime)
            {
                evaluate = _animationCurve.Evaluate(movement / maxTime);

                movement += Time.fixedDeltaTime;
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._left.plate, positions_Left, 0,
                    evaluate);
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._right.plate, positions_Right, 0,
                    evaluate);

                yield return null;
            }

            yield return _logic.MoveMenu(true);

            _instructive = InstructiveState.Wait;
        }

        protected override IEnumerator Change(float _delay)
        {
            _logic.ResetIsCorrect();

            var maxTime = Utils_Minigame1_Settings.maxTime_Moving;

            float movement = 0;
            float evaluate;

            //Run Progression
            yield return StartCoroutine(
                ProgressionCanvas._instance.FillBar(minigame1_Index, ProgressionCanvas.ProgressionType.One));

            //Run Unlock when Progression Finishes
            yield return StartCoroutine(ProgressionCanvas._instance.UnlockStuff(minigame1_Index));

            //This should be a coroutine to make it soft but fast
            movement = 0;
            while (movement <= maxTime)
            {
                evaluate = _animationCurve.Evaluate(movement / maxTime);
                movement += Time.fixedDeltaTime;
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._signPos, positions_Center, 0,
                    evaluate);
                yield return null;
            }
            evaluate = _animationCurve.Evaluate(maxTime / maxTime);
            FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._signPos, positions_Center, 0,
                evaluate);

            yield return new WaitForSeconds(3f);
            yield return new WaitForSeconds(_delay);

            //Move from point 1 to point 2 (mid to end)
            movement = 0;
            while (movement <= maxTime)
            {
                evaluate = _animationCurve.Evaluate(movement / maxTime);

                movement += Time.fixedDeltaTime;
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._left.plate, positions_Left, 1,
                    evaluate);
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._right.plate, positions_Right, 1,
                    evaluate);
                FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._signPos, positions_Center, 1,
                    evaluate);
                yield return null;
            }
            evaluate = _animationCurve.Evaluate(maxTime / maxTime);
            FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._left.plate, positions_Left, 1,
                evaluate);
            FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._right.plate, positions_Right, 1,
                evaluate);
            FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._signPos, positions_Center, 1,
                evaluate);

            _instructive = InstructiveState.Start;


            // #if UNITY_WEBGL
            //             LOLSDK.Instance.SubmitProgress(0, minigame1_Index + 1, 18);
            // #endif

            //Main Loop
            if (minigame1_Index < compareFractions.Count - 1)
            {
                minigame1_Index++;

                //If we finish the first task, then run a second instructions
                if (minigame1_Index == 1)
                {
                    yield return _logic.MoveMenu(false);
                    yield return StartCoroutine(
                        CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));
                    yield return _logic.MoveMenu(true);
                }
#if UNITY_EDITOR
                print("Index: " + minigame1_Index + " - Runs Just In Editor");
#endif

                compareFractions[minigame1_Index].TurnOn_FractionAlpha();

                _logic.InitializeDenominator(compareFractions[minigame1_Index]);
                _logic.ResetIsCorrect();

                movement = 0;
                while (movement < maxTime)
                {
                    evaluate = _animationCurve.Evaluate(movement / maxTime);

                    movement += Time.fixedDeltaTime;
                    FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._left.plate, positions_Left, 0,
                        evaluate);
                    FractionExtensionMethods.Lerp(compareFractions[minigame1_Index]._right.plate, positions_Right, 0,
                        evaluate);

                    yield return null;
                }
            }
            //The last one
            else
            {
                yield return _logic.MoveMenu(false);

                ProgressionCanvas._instance.RunPartyParticles();
                yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(false, false, true));
                yield return StartCoroutine(CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
                SceneUtils.ToNextScene();
            }

            _instructive = InstructiveState.Wait;

            currentCoroutine = null;
        }
    }
}
