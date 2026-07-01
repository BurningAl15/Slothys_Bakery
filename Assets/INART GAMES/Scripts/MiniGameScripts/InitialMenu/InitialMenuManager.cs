using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using INART.Framework.Utils;

namespace INART.SlothyBakery.Base
{
    using SceneTransition;

    /// <summary>
    /// Main menu manager. Handles the intro animation sequence and
    /// the sound-settings panel slide.
    ///
    /// Migrated from manual Lerp coroutines to DOTween Sequences.
    /// All public call-sites (CallNextScene, Call_SoundSettings, etc.) are unchanged.
    /// </summary>
    public class InitialMenuManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("UI Elements")]
        [SerializeField] private RectTransform slothImg;
        [SerializeField] private RectTransform titleImg;
        [FormerlySerializedAs("startButton")]
        [SerializeField] private Button[] startButtons;
        [SerializeField] private Button   continueButton;
        [SerializeField] private Button   soundButton;

        [Header("Sloth slide (Y positions)")]
        [SerializeField] private float slothImg_InitialY;
        [SerializeField] private float slothImg_EndY;

        [Header("Title slide (X positions)")]
        [SerializeField] private float titleImg_InitialX;
        [SerializeField] private float titleImg_EndX;

        [Header("Timings")]
        [SerializeField] private float elementDuration = 0.5f;
        [SerializeField] private float gapBetween      = 0.15f;
        [SerializeField] private AnimationCurve _animationCurve;

        [Header("Sound Settings panel")]
        [SerializeField] private CanvasGroup soundSettings;

        [Header("Click SFX")]
        [SerializeField] private AudioSource clickSource;

        // ── Runtime ────────────────────────────────────────────────────────────

        private Sequence _sequence;

        // Cached start/end positions computed once in Awake
        private Vector3 _slothStart, _slothEnd;
        private Vector3 _titleStart, _titleEnd;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            CachePositions();
            ResetToHidden();
            PlayIntroSequence();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void CallNextScene()
        {
            if (_sequence != null && _sequence.IsPlaying()) return;
            StartCoroutine(ToNextScene());
        }

        public void Call_SoundSettings()
        {
            if (_sequence != null && _sequence.IsPlaying()) return;
            PlaySoundSettingsSequence();
        }

        public void Call_InitialAnimation()
        {
            if (_sequence != null && _sequence.IsPlaying()) return;
            PlayReturnFromSoundSequence();
        }

        // ── Sequences ──────────────────────────────────────────────────────────

        /// <summary>Standard intro: sloth slides in, then title, then buttons, then sound button.</summary>
        private void PlayIntroSequence()
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            // 1. Sloth slides up
            _sequence.Append(
                slothImg.DOLocalMoveY(_slothEnd.y, elementDuration).SetEase(_animationCurve));

            // 2. Short pause then title slides in
            _sequence.AppendInterval(gapBetween);
            _sequence.Append(
                titleImg.DOLocalMoveX(_titleEnd.x, elementDuration).SetEase(_animationCurve));

            // 3. Start buttons scale in (joined so they all animate together)
            for (int i = 0; i < startButtons.Length; i++)
            {
                var btn = startButtons[i].GetComponent<RectTransform>();
                if (i == 0)
                    _sequence.Append(btn.DOScale(1f, elementDuration).SetEase(Ease.OutBack));
                else
                    _sequence.Join(btn.DOScale(1f, elementDuration).SetEase(Ease.OutBack));
            }

            // 4. Sound button scales in
            _sequence.Append(
                soundButton.GetComponent<RectTransform>()
                           .DOScale(1f, elementDuration).SetEase(Ease.OutBack));

            // 5. Enable button animators when done
            _sequence.OnComplete(() =>
            {
                foreach (var btn in startButtons)
                {
                    var anim = btn.GetComponent<Animator>();
                    if (anim != null) anim.enabled = true;
                }
                _sequence = null;
            });
        }

        /// <summary>
        /// Reverse: hides the main UI elements, then reveals the sound-settings panel.
        /// </summary>
        private void PlaySoundSettingsSequence()
        {
            _sequence?.Kill();

            // Disable button animators first
            foreach (var btn in startButtons)
            {
                var anim = btn.GetComponent<Animator>();
                if (anim != null) anim.enabled = false;
            }

            _sequence = DOTween.Sequence();

            // Slide sloth back down
            _sequence.Append(
                slothImg.DOLocalMoveY(_slothStart.y, elementDuration).SetEase(_animationCurve));

            _sequence.AppendInterval(gapBetween);

            // Slide title back out
            _sequence.Append(
                titleImg.DOLocalMoveX(_titleStart.x, elementDuration).SetEase(_animationCurve));

            // Scale out start buttons and sound button simultaneously
            for (int i = 0; i < startButtons.Length; i++)
            {
                var btn = startButtons[i].GetComponent<RectTransform>();
                if (i == 0)
                    _sequence.Append(btn.DOScale(0f, elementDuration).SetEase(Ease.InBack));
                else
                    _sequence.Join(btn.DOScale(0f, elementDuration).SetEase(Ease.InBack));
            }
            _sequence.Join(
                soundButton.GetComponent<RectTransform>()
                           .DOScale(0f, elementDuration).SetEase(Ease.InBack));

            // Fade in sound settings panel
            _sequence.Append(
                soundSettings.DOFade(1f, elementDuration).SetEase(Ease.OutQuad));

            _sequence.OnComplete(() =>
            {
                soundSettings.interactable   = true;
                soundSettings.blocksRaycasts = true;
                _sequence = null;
            });
        }

        /// <summary>
        /// Reverse of PlaySoundSettingsSequence: hides settings panel and
        /// plays the intro sequence again.
        /// </summary>
        private void PlayReturnFromSoundSequence()
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            // Fade out sound settings
            soundSettings.interactable   = false;
            soundSettings.blocksRaycasts = false;

            _sequence.Append(
                soundSettings.DOFade(0f, elementDuration).SetEase(Ease.InQuad));

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                PlayIntroSequence();
            });
        }

        // ── Scene transitions (keep as coroutines — need to yield on FadeOut) ──

        private IEnumerator ToNextScene()
        {
            if (clickSource != null)
            {
                clickSource.Play();
                yield return new WaitWhile(() => clickSource.isPlaying);
            }

            yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void CachePositions()
        {
            var slothPos = slothImg.localPosition;
            _slothStart  = new Vector3(slothPos.x, slothImg_InitialY, slothPos.z);
            _slothEnd    = new Vector3(slothPos.x, slothImg_EndY,     slothPos.z);

            var titlePos = titleImg.localPosition;
            _titleStart  = new Vector3(titleImg_InitialX, titlePos.y, titlePos.z);
            _titleEnd    = new Vector3(titleImg_EndX,     titlePos.y, titlePos.z);
        }

        private void ResetToHidden()
        {
            slothImg.localPosition = _slothStart;
            titleImg.localPosition = _titleStart;

            foreach (var btn in startButtons)
            {
                btn.GetComponent<RectTransform>().localScale = Vector3.zero;
                var anim = btn.GetComponent<Animator>();
                if (anim != null) anim.enabled = false;
            }

            soundButton.GetComponent<RectTransform>().localScale = Vector3.zero;

            soundSettings.alpha          = 0f;
            soundSettings.interactable   = false;
            soundSettings.blocksRaycasts = false;
        }
    }
}
