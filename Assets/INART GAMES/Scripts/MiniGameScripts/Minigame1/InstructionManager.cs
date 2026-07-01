using DG.Tweening;
using UnityEngine;
using INART.SlothyBakery.Core;
using INART.Framework.Utils;

/// <summary>
/// Shows / hides the instructions panel with a DOTween alpha fade.
/// Migrated from manual timer coroutine to DOFade.
/// </summary>
public class InstructionManager : Singleton<InstructionManager>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    protected override bool PersistAcrossScenes => false;

    private Tween _tween;

    protected override void OnAwake() => HideImmediate();

    // ── Public API ─────────────────────────────────────────────────────────

    public void OpenInstructions()
    {
        _tween?.Kill();
        _tween = _canvasGroup.FadeIn(fadeDuration, enableInteraction: true);
    }

    public void CloseInstructions()
    {
        _tween?.Kill();
        _tween = _canvasGroup.FadeOut(fadeDuration, disableInteraction: true);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void HideImmediate() => _canvasGroup.SetAlpha(0f, interactable: false);
}
