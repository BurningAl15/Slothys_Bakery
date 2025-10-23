using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// using LoLSDK;

namespace INART.SlothyBakery.Base
{
    using Core;
    using SceneTransition;
    using SoundManager;
    using ProgressionCanvas;
    using CanvasAnimator;

    // [Serializable]
    // public class Minigame4_Waypoints
    // {
    //     [Header("Waypoint Positions")] public Transform startPosition;
    //     public Transform midPosition;
    //     public Transform endPosition;
    // }
    //
    // [Serializable]
    // public class Minigame4_Round
    // {
    //     public GameObject parentObject;
    //     public Transform objectLeft;
    //     public string fractionValue_Left;
    //     [Space] public Transform objectRight;
    //     public string fractionValue_Right;
    //     [Space] public int correctAnswer;
    // }

    public class Minigame4 : MiniGameParent
    {
        [SerializeField] List<Minigame4_Round> rounds = new List<Minigame4_Round>();

        [SerializeField] List<SignDraggable> signDraggables = new List<SignDraggable>();
        [SerializeField] private SignColliderCenter colliderCenter;

        [SerializeField] private Minigame4_Waypoints leftObject;
        [SerializeField] private TextMeshPro leftText;
        [SerializeField] private Minigame4_Waypoints rightObject;
        [SerializeField] private TextMeshPro rightText;


        [Header("Animation Properties")] [Range(1f, 2f)] [SerializeField]
        private float timeScaler;

        [SerializeField] private AnimationCurve _animationCurve;

        [SerializeField] private Button button;

        //Others
        private Coroutine currentCoroutine = null;
        private int index = 0;
        private bool next = false;

        private Color text_Color;

        enum Side
        {
            Left,
            Right,
            Both
        }

        void Start()
        {
            text_Color = leftText.color;
        }

        private void Update()
        {
            if (GameStates._instance.GetGameState() == GameStates.GameState.PLAY)
                if (next)
                    next = false;
        }

        void InitializeNewObjectInfo()
        {
            rounds[index].parentObject.SetActive(true);

            ManageAlpha(0, Side.Both);

            for (var i = 0; i < rounds.Count; i++)
            {
                rounds[i].objectLeft.transform.localPosition = leftObject.startPosition.localPosition;
                rounds[i].objectRight.transform.localPosition = rightObject.startPosition.localPosition;
            }
        }

        public override void Init()
        {
            //Set the initial settings
            //Give the parent of the elements that will be draggable
            //Set the initial position of the draggables that are active for that round
            button.interactable = false;
            InitializeNewObjectInfo();
        }

        public override IEnumerator TurnOnGame(float _delay)
        {
            yield return new WaitForSeconds(.2f);

            var scaledTime = Time.fixedDeltaTime * timeScaler;
            var tempText = "";

            float animationTime = 0;
            var moveDelay = 1f;
            button.interactable = false;

            while (animationTime <= moveDelay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / moveDelay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                rounds[index].objectLeft.transform.localPosition = Vector3.Lerp(leftObject.startPosition.localPosition,
                    leftObject.midPosition.localPosition, animationCurveValue);
                rounds[index].objectRight.transform.localPosition = Vector3.Lerp(
                    rightObject.startPosition.localPosition, rightObject.midPosition.localPosition,
                    animationCurveValue);

                yield return null;
            }

            SetValues();

            animationTime = 0;
            while (animationTime <= moveDelay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / moveDelay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                ManageAlpha(animationCurveValue, Side.Both);

                yield return null;
            }

            for (int i = 0; i < signDraggables.Count; i++)
                signDraggables[i].InitializeDraggable(true);

            colliderCenter.Init();

            yield return new WaitForSeconds(.25f);

            //Enable button
            button.interactable = true;

            currentCoroutine = null;
        }

