using Client.Core.MessagePipeline;
using Client.Core.Network.Supervisor;
using Shared.EventBus;
using Shared.Logging;

namespace Client.Core.Application
{
    public sealed class ClientSystemInitializer : IDisposable
    {
        private readonly IEventBus _eventBus = null!;
        private readonly FileLogger _fileLogger = null!;
        private readonly ClientNetworkSupervisor _networkSupervisor = null!;
        private readonly MessagePipelineOrchestrator _messagePipeline = null!;

        private bool _disposed = false;

        public IEventBus EventBus => _eventBus;

        public ClientSystemInitializer()
        {
            // Initialize core services only (no connection, no GUI)
            _eventBus = new BasicEventBus();

            string logFilePath = LogPathHelper.CreateTimestampedLogPath("client_log");
            _fileLogger = new FileLogger(_eventBus, LogLevel.Debug, logFilePath);

            _networkSupervisor = new ClientNetworkSupervisor(_eventBus);
            _messagePipeline = new MessagePipelineOrchestrator(_eventBus);

            // Start message processing
            _messagePipeline.Start();
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Shutdown in reverse order of initialization
                _messagePipeline?.Dispose();
                _networkSupervisor?.Dispose();
                _fileLogger?.Dispose();
                _eventBus?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex.Message}");
            }

            _disposed = true;
        }
    }
}
