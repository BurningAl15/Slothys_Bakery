namespace INART.Framework.Core.Services
{
    /// <summary>
    /// Contract for the save/load service.
    ///
    /// Usage:
    ///   var save = ServiceLocator.Get&lt;ISaveService&gt;();
    ///   save.Save("playerData", data);
    ///   var data = save.Load&lt;PlayerData&gt;("playerData");
    /// </summary>
    public interface ISaveService : IGameService
    {
        /// <summary>Serializes <paramref name="data"/> and persists it under <paramref name="key"/>.</summary>
        void Save<T>(string key, T data);

        /// <summary>Loads and deserializes data stored under <paramref name="key"/>.
        /// Returns <paramref name="defaultValue"/> if the key does not exist.</summary>
        T Load<T>(string key, T defaultValue = default);

        /// <summary>Returns true if a value is stored under <paramref name="key"/>.</summary>
        bool HasKey(string key);

        /// <summary>Removes a single persisted key.</summary>
        void DeleteKey(string key);

        /// <summary>Wipes ALL persisted data for this game. Use with caution.</summary>
        void DeleteAll();
    }
}
