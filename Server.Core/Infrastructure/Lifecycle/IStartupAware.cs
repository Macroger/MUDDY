namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// An interface to mark items as startup aware, meaning they have an OnStartup method
    /// that can be called to perform any necessary initialization or setup when the application starts up.
    /// </summary>
    public interface IStartupAware
    {
        void OnStartup();
    }
}
