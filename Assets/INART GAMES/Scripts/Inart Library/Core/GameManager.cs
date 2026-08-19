using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using INART.Framework.Core;
using INART.Framework.Core.Services;

namespace INART.SlothyBakery.Core
{
    using CanvasAnimator;
    using SceneTransition;
    using Base;
    using SoundManager;

    /// <summary>
    /// Scene-level game manager. Handles the start sequence for each minigame scene.
    ///
    /// On Awake it:
    ///   1. Initializes the ServiceLocator (if not already done by a Loader scene).
    ///   2. Registers core services (SoundManager, etc.).
    ///   3. Applies settings loaded by SettingsManager.
    ///
    /// On Start it runs the cinematic intro coroutine, then signals GameStates to Play.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Minigame")]
        [SerializeField] private MiniGameParent miniGame;

        [Header("Intro Sequence")]
        [SerializeField] private float canvasAnimationDuration = 0.6f;
        [FormerlySerializedAs("testing")]
        [SerializeField] private bool isTesting;
        [FormerlySerializedAs("instructionsOn")]
        [SerializeField] private bool runAnimation = true;
        [SerializeField] private bool turnOnInstructions = true;
        [SerializeField] private bool turnOnQuestionButton = true;

        [Header("Logging")]
        [SerializeField] private LogLevel editorLogLevel = LogLevel.Info;

        // ── Runtime ────────────────────────────────────────────────────────────
        private Coroutine _currentCoroutine;

        // GameManager lives only in its scene (not DontDestroyOnLoad)
        protected override bool PersistAcrossScenes => false;

        // ── Singleton lifecycle ────────────────────────────────────────────────
        protected override void OnAwake()
        {
            GameLogger.SetLogLevel(editorLogLevel);
            GameLogger.Log("GameManager Awake — initializing services.", LogCategory.System);

            // Initialize ServiceLocator (safe to call even if a Loader scene already did it)
            ServiceLocator.Initialize();
            RegisterServices();
        }

        // ── Service registration ───────────────────────────────────────────────

        /// <summary>
        /// Register services that are available in this scene.
        /// Add more here as the framework grows (Analytics, Save, etc.).
        /// </summary>
        private void RegisterServices()
        {
            // SoundManager persists across scenes — only register if not already present
            if (!ServiceLocator.Has<IAudioService>() && SoundManager.Instance != null)
            {
                ServiceLocator.Register<IAudioService>(SoundManager.Instance);
                GameLogger.Log("SoundManager registered as IAudioService.", LogCategory.System);
            }
        }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            GameStates.Instance.Set_GameState_Load();
            miniGame.Init();

#if UNITY_ANDROID || UNITY_IOS
            CanvasAnimator.Instance.Test(false);
#else
            CanvasAnimator.Instance.Test(isTesting);
#endif
            _currentCoroutine ??= StartCoroutine(StartGame(1f));
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            ServiceLocator.ShutdownAll();
        }

        // ── Game flow coroutines ───────────────────────────────────────────────

        private IEnumerator StartGame(float delayDuration)
        {
            yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_IN());

            yield return StartCoroutine(
                CanvasAnimator.Instance.CanvasAnimation(canvasAnimationDuration, PhraseUtils.PhraseType.INTRO));

            if (runAnimation)
            {
                bool isFirstLevel = SceneUtils.IsCurrentSceneFirstLevel();
                yield return StartCoroutine(
                    CanvasAnimator.Instance.InstructionsAnimation(true, turnOnInstructions,
                        isFirstLevel && turnOnQuestionButton));
            }

            yield return new WaitForSeconds(delayDuration);
            yield return StartCoroutine(miniGame.TurnOnGame(0.1f));

            // Play success SFX via service (falls back to direct call if service not ready)
            var audio = ServiceLocator.Has<IAudioService>()
                ? ServiceLocator.Get<IAudioService>()
                : null;
            audio?.PlaySFX("success");

            if (SceneUtils.IsCurrentSceneFirstLevel())
                CanvasAnimator.Instance.TurnButton(true);

            if (SceneUtils.IsCinematicScene())
                yield return StartCoroutine(CanvasAnimator.Instance.EnableTurnButton(true));

            GameStates.Instance.Set_GameState_Play();
            GameLogger.Log("Game started — state set to Play.", LogCategory.Gameplay);
            _currentCoroutine = null;
        }

        private IEnumerator ToNextScene(float delay)
        {
            yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
            _currentCoroutine = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void CallNextScene()
        {
            _currentCoroutine ??= StartCoroutine(ToNextScene(3f));
        }

        /// <summary>Shortcut to resolve any registered service.</summary>
        public T GetService<T>() where T : IGameService => ServiceLocator.Get<T>();

#if UNITY_EDITOR
        [ContextMenu("Log Service Status")]
        private void LogServiceStatus()
        {
            Debug.Log($"IAudioService registered: {ServiceLocator.Has<IAudioService>()}");
            Debug.Log($"ISaveService   registered: {ServiceLocator.Has<ISaveService>()}");
            Debug.Log($"IAnalyticsService registered: {ServiceLocator.Has<IAnalyticsService>()}");
        }
#endif
    }
}
