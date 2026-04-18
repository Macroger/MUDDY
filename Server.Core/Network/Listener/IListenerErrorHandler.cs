namespace Server.Core.Network.Listener
{
    public interface IListenerErrorHandler
    {
        void OnListenerError(Exception exception);
    }

}
