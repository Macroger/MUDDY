using Server.Core.CommandPipeline.Types;
using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Server.Core.CommandPipeline.Parser
{
    public class StandardCommandParser
    {
        public ParseResult Parse(TransportEnvelope envelope)
        {
            try
            {
                // Extract JSON from payload
                string json = Encoding.UTF8.GetString(envelope.Payload);

                // Deserialize JSON
                var cmdJson = JsonSerializer.Deserialize<JsonCommand>(json);

                // Validate deserialization
                if (cmdJson == null) return CreateErrorResult("Payload is not valid JSON", CommandErrorType.INVALID_JSON);

                // Validate required fields
                if (string.IsNullOrEmpty(cmdJson.Verb)) return CreateErrorResult("Command must specify a 'verb' field", CommandErrorType.MISSING_VERB);

                // Build ParsedCommand
                var command = new ParsedCommand
                {
                    CommandType = cmdJson.Verb.ToLowerInvariant(),
                    Arguments = cmdJson.Args ?? Array.Empty<string>(),
                    MsgId = envelope.MessageId
                };

                return new ParseResult
                {
                    Success = true,
                    Command = command
                };
            }
            catch (JsonException ex)
            {
                return CreateErrorResult($"JSON parse error: {ex.Message}", CommandErrorType.UNKNOWN_ERROR);
            }
            catch (Exception ex)
            {
                return CreateErrorResult($"Unexpected error: {ex.Message}", CommandErrorType.UNKNOWN_ERROR);
            }
        }

        private ParseResult CreateErrorResult(string errorMsg, CommandErrorType errorType)
        {
            ParseResult result = new ParseResult
            {
                Success = false,
                Command = null,
                ErrorMessage = errorMsg,
                ErrorType = errorType
            };

            return result;
        }
    }
}
