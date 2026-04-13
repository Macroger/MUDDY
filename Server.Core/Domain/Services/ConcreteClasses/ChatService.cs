using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Server.Core.Domain.World;
using Server.Core.Infrastructure.Identity.MessageId;
using Shared.EventBus;
using System;
using System.Collections.Generic;
using System.Text;
using static Shared.EventBus.DomainEvents.ChatEvents;

namespace Server.Core.Domain.Services.ConcreteClasses
{
    public class ChatService
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

            // Build message
            string broadcastMsg = $"{sender.PlayerName} says: {message}";
            
            // Publish with player list
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Chat,
                new EventReason(
                "PlayerSaid",
                new PlayerSaidEvent(
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
                Message = $"You said: {message}"
            };
        }
    }
}
