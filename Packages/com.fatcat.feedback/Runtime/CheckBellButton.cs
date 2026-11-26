using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Feedback
{
    /// <summary>
    /// The minigames' Check button as a café counter bell (owner chose it, 2026-11-21). It sits on its own cream
    /// mat, so it reads over any table art. Grey and still until there is an answer; gold and gently swinging
    /// when the child can check; a tap swings it with a "ding" and runs the button's original actions. A tap with
    /// no answer yet only shakes it "no" (it used to count as a mistake). Goes on the existing Button: its
    /// listeners (persistent or added in code) keep working.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class CheckBellButton : MonoBehaviour
    {
        [SerializeField] private RectTransform dome;
        [SerializeField] private Image domeImage;
        [SerializeField] private Sprite readySprite, offSprite;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip ding;

        /// <summary>Effects volume (0..1); the game sets it from its settings.</summary>
        public static Func<float> Volume = () => 1f;
        /// <summary>Light vibration on a ring; the game decides (its vibration setting).</summary>
        public static Action Haptic;

        Button button;
        Button.ButtonClickedEvent onCheck;
        Func<bool> readyWhen;
        bool shownReady, initialized;
        // Rung once: no more rings until the answer is taken away (wrong: the pieces go back) or a new round
        // starts (right: new plates) — readyWhen turns false. Without readyWhen, it rests a moment.
        bool spent;
        float spentAt;
        const float RestWithoutCondition = 1.5f;
        float holdLookUntil;
        Tween wobble;

        /// <summary>Whether checking makes sense now (e.g. a sign is in the slot). The button's own
        /// <c>interactable</c> still decides whether the round takes input at all.</summary>
        public void ReadyWhen(Func<bool> condition) => readyWhen = condition;

        public bool IsReady => button != null && button.interactable && !spent && (readyWhen == null || readyWhen());

        /// <summary>Rung and waiting for the answer to be cleared or a new round.</summary>
        public bool Spent => spent;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            // Keep the button's actions (persistent calls included) and ring first.
            onCheck = button.onClick;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(Press);
            SetLook(false, true);
        }

        private void OnDisable() { wobble?.Kill(); dome.DOKill(); transform.DOKill(); }

        private void Update()
        {
            if (Time.unscaledTime < holdLookUntil) return; // let the ring play out
            if (spent && (readyWhen != null ? !readyWhen() : Time.unscaledTime > spentAt + RestWithoutCondition)) spent = false;
            bool ready = IsReady;
            if (ready != shownReady || !initialized) SetLook(ready, false);
            else if (ready && (wobble == null || !wobble.IsActive())) SetLook(true, true);
        }

        void SetLook(bool ready, bool instant)
        {
            initialized = true;
            shownReady = ready;
            if (domeImage != null) domeImage.sprite = ready ? readySprite : offSprite;
            wobble?.Kill();
            dome.DOKill(); dome.localRotation = Quaternion.identity;
            if (!ready) return;
            if (!instant) transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 6, 0.6f); // "you can ring me now"
            wobble = DOTween.Sequence()
                .Append(dome.DOLocalRotate(new Vector3(0, 0, 6f), 0.35f).SetEase(Ease.InOutSine))
                .Append(dome.DOLocalRotate(new Vector3(0, 0, -6f), 0.7f).SetEase(Ease.InOutSine))
                .Append(dome.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.InOutSine))
                .AppendInterval(0.6f)
                .SetLoops(-1);
        }

        /// <summary>Tapped (also callable from tests).</summary>
        public void Press()
        {
            if (button == null || !button.interactable || spent) return; // one ring per answer
            if (!IsReady)
            {
                transform.DOComplete();
                transform.DOShakeRotation(0.35f, new Vector3(0, 0, 10f), 14, 0f);
                return;
            }
            wobble?.Kill();
            dome.DOKill();
            dome.localRotation = Quaternion.identity;
            DOTween.Sequence()
                .Append(dome.DOLocalRotate(new Vector3(0, 0, -16f), 0.08f))
                .Append(dome.DOLocalRotate(new Vector3(0, 0, 12f), 0.12f))
                .Append(dome.DOLocalRotate(new Vector3(0, 0, -6f), 0.12f))
                .Append(dome.DOLocalRotate(Vector3.zero, 0.15f))
                .SetTarget(dome);
            transform.DOComplete();
            transform.DOPunchScale(new Vector3(0.1f, -0.12f, 0f), 0.25f, 4, 0.5f);
            if (audioSource != null && ding != null) audioSource.PlayOneShot(ding, Mathf.Clamp01(Volume()));
            Haptic?.Invoke();
            holdLookUntil = Time.unscaledTime + 0.6f;
            spent = true; spentAt = Time.unscaledTime;
            onCheck?.Invoke();
        }
    }
}
