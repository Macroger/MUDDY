namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// An interface used to mark items as startable, meaning they have a Start method that can be called to start their operation.
    /// </summary>
    public interface IStartable
    {
        void Start();
    }
}
