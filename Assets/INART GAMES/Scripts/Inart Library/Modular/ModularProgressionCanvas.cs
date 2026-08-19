using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using INART.Framework.Core;

namespace INART.Framework.Modular
{
    using SoundManager = INART.SlothyBakery.SoundManager.SoundManager;

    /// <summary>
    /// Generates progression bars, lock icons and VFX procedurally from GameFlowManager data.
    ///
    /// LAYER ORDER (Canvas children, top to bottom = back to front):
    ///   BarsContainer   — bar fill images (background layer)
    ///   LocksContainer  — lock/unlock icons (always on top of bars)
    ///   VFXContainer    — particle effects (always on top of locks)
    ///
    /// This mirrors the legacy pattern where all bars were one group,
    /// all locks were another group above them, etc.
    ///
    /// SETUP:
    ///   Assign the three RectTransform containers in the Inspector.
    ///   Assign lockIconPrefab, vfxPrefab, and unlockSprite.
    ///   barCellPrefab comes from GameFlowConfigSO.
    /// </summary>
    public class ModularProgressionCanvas : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Containers (assign in Inspector)")]
        [Tooltip("RectTransform holding the bar fill images. Add HorizontalLayoutGroup here.")]
        [SerializeField] private RectTransform barsContainer;

        [Tooltip("RectTransform holding the lock icons + their VFX children.\n" +
                 "Must come AFTER barsContainer in the Hierarchy so it renders on top.")]
        [SerializeField] private RectTransform locksContainer;

        [Header("Prefabs")]
        [Tooltip("Prefab for each lock icon.\n" +
                 "Root: Image component (the lock sprite).\n" +
                 "Child: ParticleSystem (completion VFX, optional).")]
        [SerializeField] private GameObject lockIconPrefab;

        [Header("Sprites")]
        [Tooltip("Sprite shown on the lock icon while locked.")]
        [SerializeField] private Sprite lockedSprite;

        [Tooltip("Sprite swapped onto the lock icon when a round is unlocked.")]
        [SerializeField] private Sprite unlockSprite;

        [Tooltip("Played when ALL rounds in the minigame are done.")]
        [SerializeField] private ParticleSystem partyParticles;

        [Header("Animation Timings")]
        [SerializeField] private float fillDuration   = 0.5f;
        [SerializeField] private float unlockDuration = 0.35f;

        [Header("Testing")]
        [Tooltip("Round count used when the scene is not registered in the FlowConfig.\n" +
                 "Set > 0 to test in DEV_ModularMinigame. Leave 0 in production.")]
        [SerializeField] private int debugRoundCount = 0;

        // ── Runtime ────────────────────────────────────────────────────────────

