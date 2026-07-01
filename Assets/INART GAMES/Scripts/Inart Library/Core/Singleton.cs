using UnityEngine;
using INART.Framework.Core;

namespace INART.SlothyBakery.Core
{
    /// <summary>
    /// Generic thread-safe Singleton base for MonoBehaviours.
    ///
    /// Usage:
    ///   public class MyManager : Singleton&lt;MyManager&gt;
    ///   {
    ///       protected override bool PersistAcrossScenes => true;
    ///       protected override void OnAwake() { /* init */ }
    ///   }
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

        /// <summary>
        /// Returns the singleton instance. Returns null if the application is quitting
        /// or if no instance exists in the scene.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    GameLogger.Warning(
                        $"[Singleton] Instance of {typeof(T).Name} requested while application is quitting. Returning null.",
                        LogCategory.System);
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                        _instance = FindFirstObjectByType<T>();

                    return _instance;
                }
            }
        }

        /// <summary>Whether this singleton should survive scene loads. Default: false.</summary>
        protected virtual bool PersistAcrossScenes => false;

        protected virtual void Awake()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;

                    if (PersistAcrossScenes)
                        DontDestroyOnLoad(gameObject);

                    OnAwake();
                }
                else if (_instance != this)
                {
                    GameLogger.Warning(
                        $"[Singleton] Duplicate {typeof(T).Name} detected on '{gameObject.name}'. Destroying duplicate.",
                        LogCategory.System);
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>Called once on the first (valid) Awake. Override instead of Awake().</summary>
        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
            _instance = null;
        }
    }
}
