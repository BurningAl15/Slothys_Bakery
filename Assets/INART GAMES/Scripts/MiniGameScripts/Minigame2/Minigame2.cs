using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace INART.SlothyBakery.Base
{
    using Core;
    using SceneTransition;
    using SoundManager;
    using ProgressionCanvas;
    using CanvasAnimator;
    using Analytics;
    using DailyChallenge;

    public enum Side
    {
        Left,
        Right,
        Both
    }

    [Serializable]
    public class Minigame4_Waypoints
    {
        [Header("Waypoint Positions")] public Transform startPosition;
        public Transform midPosition;
        public Transform endPosition;
    }

    [Serializable]
    public class Minigame4_Round
    {
        public GameObject parentObject;
        public Minigame2_SpriteControl _spriteControl;
        public Transform objectLeft;
        public string fractionValue_Left;
        [Space]
        public Transform objectRight;
        public string fractionValue_Right;
        [Space]
        public int correctAnswer;
    }

    public class Minigame2 : MiniGameParent
    {
        [SerializeField] List<Minigame4_Round> rounds = new List<Minigame4_Round>();

        [SerializeField] private Minigame4_Waypoints leftObject;
        [SerializeField] private TextMeshPro leftText;
        [SerializeField] private Minigame4_Waypoints rightObject;
        [SerializeField] private TextMeshPro rightText;


        [Header("Animation Properties")]
        [Range(1f, 2f)]
        [SerializeField]
        private float timeScaler;

        [SerializeField] private AnimationCurve _animationCurve;

        [SerializeField] private Button button;

        //Others
        private Coroutine currentCoroutine = null;
        private int index = 0;
        private bool next = false;

        private Color text_Color;

        [Header("UX Properties")]
        [SerializeField] private float waitTime_UntilNextRow;

        private bool isTurnedOn = false;
        private bool contentReady;
        private bool answerAccepted;

        void Start()
        {
            contentReady = ValidateContent();
            if (!contentReady)
            {
                Debug.LogError("Minigame2 has incomplete round or animation data. Gameplay is disabled.", this);
                if (button != null) button.interactable = false;
                return;
            }

            text_Color = leftText.color;
            for (int i = 0; i < rounds.Count; i++)
                rounds[i]._spriteControl.Init();
            SaveLoadState.Instance?.EnterMinigame(1, SceneUtils.SecondMinigameScene);
        }

        private bool ValidateContent()
        {
            if (rounds == null || rounds.Count == 0 || leftText == null || rightText == null || button == null ||
                leftObject == null || rightObject == null || !HasWaypoints(leftObject) || !HasWaypoints(rightObject) ||
                _animationCurve == null || _animationCurve.length == 0 || timeScaler <= 0f ||
                float.IsNaN(timeScaler) || float.IsInfinity(timeScaler))
                return false;

            for (int i = 0; i < rounds.Count; i++)
            {
                Minigame4_Round round = rounds[i];
                if (round == null || round.parentObject == null || round._spriteControl == null ||
                    round.objectLeft == null || round.objectRight == null || round.correctAnswer < 0 || round.correctAnswer > 2 ||
                    string.IsNullOrWhiteSpace(round.fractionValue_Left) || string.IsNullOrWhiteSpace(round.fractionValue_Right))
                    return false;
            }

            return true;
        }

        private static bool HasWaypoints(Minigame4_Waypoints waypoints)
        {
            return waypoints != null && waypoints.startPosition != null && waypoints.midPosition != null &&
                   waypoints.endPosition != null;
        }

        private void Update()
        {
            if (GameStates.Instance.GetGameState() == GameStates.States.Play)
                if (next)
                    next = false;
        }

        void InitializeNewObjectInfo()
        {
            for (int i = 0; i < rounds.Count; i++)
                rounds[i].parentObject.SetActive(i == index);

            ManageAlpha(0, Side.Both);

            for (var i = 0; i < rounds.Count; i++)
            {
                rounds[i].objectLeft.transform.localPosition = leftObject.startPosition.localPosition;
                rounds[i].objectRight.transform.localPosition = rightObject.startPosition.localPosition;
            }
        }

        public override void Init()
        {
            contentReady = ValidateContent();
            if (!contentReady)
            {
                Debug.LogError("Minigame2 cannot initialize because its round data is incomplete.", this);
                if (button != null) button.interactable = false;
                return;
            }

            if (Fatcat.Slothy.Bakery.Modes.BakerySession.OffStory &&
                Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.HasValue)
                Fatcat.Slothy.Bakery.Modes.BakerySession.ShuffleWithSeed(
                    rounds, Fatcat.Slothy.Bakery.Modes.BakerySession.Seed.Value ^ 0x202);

            int resumeIdx = SaveLoadState.Instance != null ? SaveLoadState.Instance.GetRoundCheckpoint(1) : 0;
            index = Mathf.Clamp(resumeIdx, 0, rounds.Count - 1);
            next = false;
            isTurnedOn = false;
            answerAccepted = false;
            text_Color = leftText.color;
            //Set the initial settings
            //Give the parent of the elements that will be draggable
            //Set the initial position of the draggables that are active for that round
            button.interactable = false;
            InitializeNewObjectInfo();
            ProgressionCanvas.Instance?.ApplyCompletedRounds(index);
            if (SignManager.Instance == null)
            {
                contentReady = false;
                Debug.LogError("Minigame2 requires a SignManager in its scene.", this);
                return;
            }

            SignManager.Instance.Initialize();
            SignManager.Instance.DisableColliders();
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            if (!contentReady || index < 0 || index >= rounds.Count || currentCoroutine != null) yield break;

            yield return new WaitForSeconds(.2f);

            float moveDur = timeScaler > 0f ? 1f / timeScaler : 0.6f;
            button.interactable = false;

            yield return AnimateRoundPair(
                leftObject.startPosition.localPosition, leftObject.midPosition.localPosition,
                rightObject.startPosition.localPosition, rightObject.midPosition.localPosition,
                moveDur);

            SetValues();
            if (!isTurnedOn && rounds[index]._spriteControl != null)
            {
                rounds[index]._spriteControl.TurnOnSprites();
                isTurnedOn = true;
            }

            yield return AnimateRoundAlpha(0f, 1f, moveDur);

            SignManager.Instance.ResetAll();
            SignManager.Instance.EnableColliders();

            yield return new WaitForSeconds(.25f);

            //Enable button
            button.interactable = true;

            currentCoroutine = null;
        }

        protected override IEnumerator Change(float _delay)
        {
            if (!contentReady || index < 0 || index >= rounds.Count || timeScaler <= 0f || _delay <= 0f ||
                SignManager.Instance == null || ValueManager._instance == null)
            {
                currentCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(.25f);
            var scaledTime = Time.fixedDeltaTime * timeScaler;
            var tempText = "";

            if (!isTurnedOn)
            {
                rounds[index]._spriteControl.TurnOnSprites();
                isTurnedOn = true;
            }

            yield return StartCoroutine(
                ProgressionCanvas.Instance.FillBar(index, ProgressionCanvas.ProgressionType.One));
            yield return StartCoroutine(ProgressionCanvas.Instance.UnlockStuff(index));

            // Analytics + Daily Challenge + Checkpoint before transition delay
            AnalyticsManager.Instance?.TrackLevelCompleted($"minigame2_round_{index}", true);
            DailyChallengeManager.Instance?.AddProgress("complete_rounds", 1);
            if (index < rounds.Count - 1)
                SaveLoadState.Instance?.SaveRoundCheckpoint(1, SceneUtils.SecondMinigameScene, index + 1);
            else
                SaveLoadState.Instance?.CompleteStoryChapter(2, SceneUtils.ThirdMinigameScene, false);

            yield return new WaitForSeconds(waitTime_UntilNextRow);

            // colliderCenter.ResetColliderEnabling();
            // colliderCenter.Deactivate();
            //
            // for (int i = 0; i < signDraggables.Count; i++)
            // {
            //     signDraggables[i].Deactivate();
            //     signDraggables[i].IsNotInRecognitionZone_WithoutCollider();
            //     signDraggables[i].IsSelected();
            // }
            SignManager.Instance.DisableColliders();
            SignManager.Instance.ResetAll();

            button.interactable = false;
            ValueManager._instance.ResetValue();

            float stepDur = timeScaler > 0f ? _delay / timeScaler : _delay;

            yield return AnimateRoundPair(
                leftObject.midPosition.localPosition, leftObject.endPosition.localPosition,
                rightObject.midPosition.localPosition, rightObject.endPosition.localPosition,
                stepDur);

            yield return AnimateRoundAlpha(1f, 0f, stepDur);

            rounds[index].parentObject.SetActive(false);
            ValueManager._instance.ResetValue();

            yield return new WaitForSeconds(.25f);
            //Warning(Aldhair): LolSDK
            if (index < rounds.Count - 1)
            {
                index++;
                answerAccepted = false;

                if (index == 1)
                {
                    yield return StartCoroutine(
                        CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));
                }

                rounds[index].parentObject.SetActive(true);

                yield return AnimateRoundPair(
                    leftObject.startPosition.localPosition, leftObject.midPosition.localPosition,
                    rightObject.startPosition.localPosition, rightObject.midPosition.localPosition,
                    stepDur);

                SetValues();
                if (!isTurnedOn && rounds[index]._spriteControl != null)
                {
                    rounds[index]._spriteControl.TurnOnSprites();
                    isTurnedOn = true;
                }

                yield return AnimateRoundAlpha(0f, 1f, stepDur);

                // print("Slide Draggable Group");

                yield return new WaitForSeconds(.25f);

                // for (int i = 0; i < signDraggables.Count; i++)
                //     signDraggables[i].InitializeDraggable(true);
                //
                // colliderCenter.Init();
                SignManager.Instance.EnableColliders();

                button.interactable = true;
            }
            else
            {
                yield return StartCoroutine(CanvasAnimator.Instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator.Instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                if (SceneTransitionController.Instance != null && SceneTransitionController.Instance.gameObject.activeInHierarchy)
                    yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
                SceneUtils.ToNextScene();
            }

            isTurnedOn = false;

            currentCoroutine = null;
        }

        private YieldInstruction AnimateRoundPair(Vector3 leftFrom, Vector3 leftTo, Vector3 rightFrom, Vector3 rightTo, float duration)
        {
            var leftTf = rounds[index].objectLeft.transform;
            var rightTf = rounds[index].objectRight.transform;
            leftTf.DOKill();
            rightTf.DOKill();
            leftTf.localPosition = leftFrom;
            rightTf.localPosition = rightFrom;

            var seq = DOTween.Sequence();
            if (_animationCurve != null && _animationCurve.length > 0)
            {
                seq.Join(leftTf.DOLocalMove(leftTo, duration).SetEase(_animationCurve));
                seq.Join(rightTf.DOLocalMove(rightTo, duration).SetEase(_animationCurve));
            }
            else
            {
                seq.Join(leftTf.DOLocalMove(leftTo, duration).SetEase(Ease.OutCubic));
                seq.Join(rightTf.DOLocalMove(rightTo, duration).SetEase(Ease.OutCubic));
            }
            return seq.WaitForCompletion();
        }

        private YieldInstruction AnimateRoundAlpha(float from, float to, float duration)
        {
            var tween = DOVirtual.Float(from, to, duration, v => ManageAlpha(v, Side.Both));
            if (_animationCurve != null && _animationCurve.length > 0)
                tween.SetEase(_animationCurve);
            else
                tween.SetEase(Ease.OutQuad);
            return tween.WaitForCompletion();
        }

        void ManageAlpha(float _alpha, Side _side)
        {
            switch (_side)
            {
                case Side.Left:
                    leftText.color = new Color(text_Color.r, text_Color.g, text_Color.b, _alpha);

                    break;
                case Side.Right:
                    rightText.color = new Color(text_Color.r, text_Color.g, text_Color.b, _alpha);

                    break;
                case Side.Both:
                    leftText.color = new Color(text_Color.r, text_Color.g, text_Color.b, _alpha);
                    rightText.color = new Color(text_Color.r, text_Color.g, text_Color.b, _alpha);

                    break;
            }
        }

        void SetValues()
        {
            leftText.text = rounds[index].fractionValue_Left;
            rightText.text = rounds[index].fractionValue_Right;
        }

        public void CheckValue()
        {
            if (!contentReady || answerAccepted || currentCoroutine != null || index < 0 || index >= rounds.Count ||
                ValueManager._instance == null || SignManager.Instance == null)
                return;

            if (ValueManager._instance.GetValue() == rounds[index].correctAnswer)
            {
                answerAccepted = true;
                MessageManager.Instance.Animate_Message(true);
                button.interactable = false;

                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));

                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Success);
                AnalyticsManager.Instance?.TrackAnswerCorrect(index);
                Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(true);
                Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(true, SignManager.Instance != null ? SignManager.Instance.transform.position : Vector3.zero);
            }
            else
            {
                if (ValueManager._instance.GetValue() != -1)
                {
                    Fatcat.Slothy.Bakery.Modes.BakerySession.RecordAnswer(false);
                    Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.ShowAnswerStamp(false, SignManager.Instance != null ? SignManager.Instance.transform.position : Vector3.zero);
                }
                MessageManager.Instance.Animate_Message(false);
                if (!isTurnedOn)
                {
                    rounds[index]._spriteControl.TurnOnSprites();
                    isTurnedOn = true;
                }

                SignManager.Instance.ResetAll();
                ValueManager._instance.ResetValue();
                answerAccepted = false;
                SoundManager.Instance.Run_SFX(SoundManager.SoundType.Wrong);
                AnalyticsManager.Instance?.TrackAnswerWrong(index);
            }
        }
    }
}
