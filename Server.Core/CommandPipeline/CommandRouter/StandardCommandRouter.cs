// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.CommandHandler;

namespace Server.Core.CommandPipeline.CommandRouter
{
    public class StandardCommandRouter : ICommandRouter
    {

        private readonly Dictionary<string, ICommandHandler> _handlers;

        /// <summary>
        /// Initializes a new instance of the StandardCommandRouter.
        /// </summary>
        public StandardCommandRouter()
        {
            _handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a handler for a specific command verb.
        /// </summary>
        /// <param name="verb">The command verb (e.g., "say", "move").</param>
        /// <param name="handler">The handler to execute for this verb.</param>
        public bool RegisterHandler(string verb, ICommandHandler handler)
        {
            // Validate the string verb - make sure its not null or empty.
            if (string.IsNullOrEmpty(verb))
                throw new ArgumentNullException(nameof(verb), "Verb cannot be null or empty");

            // Validate the handler - make sure its not null.
            if (handler == null)
                throw new ArgumentNullException(nameof(handler), "Handler cannot be null");

            // Check if theres already a handler registered for this verb. If not, add it.
            bool result = _handlers.TryAdd(verb, handler);

            return result;
        }

        /// <summary>
        /// Routes a command context to its corresponding handler.
        /// </summary>
        /// <param name="context">The enriched command context.</param>
        /// <returns>The matching handler, or null if command is unknown.</returns>
        public ICommandHandler? Route(Types.ParsedCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command), "Context cannot be null");

            // Try to find a handler for this verb.
            _handlers.TryGetValue(command.CommandType, out ICommandHandler? handler);
            return handler;
        }
    }
}
