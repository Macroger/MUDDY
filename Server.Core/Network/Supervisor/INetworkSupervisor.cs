// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline;
using Server.Core.Network.Model;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Server.Core.Network.Supervisor
{
    public interface INetworkSupervisor
    {
        /// <summary>
        /// Gets a value indicating whether the server is currently listening for incoming connection requests.
        /// </summary>
        public bool IsListeningForConnections { get; }

        /// <summary>
        /// Start accepting new client connections. 
        /// </summary>
        /// <returns></returns>
        bool StartListener();

        /// <summary>
        /// Stops the server from accepting new client connections.
        /// </summary>
        /// <returns>true if the server successfully stops accepting new clients; otherwise, false.</returns>
        bool StopListener();

        /// <summary>
        /// Closes the specified connection and releases any associated resources.
        /// </summary>
        /// <remarks>After calling this method, the connection is no longer available for communication.
        /// Any pending operations on the connection may be terminated.</remarks>
        /// <param name="connectionId">The identifier of the connection to close. Must be a valid, active connection.</param>
        /// <param name="reason">The reason for closing the connection, which determines how the closure is handled and reported.</param>
        void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason);

        /// <summary>
        /// Process a newly accepted client connection. This method is responsible for handling the initial setup
        /// and registration of the new client connection within the network supervisor.
        /// </summary>
        /// <param name="acceptedConnection"></param>
        void ProcessNewConnection(AcceptedConnection acceptedConnection);

        /// <summary>
        /// Sends a transport message to all connected clients. The message will be broadcasted to every client currently connected to the server.
        /// </summary>
        /// <param name="msg"></param>
        void BroadcastMessage(TransportEnvelope msg);

        /// <summary>
        /// Sends a transport message to a specific client identified by their connection ID.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        void SendToClient(ConnectionId client, TransportEnvelope msg);

        /// <summary>
        /// Sends a transport message to multiple clients identified by their connection IDs.
        /// </summary>
        /// <param name="clients">A collection of connection identifiers representing the clients to which the message will be sent. Cannot be
        /// null or contain null elements.</param>
        /// <param name="msg">The transport envelope containing the message to send to the specified clients. Cannot be null.</param>
        void SendToMultipleClients(IEnumerable<ConnectionId> clients, TransportEnvelope msg);

        /// <summary>
        /// Sets the command pipeline orchestrator to be used for processing commands.
        /// </summary>
        /// <param name="pipeline">The command pipeline orchestrator that defines the sequence of processing steps for commands. Cannot be
        /// null.</param>
        /// <returns>true if the pipeline was set successfully; otherwise, false.</returns>
        public bool SetCommandPipeline(CommandPipelineOrchestrator pipeline);
    }
}
