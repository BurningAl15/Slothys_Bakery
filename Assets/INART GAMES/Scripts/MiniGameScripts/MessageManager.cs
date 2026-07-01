using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using INART.SlothyBakery.Core;
using INART.Framework.Utils;

/// <summary>
/// Shows a feedback sprite (correct / wrong) with a scale-pop animation.
/// Migrated from manual coroutine Lerp to DOTween Sequence.
/// </summary>
public class MessageManager : Singleton<MessageManager>
{
    [SerializeField] private RectTransform message;
    [SerializeField] private Sprite badMessage, goodMessage;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float holdDuration = 0.5f;

    private Image _messageImage;
    private Sequence _sequence;

    protected override bool PersistAcrossScenes => false;

    protected override void OnAwake()
    {
        _messageImage = message.GetComponent<Image>();
        message.localScale = Vector3.zero;
    }

    /// <summary>
    /// Shows the feedback popup. Safe to call while a previous animation is running
    /// — kills the old sequence first and snaps back to zero.
    /// </summary>
    public void Animate_Message(bool isCorrect)
    {
        // Kill any running animation and reset immediately
        _sequence?.Kill();
        message.localScale = Vector3.zero;

        _messageImage.sprite = isCorrect ? goodMessage : badMessage;
        _messageImage.SetNativeSize();

        _sequence = DOTween.Sequence()
            // Grow to maxScale
            .Append(message.DOScale(maxScale, 0.1f).SetEase(Ease.OutBack))
            // Hold
            .AppendInterval(holdDuration)
            // Shrink back to zero
            .Append(message.DOScale(0f, 0.12f).SetEase(Ease.InBack))
            .OnComplete(() => _sequence = null);
    }
}
