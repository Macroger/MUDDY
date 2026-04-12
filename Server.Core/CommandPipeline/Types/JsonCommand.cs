using System.Text.Json.Serialization;

namespace Server.Core.CommandPipeline.Types
{
    public class JsonCommand
    {
        /// <summary>
        /// Gets or sets the verb associated with the command.
        /// </summary>
        [JsonPropertyName("verb")]
        public string? Verb { get; set; }

        /// <summary>
        /// Gets or sets the arguments associated with the command.
        /// </summary>
        [JsonPropertyName("args")]
        public string[]? Args { get; set; }
    }
}