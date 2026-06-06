using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.Domain.Services.ChatService;

namespace Server.Core.CommandPipeline.CommandRegistration
{

    public class ChatCommandRegistrar : ICommandRegistrar
    {
        private readonly IChatService _chatService;

        public ChatCommandRegistrar(IChatService chatService)
        {
            _chatService = chatService;
        }

        public void Register(ICommandRouter router)
        {
            var handler = new ChatCommandHandler(_chatService);
            router.RegisterHandler("say", handler);
        }
    }
}
