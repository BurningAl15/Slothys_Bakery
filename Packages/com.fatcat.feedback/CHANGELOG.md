# Changelog

## 0.3.0 — 2026-11-22
- `CheckBellButton` rings once per answer: after a ring it stays off until `ReadyWhen` turns false (the answer
  was taken away or a new round began), or for 1.5 s without a condition.
- `FeedbackStamp.Sparkle(RectTransform)`: sparkles on UI elements of any canvas (an overlay's positions are
  not world points; the progress bar's sparkles were drawn off screen).
- The peeking character rises without overshoot (its cut edge never shows).

## 0.2.0 — 2026-11-21
- `IFeedbackFace`: an optional face on the peeking character. It is told whether the answer was right; with a
  face, the character also peeks (a little lower) on wrong answers, looking worried.

## 0.1.0 — 2026-11-21
- First version, extracted from Slothy's Café: Juice, UiChoreography, FeedbackStamp, FeedbackCelebration,
  CheckBellButton.
- DOTween UI tweens re-implemented on its core API (FeedbackTweens), so the package compiles in any project with
  DOTween without the modules asmdef.
