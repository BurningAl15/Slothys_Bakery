using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace INART.SlothyBakery.Base
{
    using Core;
    using CanvasAnimator;
    using SceneTransition;
    using ProgressionCanvas;
    using Analytics;
    using DailyChallenge;

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
            if (_fraction == null || _sprites == null)
            {
                Debug.LogError("[Bakery MG1] Fraction renderer is missing its fraction or piece sprites.");
                return;
            }

            _fraction.InitializeFraction(numerator, denominator);
            for (int i = 0; i < _sprites.Count; i++)
                if (_sprites[i] != null)
                    _sprites[i].color = new Color(.25f, .25f, .25f, .25f);

            for (int i = 0; i < Mathf.Min(numerator, _sprites.Count); i++)
                if (_sprites[i] != null)
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

        /// <summary>
        /// One leg of a plate's or sign's path, as the pre-DOTween Lerp(points, from) did: snap to point
        /// <paramref name="from"/> and move to point from + 1. (The DOTween port moved to point "from" instead, so
        /// the plates stayed off screen on device.) Null when the object or the points are missing.
        /// </summary>
        static Tween MoveAlong(Transform t, List<Transform> points, int from, float duration, Ease ease)
        {
            if (t == null || points == null || points.Count < from + 2 || points[from] == null || points[from + 1] == null) return null;
            t.DOKill();
            t.position = points[from].position;
            return t.DOMove(points[from + 1].position, duration).SetEase(ease);
        }

        static void JoinIfAny(Sequence seq, Tween t) { if (t != null) seq.Join(t); }
        [SerializeField] private int minigame1_Index = 0;

        private Dictionary<Sign, Sprite> spriteDictionary = new Dictionary<Sign, Sprite>();

        private Coroutine currentCoroutine = null;
        private bool next = false;
        private bool contentReady;

        [SerializeField] private InstructiveState _instructive;

        [SerializeField] private Minigame1_Logic _logic;

        [SerializeField] private AnimationCurve _animationCurve;

        void Awake()
        {
            for (int i = 0; signRenderers != null && i < signRenderers.Count; i++)
            {
                if (signRenderers[i] == null || signRenderers[i]._sprite == null) continue;
                spriteDictionary[signRenderers[i]._sign] = signRenderers[i]._sprite;
            }

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
            if (GameStates.Instance.GetGameState() == GameStates.States.Play)
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
            contentReady = ValidateContent();
            minigame1_Index = 0;
            _instructive = InstructiveState.Start;
            if (!contentReady) return;

            if (Fatcat.Slothy.Bakery.Modes.BakerySession.OffStory &&
                Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.HasValue)
                Fatcat.Slothy.Bakery.Modes.BakerySession.ShuffleWithSeed(
                    compareFractions, Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.Value ^ 0x101);

            for (int i = 0; i < compareFractions.Count; i++)
            {
                compareFractions[i].Initialize();
                compareFractions[i].SetSprite(GetSprite(compareFractions[i]._value));
                compareFractions[i].InitializeSignPos(positions_Center[0]);
                compareFractions[i].SetInitialColor(_logic.normal);
            }
            _logic.Init(_animationCurve);

            int resumeIdx = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetRoundCheckpoint(0) : 0;
            minigame1_Index = Mathf.Clamp(resumeIdx, 0, compareFractions.Count - 1);
            ProgressionCanvas.Instance?.ApplyCompletedRounds(minigame1_Index);
        }

        private bool ValidateContent()
        {
            if (compareFractions == null || compareFractions.Count == 0 || _logic == null ||
                !_logic.HasRequiredQuestionPrompts || positions_Left == null || positions_Left.Count < 2 ||
                positions_Right == null || positions_Right.Count < 2 ||
                positions_Center == null || positions_Center.Count < 2 || _animationCurve == null ||
                _animationCurve.length == 0)
            {
                Debug.LogError("[Bakery MG1] Missing rounds, prompts, waypoints, or scene references.", this);
                return false;
            }

            for (int i = 0; i < compareFractions.Count; i++)
            {
                CompareFraction round = compareFractions[i];
                if (round == null || round._sprite == null || !spriteDictionary.ContainsKey(round._value) ||
                    !IsValidFraction(round._left) || !IsValidFraction(round._right))
                {
                    Debug.LogError($"[Bakery MG1] Invalid authored data at round {i}.", this);
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidFraction(FractionRenderer fraction)
        {
            if (fraction == null || fraction._fraction == null || fraction.plate == null ||
                fraction.denominator <= 0 || fraction.numerator < 0 || fraction._sprites == null ||
                fraction.numerator > fraction._sprites.Count)
                return false;

            for (int i = 0; i < fraction.numerator; i++)
                if (fraction._sprites[i] == null) return false;

            return true;
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            if (!contentReady) yield break;
            yield return new WaitForSeconds(Utils_Minigame1_Settings.waitTime);

            compareFractions[minigame1_Index].TurnOn_FractionAlpha();

            _logic.InitializeDenominator(compareFractions[minigame1_Index]);

            float maxTime = Mathf.Max(0.35f, Utils_Minigame1_Settings.maxTime_Moving);
            var enterSeq = DOTween.Sequence();
            // Same path as the original Lerp(points, 0): from point 0 (off screen) to point 1 (on the table).
            JoinIfAny(enterSeq, MoveAlong(compareFractions[minigame1_Index]._left.plate, positions_Left, 0, maxTime, Ease.OutCubic));
            JoinIfAny(enterSeq, MoveAlong(compareFractions[minigame1_Index]._right.plate, positions_Right, 0, maxTime, Ease.OutCubic));
            yield return enterSeq.WaitForCompletion();

            yield return _logic.MoveMenu(true);

            _instructive = InstructiveState.Wait;
        }

        protected override IEnumerator Change(float _delay)
        {
            _logic.ResetIsCorrect();

            var maxTime = Mathf.Max(0.35f, Utils_Minigame1_Settings.maxTime_Moving);

            //Run Progression
            yield return StartCoroutine(
                ProgressionCanvas.Instance.FillBar(minigame1_Index, ProgressionCanvas.ProgressionType.One));

            //Run Unlock when Progression Finishes
            yield return StartCoroutine(ProgressionCanvas.Instance.UnlockStuff(minigame1_Index));

            // Analytics + Daily Challenge + Checkpoint before transition delay
            AnalyticsManager.Instance?.TrackLevelCompleted($"minigame1_round_{minigame1_Index}", true);
            DailyChallengeManager.Instance?.AddProgress("complete_rounds", 1);
            if (minigame1_Index < compareFractions.Count - 1)
                SaveLoadState.Instance?.SaveRoundCheckpoint(0, SceneUtils.FirstMinigameScene, minigame1_Index + 1);
            else
                SaveLoadState.Instance?.CompleteStoryChapter(1, SceneUtils.SecondMinigameScene, false);

            // Bring sign in with DOTween
            var signIn = MoveAlong(compareFractions[minigame1_Index]._signPos, positions_Center, 0, maxTime, Ease.OutBack);
            if (signIn != null) yield return signIn.WaitForCompletion();

            yield return new WaitForSeconds(Mathf.Min(1.2f, 0.8f + _delay * 0.4f));

            // Slide current plates & sign out with DOTween
            var exitSeq = DOTween.Sequence();
            // Original Lerp(points, 1): from point 1 (table) to point 2 (out).
            JoinIfAny(exitSeq, MoveAlong(compareFractions[minigame1_Index]._left.plate, positions_Left, 1, maxTime, Ease.InCubic));
            JoinIfAny(exitSeq, MoveAlong(compareFractions[minigame1_Index]._right.plate, positions_Right, 1, maxTime, Ease.InCubic));
            JoinIfAny(exitSeq, MoveAlong(compareFractions[minigame1_Index]._signPos, positions_Center, 1, maxTime, Ease.InCubic));
            yield return exitSeq.WaitForCompletion();

            _instructive = InstructiveState.Start;

            //Main Loop
            if (minigame1_Index < compareFractions.Count - 1)
            {
                minigame1_Index++;

                //If we finish the first task, then run a second instructions
                if (minigame1_Index == 1)
                {
                    yield return _logic.MoveMenu(false);
                    yield return StartCoroutine(
                        CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));
                    yield return _logic.MoveMenu(true);
                }
#if UNITY_EDITOR
                print("Index: " + minigame1_Index + " - Runs Just In Editor");
#endif

                compareFractions[minigame1_Index].TurnOn_FractionAlpha();

                _logic.InitializeDenominator(compareFractions[minigame1_Index]);
                _logic.ResetIsCorrect();

                var nextSeq = DOTween.Sequence();
                JoinIfAny(nextSeq, MoveAlong(compareFractions[minigame1_Index]._left.plate, positions_Left, 0, maxTime, Ease.OutCubic));
                JoinIfAny(nextSeq, MoveAlong(compareFractions[minigame1_Index]._right.plate, positions_Right, 0, maxTime, Ease.OutCubic));
                yield return nextSeq.WaitForCompletion();
            }
            //The last one
            else
            {
                yield return _logic.MoveMenu(false);

                ProgressionCanvas.Instance.RunPartyParticles();
                yield return StartCoroutine(CanvasAnimator.Instance.InstructionsAnimation(false, false, true));
                yield return StartCoroutine(CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                if (SceneTransitionController.Instance != null && SceneTransitionController.Instance.gameObject.activeInHierarchy)
                    yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
                SceneUtils.ToNextScene();
            }

            _instructive = InstructiveState.Wait;

            currentCoroutine = null;
        }
    }
}
