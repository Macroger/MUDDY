using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.Services.Command
{
    /// <summary>
    /// Serializes client text commands into JSON format for transmission to the server.
    /// </summary>
    public interface ICommandSerializer
    {
        /// <summary>
        /// Converts a text command into JSON with verb and args structure.
        /// </summary>
        /// <param name="commandText">The command text (e.g., "login Matt 123")</param>
        /// <returns>JSON string in format: {"verb":"login","args":["Matt","123"]}</returns>
        string SerializeCommand(string commandText);
    }
}