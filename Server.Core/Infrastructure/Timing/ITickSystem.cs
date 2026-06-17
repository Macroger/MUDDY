namespace Server.Core.Infrastructure.Timing
{
    public interface ITickSystem
    {
        /// <summary>
        /// Starts the tick system
        /// </summary>
        void Start();
        /// <summary>
        /// Stops the tick system
        /// </summary>
        void Stop();
    }
}
