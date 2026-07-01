namespace INART.Framework.Core.Services
{
    /// <summary>
    /// Base contract for every service registered in the ServiceLocator.
    /// All services must implement Initialize() and Shutdown().
    /// </summary>
    public interface IGameService
    {
        void Initialize();
        void Shutdown();
    }
}
