using Client.Core.CommandPipeline;
using Shared.Protocol.Transport;

namespace Tests.Client
{
    [TestClass]
    public class ChatMessageHandlerTests
    {
        [TestMethod]
        public async Task ChatMessageHandler_RaisesEvent()
        {
            string? received = null;
            var handler = new ChatMessageHandler();
            ChatMessageHandler.OnChatMessageReceived += msg => received = msg;
            var envelope = new TransportEnvelope(
                messageId: new Shared.Identity.MessageId(1),
                messageType: TransportMessageType.Chat,
                flags: 0,
                payload: System.Text.Encoding.UTF8.GetBytes("hello world"),
                connectionId: new Shared.Identity.ConnectionId("test"),
                sessionId: null
            );
            await handler.HandleAsync(envelope);
            Assert.AreEqual("hello world", received);
        }
    }

    [TestClass]
    public class BinaryTransferHandlerTests
    {
        [TestMethod]
        public async Task BinaryTransferHandler_RaisesEventWithPayload()
        {
            byte[]? received = null;
            var handler = new BinaryTransferHandler();
            BinaryTransferHandler.OnImageReceived += img => received = img;
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            var envelope = new TransportEnvelope(
                messageId: new Shared.Identity.MessageId(2),
                messageType: TransportMessageType.BinaryTransfer,
                flags: 0,
                payload: payload,
                connectionId: new Shared.Identity.ConnectionId("test"),
                sessionId: null
            );
            await handler.HandleAsync(envelope);
            Assert.IsNotNull(received);
            CollectionAssert.AreEqual(payload, received);
        }
    }
}
