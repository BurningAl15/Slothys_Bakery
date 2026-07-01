using System;
using System.Collections.Generic;
using INART.Framework.Core.Services;

namespace INART.Framework.Core
{
    /// <summary>
    /// Lightweight service container. Register services once (usually in GameManager.Awake)
    /// and resolve them from anywhere without creating hard dependencies.
    ///
    /// Usage:
    ///   // Register
    ///   ServiceLocator.Register&lt;IAudioService&gt;(soundManagerInstance);
    ///
    ///   // Resolve
    ///   var audio = ServiceLocator.Get&lt;IAudioService&gt;();
    ///
    ///   // Safe resolve (no error log)
    ///   if (ServiceLocator.Has&lt;IAudioService&gt;()) { ... }
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IGameService> _services =
            new Dictionary<Type, IGameService>();

        private static bool _isInitialized = false;

        // ── Setup ──────────────────────────────────────────────────────────────

        /// <summary>Clears any previous state. Called by GameManager on Awake.</summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                GameLogger.Warning("ServiceLocator already initialized.", LogCategory.System);
                return;
            }

            _services.Clear();
            _isInitialized = true;
            GameLogger.Log("ServiceLocator initialized.", LogCategory.System);
        }

        // ── Registration ───────────────────────────────────────────────────────

        /// <summary>
        /// Registers <paramref name="service"/> under interface type <typeparamref name="T"/>.
        /// Overwrites silently if the type was already registered (logs a warning).
        /// </summary>
        public static void Register<T>(T service) where T : IGameService
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                GameLogger.Warning($"Service '{type.Name}' already registered. Overwriting.", LogCategory.System);
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
                GameLogger.Log($"Service registered: {type.Name}", LogCategory.System);
            }
        }

        /// <summary>Removes a previously registered service.</summary>
        public static void Unregister<T>() where T : IGameService
        {
            var type = typeof(T);
            if (_services.Remove(type))
                GameLogger.Log($"Service unregistered: {type.Name}", LogCategory.System);
            else
                GameLogger.Warning($"Tried to unregister '{type.Name}' but it was not registered.", LogCategory.System);
        }

        // ── Resolution ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the service registered under <typeparamref name="T"/>.
        /// Logs an error and returns default if not found.
        /// </summary>
        public static T Get<T>() where T : IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
                return (T)service;

            GameLogger.Error($"Service '{type.Name}' not found. Did you forget to register it in GameManager?",
                LogCategory.System);
            return default;
        }

        /// <summary>Returns true if a service of type <typeparamref name="T"/> is registered.</summary>
        public static bool Has<T>() where T : IGameService
            => _services.ContainsKey(typeof(T));

        // ── Lifecycle ──────────────────────────────────────────────────────────

        /// <summary>
        /// Calls Shutdown() on every registered service and clears the container.
        /// Called by GameManager.OnApplicationQuit.
        /// </summary>
        public static void ShutdownAll()
        {
            GameLogger.Log("Shutting down all services...", LogCategory.System);

            foreach (var service in _services.Values)
            {
                try
                {
                    service.Shutdown();
                }
                catch (Exception ex)
                {
                    GameLogger.Exception(ex, LogCategory.System);
                }
            }

            _services.Clear();
            _isInitialized = false;
            GameLogger.Log("ServiceLocator cleared.", LogCategory.System);
        }
    }
}
