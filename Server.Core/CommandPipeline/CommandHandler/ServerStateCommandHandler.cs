// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Infrastructure.Lifecycle;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles the "serverstate" command, which allows a player to query or transition the server
    /// between ACTIVE, MAINTENANCE, and SHUTTING_DOWN states.
    /// <para>
    /// Usage: <c>serverstate</c> (query current state) or <c>serverstate &lt;active|maintenance|shutdown&gt;</c> (change state)
    /// </para>
    /// </summary>
    public sealed class ServerStateCommandHandler : ICommandHandler
    {
        private readonly IServerLifecycle _lifecycle;

        /// <summary>
        /// Initializes a new instance of <see cref="ServerStateCommandHandler"/>.
        /// </summary>
        /// <param name="lifecycle">The server lifecycle used to drive state transitions.</param>
        public ServerStateCommandHandler(IServerLifecycle lifecycle)
        {
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            string? targetArg = context.Command.Arguments.FirstOrDefault();

            // If no argument provided, return the current state
            if (string.IsNullOrWhiteSpace(targetArg))
            {
                string currentStateName = _lifecycle.CurrentState switch
                {
                    ServerStateEnum.LOADING => "LOADING",
                    ServerStateEnum.ACTIVE => "ACTIVE",
                    ServerStateEnum.MAINTENANCE => "MAINTENANCE",
                    ServerStateEnum.SHUTTING_DOWN => "SHUTTING_DOWN",
                    _ => "UNKNOWN"
                };

                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Current server state: {currentStateName}"
                });
            }

            ServerStateEnum? target = targetArg.ToLowerInvariant() switch
            {
                "active" => ServerStateEnum.ACTIVE,
                "maintenance" => ServerStateEnum.MAINTENANCE,
                "shutdown" => ServerStateEnum.SHUTTING_DOWN,
                _ => null
            };

            if (target is null)
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = $"Unknown server state '{targetArg}'. Valid states: active, maintenance, shutdown."
                });
            }

            bool ok = _lifecycle.SetState(target.Value);

            if (!ok)
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = $"Cannot transition to '{targetArg}' from the current server state."
                });
            }

            return Task.FromResult(new CommandResult
            {
                Success = true,
                Message = $"Server state changed to {target.Value}."
            });
        }
    }
}
