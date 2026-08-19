using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using INART.Framework.Modular;

namespace INART.SlothyBakery.Base
{
    public class MiniGameParent : MonoBehaviour
    {
        /// <summary>
        /// Set here everything you want to initialize in the start method
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Set everything that will be animated in the beginning
        /// </summary>
        /// <param name="_delay"></param>
        /// <returns></returns>
        public virtual IEnumerator TurnOnGame(float _delay)
        {
            yield return null;
        }

        /// <summary>
        /// Set everything that will change every time that a round finishes
        /// </summary>
        /// <param name="_delay"></param>
        /// <returns></returns>
        protected virtual IEnumerator Change(float _delay)
        {
            yield return null;
        }

        // ── Modular flow integration ───────────────────────────────────────────

        /// <summary>
        /// Call this at the end of each successful round instead of manually
        /// writing to ProgressionCanvas, SaveLoadState, AnalyticsManager and
        /// DailyChallengeManager.
        ///
        /// GameFlowManager handles all of that and fires the events that
        /// ModularProgressionCanvas listens to.
        ///
        /// Safe to call even if GameFlowManager is not present in the scene —
        /// falls back gracefully so legacy scenes are unaffected.
        /// </summary>
        protected void ReportRoundComplete()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ReportRoundCompleted();
            }
            else
            {
                // GameFlowManager not present — this is a legacy scene running the
                // old system. Do nothing; the existing minigame code handles progression.
                Debug.LogWarning(
                    "[MiniGameParent] ReportRoundComplete called but GameFlowManager " +
                    "is not in the scene. No modular progression will be tracked.",
                    this);
            }
        }
    }
}