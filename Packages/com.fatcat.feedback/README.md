# FatCat Feedback Kit (`com.fatcat.feedback`)

Game feel for the studio's learning games: every answer gets feedback, screen elements enter and leave
staggered, and each minigame ends with a celebration. Everything is animated with DOTween.

Made in Slothy's Café (2026-11-21). The full guide, with the builders and the order to run them, is in
`SlothysCafe/Documentation/SLOTHY_LIBRARIES.md`.

## Requirements
- DOTween (Demigiant) in the project, in any folder. The kit uses only DOTween's core API (`FeedbackTweens`),
  so the DOTween modules need no asmdef.
- uGUI and TextMesh Pro (`com.unity.ugui` 2.x).

## Install
- Package Manager → **Add package from tarball** → `com.fatcat.feedback-<version>.tgz`
  (`CurrentlyWorkin/SharedPackages/dist`), or copy the folder into `Packages/`.

## Parts
| Part | What it does |
|---|---|
| `Juice` | Static tweens: `PopIn`/`PopOut`, `Hop`, `Shake` (Transform or RectTransform), `Pulse`, `Squash`, `ArcTo`, `SlideIn`/`SlideOut` (and `…Local`). |
| `UiChoreography` | A scene component that lists the elements and where each one enters from (Top, Bottom, Left, Right, Pop) and with what stagger. `EnterCurrent()` / `ExitCurrent()` / `HideNow()`. |
| `FeedbackStamp` | `Right(message, worldPoint)` / `Wrong(…)` show a stamp, sparkles, a line with the reason and a streak every 3 right answers; they return how long to wait. `Sparkle`, `ResetStreak`. |
| `FeedbackCelebration` | `Show(stars, title, subtitle, continueText)`: end card with up to 3 stars. Continues on its own after 6 s. |
| `CheckBellButton` | Goes on an existing Button: a counter bell, grey until `ReadyWhen(...)` is true, then it swings; a tap rings and runs the button's original actions. |

## What the game provides
- The `FeedbackStamp` prefab at `Resources/FatCatFeedback/FeedbackStamp` (the art is the game's). Café builds
  it with `CafeFeedbackBuilder`.
- Hooks, set once at start-up (Café: `CafeFeedbackHooks`):
  - `FeedbackStamp.StreakLabel`: the localized streak text.
  - `FeedbackStamp.Haptic`: vibration, respecting the game's setting.
  - `FeedbackCelebration.StarSound`: the sound for each star.
  - `CheckBellButton.Volume` and `CheckBellButton.Haptic`.
