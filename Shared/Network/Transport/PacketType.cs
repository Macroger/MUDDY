// =============================================================================
/// @file       Shared/Network/Transport/PacketType.cs
/// @namespace  Shared.Network.Types
/// @brief      Defines the enumeration of packet types used in network communication.
// =============================================================================

namespace Shared.Network.Types
{
    public enum PacketType
    {
        Authentication,         // A message related to authentication, such as login or token validation.
        BinaryTransfer,         // A binary data transfer message, used for sending files or large data blobs.
        Command,                // A command message, typically used to by the client to request the server perform an action.
        Error,                  // An error message, indicating that an error occurred during processing of a previous message.
        PlayerStateEvent,       // A message updating the client on the state of a player, such as position, health, or inventory changes.
        WorldStateEvent,        // A message updating the client on the state of the game world, such as environmental changes or global events.
        QuestStateEvent,        // A message updating the client on the state of a quest, such as progress or completion.
        EventNotification,      // A message notifying the client of an event, such as a quest update or achievement unlocked.
        Ping,                   // A ping message, used to check the latency or connectivity between client and server.               
        Response,               // A response message, typically sent by the server to a command or request from the client.        
        System                  // A system message, used for internal communication between server components or for administrative purposes.
    }
}