        private readonly List<ProgressionBarCell> _bars  = new List<ProgressionBarCell>();
        private readonly List<Image>              _locks = new List<Image>();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (!ValidateSetup()) return;
            BuildLayers();
            GameFlowManager.OnRoundCompleted += HandleRoundCompleted;
        }

        private void OnDestroy()
        {
            GameFlowManager.OnRoundCompleted -= HandleRoundCompleted;
        }

        // ── Setup ──────────────────────────────────────────────────────────────

        private bool ValidateSetup()
        {
            if (GameFlowManager.Instance == null)
            {
                GameLogger.Warning(
                    "ModularProgressionCanvas: GameFlowManager not found.",
                    LogCategory.UI);
                return false;
            }

            if (barsContainer == null || locksContainer == null)
            {
                GameLogger.Error(
                    "ModularProgressionCanvas: barsContainer or locksContainer not assigned.",
                    LogCategory.UI);
                return false;
            }

            var config = GameFlowManager.Instance.Config;
            if (config == null || config.barCellPrefab == null)
            {
                GameLogger.Error(
                    "ModularProgressionCanvas: GameFlowConfigSO.barCellPrefab not assigned.",
                    LogCategory.UI);
                return false;
            }

            return true;
        }

        private void BuildLayers()
        {
            var def    = GameFlowManager.Instance.CurrentMinigameDef;
            var config = GameFlowManager.Instance.Config;

            int count = (def != null)            ? def.roundCount    :
                        (debugRoundCount > 0)    ? debugRoundCount   : 0;

            if (count == 0)
            {
                GameLogger.Warning(
                    "ModularProgressionCanvas: roundCount is 0. " +
                    "Register the scene in FlowConfig or set Debug Round Count > 0.",
                    LogCategory.UI);
                return;
            }

            GameLogger.Log(
                $"ModularProgressionCanvas: building {count} cells " +
                $"(source: {(def != null ? def.sceneName : "debug override")}).",
                LogCategory.UI);

            for (int i = 0; i < count; i++)
            {
                // ── Bar layer ──────────────────────────────────────────────────
                var barGO  = Instantiate(config.barCellPrefab, barsContainer);
                var cell   = barGO.GetComponent<ProgressionBarCell>();
                if (cell == null)
                {
                    GameLogger.Error(
                        "ModularProgressionCanvas: barCellPrefab has no ProgressionBarCell.",
                        LogCategory.UI);
                    continue;
                }
                if (cell.barImage != null) cell.barImage.fillAmount = 0f;
                cell.FillValue = 0f;
                _bars.Add(cell);

                // ── Lock layer (with VFX as child) ────────────────────────────
                Image lockImg = null;
                if (lockIconPrefab != null)
                {
                    var lockGO = Instantiate(lockIconPrefab, locksContainer);
                    lockImg    = lockGO.GetComponent<Image>();
                    if (lockImg != null && lockedSprite != null)
                        lockImg.sprite = lockedSprite;
                }
                _locks.Add(lockImg);
            }
        }

        // ── Event handler ──────────────────────────────────────────────────────

        private void HandleRoundCompleted(int minigameIndex, int roundIndex, bool isLastRound)
        {
            if (minigameIndex != GameFlowManager.Instance.CurrentMinigameIndex) return;
            StartCoroutine(AnimateRoundComplete(roundIndex, isLastRound));
        }

        private IEnumerator AnimateRoundComplete(int roundIndex, bool isLastRound)
        {
            yield return StartCoroutine(FillBar(roundIndex));
            yield return StartCoroutine(UnlockCell(roundIndex));
            if (isLastRound) partyParticles?.Play();
        }

        // ── Animations ─────────────────────────────────────────────────────────

        public IEnumerator FillBar(int index)
        {
            if (!IsValid(index, nameof(FillBar))) yield break;

            var cell     = _bars[index];
            cell.FillValue = 1f;

            yield return cell.barImage
                .DOFillAmount(1f, fillDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            // VFX lives as a child of the lock icon
            var lockImg = _locks[index];
            if (lockImg != null)
            {
                var ps = lockImg.GetComponentInChildren<ParticleSystem>();
                ps?.Play();
            }
        }

        public IEnumerator UnlockCell(int index)
        {
            if (!IsValid(index, nameof(UnlockCell))) yield break;
            if (unlockSprite == null) yield break;

            var lockImg = _locks[index];
            if (lockImg == null) yield break;

            var rt = lockImg.GetComponent<RectTransform>();
            lockImg.sprite  = unlockSprite;
            rt.localScale   = Vector3.one * 1.5f;

            SoundManager.Instance?.Run_SFX(SoundManager.SoundType.Unlock);

            yield return rt
                .DOScale(Vector3.one, unlockDuration)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        public void ResetAllBars()
        {
            foreach (var cell in _bars)
            {
                cell.FillValue = 0f;
                if (cell.barImage != null)
                    cell.barImage.DOFillAmount(0f, 0.2f).SetEase(Ease.InQuad);
            }
            foreach (var lockImg in _locks)
                if (lockImg != null && lockedSprite != null)
                    lockImg.sprite = lockedSprite;
        }
        // ── Helpers ────────────────────────────────────────────────────────────

        private bool IsValid(int index, string caller)
        {
            if (index >= 0 && index < _bars.Count) return true;
            GameLogger.Error(
                $"ModularProgressionCanvas.{caller}: index {index} out of range.",
                LogCategory.UI);
            return false;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Simulate Round Complete")]
        private void DebugSimulateRound()
        {
            if (GameFlowManager.Instance == null) { Debug.LogWarning("No GameFlowManager."); return; }
            GameFlowManager.Instance.ReportRoundCompleted();
        }

        [ContextMenu("Debug: Reset All Bars")]
        private void DebugResetBars() => ResetAllBars();
#endif
    }
}
