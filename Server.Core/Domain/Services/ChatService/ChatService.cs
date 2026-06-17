// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Server.Core.Infrastructure.Events;
using Server.Core.Infrastructure.Identity.MessageId;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Identity;

namespace Server.Core.Domain.Services.ChatService
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
                var errorResponse = new CommandResult
                {
                    Success = false,
                    Message = "Message cannot be empty"
                };

                return errorResponse;
            }

            // Ensure player is not muted.
            if (sender.ActiveConditions.Contains(PlayerCondition.Muted))
            {
                var errorResponse = new CommandResult
                {
                    Success = false,
                    Message = "You are muted and cannot send messages."
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

            _eventBus.Publish(EventMessageType.Chat,
                new ChatEvents.PlayerSaidEvent
                (
                    sender.PlayerName,
                    sender.CurrentLocation,
                    broadcastMsg,
                    room.PlayersPresent
                )
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
