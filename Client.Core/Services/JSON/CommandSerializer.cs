using System.Text.Json;

namespace Client.Core.Services.Command
{
    /// <summary>
    /// Serializes client text commands into JSON format expected by the server.
    /// </summary>
    public sealed class CommandSerializer : ICommandSerializer
    {
        /// <summary>
        /// Converts a text command into JSON with verb and args structure.
        /// </summary>
        /// <param name="commandText">The command text (e.g., "login Matt 123")</param>
        /// <returns>JSON string in format: {"verb":"login","args":["Matt","123"]}</returns>
        /// <exception cref="ArgumentNullException">Thrown when commandText is null.</exception>
        /// <exception cref="ArgumentException">Thrown when commandText is empty or whitespace.</exception>
        public string SerializeCommand(string commandText)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException("Command text cannot be empty or whitespace.", nameof(commandText));
            }

            // Parse the command text into verb and arguments
            string[] parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string verb = parts[0];
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            // Create JSON command structure
            var jsonCommand = new
            {
                verb = verb,
                args = args
            };

            // Serialize to JSON
            return JsonSerializer.Serialize(jsonCommand);
        }
    }
}