using Client.Core.CommandPipeline;
using Shared.Protocol.Transport;

namespace Tests.Client
{
    [TestClass]
    public class ClientCommandPipelineOrchestratorTests
    {
        [TestMethod]
        public void RegisterHandler_And_ProcessMessage_CallsHandler()
        {
            var orchestrator = new ClientCommandPipelineOrchestrator();
            var handlerMock = new Moq.Mock<IClientCommandHandler>();
            orchestrator.RegisterHandler("Chat", handlerMock.Object);
            var envelope = new TransportEnvelope(
                messageId: new Shared.Identity.MessageId(1),
                messageType: TransportMessageType.Chat,
                flags: 0,
                payload: System.Text.Encoding.UTF8.GetBytes("hello"),
                connectionId: new Shared.Identity.ConnectionId("test"),
                sessionId: null
            );
            orchestrator.Start();
            orchestrator.ProcessMessage(envelope);
            Task.Delay(100).Wait();
            handlerMock.Verify(h => h.HandleAsync(Moq.It.IsAny<TransportEnvelope>()), Moq.Times.Once);
            orchestrator.StopAsync().Wait();
        }
    }
}
