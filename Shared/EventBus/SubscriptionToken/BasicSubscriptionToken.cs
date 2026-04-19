namespace Shared.EventBus.SubscriptionToken
{
    internal sealed class BasicSubscriptionToken : ISubscriptionToken
    {
        private readonly Action _unsubscribe;
        private bool _isDisposed;


        public BasicSubscriptionToken(Action unsubscribe)
        {
            // Assign the unsubscribe action; if the action is invalid throw an exception
            _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            // Check if already disposed
            if (_isDisposed) return;

            // Perform the unsubscribe action
            _unsubscribe();

            // Register this token as disposed.
            _isDisposed = true;

        }
    }
}
