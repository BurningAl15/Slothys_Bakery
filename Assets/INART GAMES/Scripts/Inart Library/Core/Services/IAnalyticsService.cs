using System.Collections.Generic;

namespace INART.Framework.Core.Services
{
    /// <summary>
    /// Contract for the analytics service (Unity Gaming Services or any other provider).
    ///
    /// Usage:
    ///   var analytics = ServiceLocator.Get&lt;IAnalyticsService&gt;();
    ///   analytics.TrackEvent("round_completed", new Dictionary&lt;string, object&gt; { {"round", 3} });
    /// </summary>
    public interface IAnalyticsService : IGameService
    {
        // ── Generic events ────────────────────────────────────────────────────
        void TrackEvent(string eventName);
        void TrackEvent(string eventName, Dictionary<string, object> parameters);

        // ── Gameplay shortcuts (implement what your game tracks) ──────────────
        void TrackLevelStarted(string levelName);
        void TrackLevelCompleted(string levelName, bool success);
        void TrackAnswerCorrect(int roundIndex);
        void TrackAnswerWrong(int roundIndex);
        void TrackGameCompleted();
    }
}