        protected override IEnumerator Change(float _delay)
        {
            yield return new WaitForSeconds(.25f);
            var scaledTime = Time.fixedDeltaTime * timeScaler;
            var tempText = "";

            yield return StartCoroutine(
                ProgressionCanvas._instance.FillBar(index, ProgressionCanvas.ProgressionType.One));
            yield return StartCoroutine(ProgressionCanvas._instance.UnlockStuff(index));

            colliderCenter.ResetColliderEnabling();
            colliderCenter.Deactivate();

            for (int i = 0; i < signDraggables.Count; i++)
            {
                signDraggables[i].Deactivate();
                signDraggables[i].IsNotInRecognitionZone_WithoutCollider();
                signDraggables[i].IsSelected();
            }

            button.interactable = false;
            ValueManager._instance.ResetValue();

            float animationTime = 0;

            while (animationTime <= _delay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _delay);
                var animationCurveValue = _animationCurve.Evaluate(percent);

                rounds[index].objectLeft.transform.localPosition = Vector3.Lerp(leftObject.midPosition.localPosition,
                    leftObject.endPosition.localPosition, animationCurveValue);
                rounds[index].objectRight.transform.localPosition = Vector3.Lerp(rightObject.midPosition.localPosition,
                    rightObject.endPosition.localPosition, animationCurveValue);

                yield return null;
            }

            animationTime = 0;
            while (animationTime <= _delay)
            {
                animationTime += scaledTime;
                var percent = Mathf.Clamp01(animationTime / _delay);
                var animationCurveValue = 1 - _animationCurve.Evaluate(percent);

                ManageAlpha(animationCurveValue, Side.Both);

                yield return null;
            }

            rounds[index].parentObject.SetActive(false);
            ValueManager._instance.ResetValue();

            yield return new WaitForSeconds(.25f);
            //Warning(Aldhair): LolSDK
// #if UNITY_WEBGL
//             LOLSDK.Instance.SubmitProgress(0, index + 7, 18);
// #endif
            if (index < rounds.Count - 1)
            {
                index++;



                if (index == 1)
                {
                    yield return StartCoroutine(
                        CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.CONGRAT));
                }

                rounds[index].parentObject.SetActive(true);

// #if UNITY_WEBGL
//                 //LOLSDK.Instance.SpeakText("Minigame2_Question_" + index);
// #endif

                ////////////////////////////////////////////////////////////////////////////////////

                animationTime = 0;
                while (animationTime <= _delay)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _delay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    rounds[index].objectLeft.transform.localPosition = Vector3.Lerp(
                        leftObject.startPosition.localPosition, leftObject.midPosition.localPosition,
                        animationCurveValue);
                    rounds[index].objectRight.transform.localPosition = Vector3.Lerp(
                        rightObject.startPosition.localPosition, rightObject.midPosition.localPosition,
                        animationCurveValue);

                    yield return null;
                }

                SetValues();

                animationTime = 0;
                while (animationTime <= _delay)
                {
                    animationTime += scaledTime;
                    var percent = Mathf.Clamp01(animationTime / _delay);
                    var animationCurveValue = _animationCurve.Evaluate(percent);

                    ManageAlpha(animationCurveValue, Side.Both);

                    yield return null;
                }

                print("Slide Draggable Group");

                yield return new WaitForSeconds(.25f);

                for (int i = 0; i < signDraggables.Count; i++)
                    signDraggables[i].InitializeDraggable(true);

                colliderCenter.Init();

                button.interactable = true;
            }
            else
            {
                yield return StartCoroutine(CanvasAnimator._instance.InstructionsAnimation(false));
                yield return StartCoroutine(CanvasAnimator._instance.CanvasAnimation(.6f, PhraseUtils.PhraseType.END));
                yield return StartCoroutine(SceneTransitionController._instance.FadeEffect_OUT());
                SceneUtils.ToNextScene();
            }


            currentCoroutine = null;
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
            if (ValueManager._instance.GetValue() == rounds[index].correctAnswer)
            {
                if (currentCoroutine == null)
                    currentCoroutine = StartCoroutine(Change(1f));

                SoundManager._instance.Run_SFX(SoundManager.SoundType.Success);
            }
            else
            {
                //Reset everything            
                colliderCenter.ResetColliderEnabling();

                for (int i = 0; i < signDraggables.Count; i++)
                {
                    signDraggables[i].IsNotInRecognitionZone();
                    signDraggables[i].IsSelected();
                }

                ValueManager._instance.ResetValue();
                SoundManager._instance.Run_SFX(SoundManager.SoundType.Wrong);
                print("Nope :C");
            }
        }
    }
}