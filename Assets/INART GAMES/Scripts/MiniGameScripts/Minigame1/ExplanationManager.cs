using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using INART.SlothyBakery.Core;

/// <summary>
/// Tutorial/explanation UI for Minigame 1.
/// Shows step-by-step fraction instructions with animated fades.
///
/// Migrated from manual alpha-loop coroutines to DOTween DOFade.
/// The public Instruction() coroutine is preserved so CanvasAnimator
/// can still yield on it unchanged.
/// </summary>
public class ExplanationManager : Singleton<ExplanationManager>
{
    // ── Inspector — main container ─────────────────────────────────────────
    [SerializeField] private CanvasGroup wholeGroup;
    [SerializeField] private Button      understoodButton;
    [SerializeField] private TextMeshProUGUI denominator, numerator;
    [SerializeField] private Image           fraction;
    [SerializeField] private Color           highlightColor;

    // ── Inspector — title section ──────────────────────────────────────────
    [Header("Title")]
    [SerializeField] private CanvasGroup title;
    [SerializeField] private CanvasGroup group1;
    [SerializeField] private CanvasGroup group2;

    // ── Inspector — explanation 1 ──────────────────────────────────────────
    [Header("Explanation 1")]
    [SerializeField] private CanvasGroup title_explanation_1;
    [SerializeField] private CanvasGroup explanation1_1_fraction;
    [SerializeField] private CanvasGroup simbol_1;
    [SerializeField] private CanvasGroup explanation1_2_fraction;

    // ── Inspector — explanation 2 ──────────────────────────────────────────
    [Header("Explanation 2")]
    [SerializeField] private CanvasGroup title_explanation_2;
    [SerializeField] private CanvasGroup explanation2_1_fraction;
    [SerializeField] private CanvasGroup simbol_2;
    [SerializeField] private CanvasGroup explanation2_2_fraction;
    [SerializeField] private CanvasGroup arrow;
    [Space]
    [SerializeField] private CanvasGroup explanation2_3_fraction;
    [SerializeField] private CanvasGroup simbol_3;
    [SerializeField] private CanvasGroup explanation2_4_fraction;
    [SerializeField] private CanvasGroup simbol_4;
    [SerializeField] private CanvasGroup explanation2_5_fraction;
    [Space]
    [SerializeField] private CanvasGroup title_explanation_3;
    [SerializeField] private CanvasGroup explanation2_6_fraction;
    [SerializeField] private CanvasGroup simbol_5;
    [SerializeField] private CanvasGroup explanation2_7_fraction;

    // ── Inspector — timings ────────────────────────────────────────────────
    [Header("Timings")]
    [SerializeField] private float fadeDuration  = 0.2f;
    [SerializeField] private float shortPause    = 0.15f;
    [SerializeField] private float colorDuration = 0.2f;

    // ── Singleton config ───────────────────────────────────────────────────
    protected override bool PersistAcrossScenes => false;

    private Tween _buttonTween;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override void OnAwake() => InitializeAll();

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full step-by-step tutorial sequence.
    /// Called by CanvasAnimator via: yield return StartCoroutine(Instruction())
    /// </summary>
    public IEnumerator Instruction()
    {
        yield return FadeIn(wholeGroup);
        yield return FadeIn(title);

        // Section 1
        yield return FadeIn(group1);
        yield return FadeIn(title_explanation_1);
        yield return FadeIn(explanation1_1_fraction, explanation1_2_fraction);
        yield return new WaitForSeconds(1f);
        yield return FadeIn(simbol_1);
        yield return new WaitForSeconds(2f);

        // Section 2
        yield return FadeIn(group2);
        yield return FadeIn(title_explanation_2);
        yield return FadeIn(explanation2_1_fraction, explanation2_2_fraction);
        yield return new WaitForSeconds(1f);
        yield return FadeIn(simbol_2);
        yield return FadeIn(arrow);
        yield return LerpHighlightColor();

        yield return FadeIn(explanation2_3_fraction);
        yield return FadeIn(simbol_3);
        yield return FadeIn(explanation2_4_fraction);
        yield return FadeIn(simbol_4);
        yield return FadeIn(explanation2_5_fraction);

        yield return FadeIn(title_explanation_3);
        yield return FadeIn(explanation2_6_fraction);
        yield return FadeIn(simbol_5);
        yield return FadeIn(explanation2_7_fraction);

        yield return new WaitForSeconds(2f);

        // Show "understood" button
        var btnCg = understoodButton.GetComponent<CanvasGroup>();
        yield return FadeIn(btnCg);
        understoodButton.interactable = true;
    }

    /// <summary>Hides the entire explanation panel instantly.</summary>
    public void TurnOff() => wholeGroup.alpha = 0f;

    /// <summary>Fades out and disables the "understood" button.</summary>
    public void TurnOffButton()
    {
        _buttonTween?.Kill();
        var btnCg = understoodButton.GetComponent<CanvasGroup>();
        _buttonTween = DOTween.Sequence()
            .AppendInterval(0.25f)
            .Append(btnCg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                understoodButton.interactable = false;
                understoodButton.gameObject.SetActive(false);
                _buttonTween = null;
            });
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>Fades a single CanvasGroup to 1 and pauses briefly after.</summary>
    private IEnumerator FadeIn(CanvasGroup cg)
    {
        yield return cg.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad).WaitForCompletion();
        yield return new WaitForSeconds(shortPause);
    }

    /// <summary>Fades two CanvasGroups to 1 simultaneously and pauses briefly after.</summary>
    private IEnumerator FadeIn(CanvasGroup cg1, CanvasGroup cg2)
    {
        var seq = DOTween.Sequence()
            .Append(cg1.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad))
            .Join(cg2.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));
        yield return seq.WaitForCompletion();
        yield return new WaitForSeconds(shortPause);
    }

    /// <summary>Tweens the fraction image and text to the highlight color.</summary>
    private IEnumerator LerpHighlightColor()
    {
        var seq = DOTween.Sequence()
            .Append(fraction.DOColor(highlightColor, colorDuration).SetEase(Ease.Linear))
            .Join(denominator.DOColor(highlightColor, colorDuration).SetEase(Ease.Linear))
            .Join(numerator.DOColor(highlightColor, colorDuration).SetEase(Ease.Linear));
        yield return seq.WaitForCompletion();
        yield return new WaitForSeconds(shortPause);
    }

    /// <summary>Sets all CanvasGroups to alpha 0 and disables the button.</summary>
    private void InitializeAll()
    {
        CanvasGroup[] all =
        {
            wholeGroup, title, group1, group2,
            title_explanation_1, explanation1_1_fraction, simbol_1, explanation1_2_fraction,
            title_explanation_2, explanation2_1_fraction, simbol_2, explanation2_2_fraction,
            arrow, explanation2_3_fraction, simbol_3, explanation2_4_fraction,
            simbol_4, explanation2_5_fraction, title_explanation_3,
            explanation2_6_fraction, simbol_5, explanation2_7_fraction
        };

        foreach (var cg in all) cg.alpha = 0f;

        var btnCg = understoodButton.GetComponent<CanvasGroup>();
        btnCg.alpha               = 0f;
        understoodButton.interactable = false;
    }
}
