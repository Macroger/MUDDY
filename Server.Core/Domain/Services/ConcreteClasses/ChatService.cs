using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.Services.Interfaces;
using Server.Core.Domain.World;
using Server.Core.Infrastructure.Identity.MessageId;
using Shared.EventBus;
using Shared.EventBus.DomainEvents;
using Shared.Identity;

namespace Server.Core.Domain.Services.ConcreteClasses
{
    public class ChatService : IChatService
    {
        private readonly IEventBus _eventBus;

        public ChatService(IEventBus eventBus, IMessageIdGenerator messageIdGenerator)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<CommandResult> BroadcastMessageAsync(
            PlayerState sender,
            WorldState world,
            string message)
         {
            // Validate
            if (string.IsNullOrEmpty(message))
            {
               var errorResponse =  new CommandResult
                {
                    Success = false,
                    Message = "Message cannot be empty"
                };

                return errorResponse;
            }
            
            // Get current room
            var room = world.Rooms[sender.CurrentLocation];

            // Get list of other connected players in the room
            HashSet<ConnectionId> otherPlayersInRoom = room.PlayersPresent
                .Where(pid => !pid.Equals(sender.ConnId))
                .ToHashSet();

            // Build message
            string broadcastMsg = $"{sender.PlayerName} says: {message}";
            
            // Publish with player list
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Chat,
                new EventReason(
                "PlayerSaid",
                new ChatEvents.PlayerSaidEvent(
                    sender.PlayerName,
                    sender.CurrentLocation,
                    broadcastMsg,
                    room.PlayersPresent 
                ))
            );

            // Return success
            return new CommandResult
            {
                Success = true,
                Message = $"You said: {message}",
                AdditionalRecipients = otherPlayersInRoom
            };
        }
    }
}
