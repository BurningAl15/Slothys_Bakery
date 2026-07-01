using DG.Tweening;
using UnityEngine;
using INART.SlothyBakery.Core;
using INART.Framework.Utils;

/// <summary>
/// Slides the hint panel in/out using DOTween.
/// Migrated from manual Lerp coroutine to DOLocalMove.
/// Uses the same AnimationCurve as before so the easing stays identical.
/// </summary>
public class HintManager : Singleton<HintManager>
{
    [SerializeField] private Vector2 initialPoint;
    [SerializeField] private Vector2 endPoint;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float duration = 0.4f;

    [SerializeField] private RectTransform _messageTransform;

    protected override bool PersistAcrossScenes => false;

    private Tween _tween;
    private bool _isOn;

    protected override void OnAwake()
    {
        _messageTransform.localPosition = initialPoint;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void TurnOn()
    {
        if (_isOn) return;
        _isOn = true;
        Slide(endPoint);
    }

    public void TurnOff()
    {
        if (!_isOn) return;
        _isOn = false;
        Slide(initialPoint);
    }

    // ── Internal ───────────────────────────────────────────────────────────

    private void Slide(Vector2 target)
    {
        _tween?.Kill();
        // MoveToLocal with AnimationCurve preserves original easing feel
        _tween = _messageTransform.MoveToLocal(target, duration, animCurve);
    }
}
