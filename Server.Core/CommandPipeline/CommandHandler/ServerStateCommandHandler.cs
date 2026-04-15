using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Infrastructure.Lifecycle;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles the "serverstate" command, which allows a player to transition the server
    /// between ACTIVE, MAINTENANCE, and SHUTTING_DOWN states.
    /// <para>
    /// Usage: <c>serverstate &lt;active|maintenance|shutdown&gt;</c>
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

            if (string.IsNullOrWhiteSpace(targetArg))
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = "Usage: serverstate <active|maintenance|shutdown>"
                });
            }

            ServerStateEnum? target = targetArg.ToLowerInvariant() switch
            {
                "active"      => ServerStateEnum.ACTIVE,
                "maintenance" => ServerStateEnum.MAINTENANCE,
                "shutdown"    => ServerStateEnum.SHUTTING_DOWN,
                _             => null
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
