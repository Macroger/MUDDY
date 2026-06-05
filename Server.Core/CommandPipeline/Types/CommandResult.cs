// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Protocol.Types;

namespace Server.Core.CommandPipeline.Types
{
    public class CommandResult
    {
        /// <summary>
        /// Whether the command executed successfully.
        /// </summary>
        public required bool Success { get; init; }

        /// <summary>
        /// The response message to send to the player.
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// Optional additional connections that should also receive this message.
        /// Used for broadcast commands such as chat, emotes, or room notifications.
        /// </summary>
        public IReadOnlySet<ConnectionId>? AdditionalRecipients { get; init; }

        /// <summary>
        /// When non-null, the orchestrator sends this raw binary buffer instead of <see cref="Message"/>.
        /// The packet is transmitted with <see cref="MessageFlags.BinaryPayload"/> and
        /// <see cref="TransportMessageType.BinaryTransfer"/>, bypassing the JSON size cap.
        /// </summary>
        public byte[]? BinaryPayload { get; init; }
    }
}
