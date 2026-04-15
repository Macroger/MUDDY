using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles the "sendimage" command. Reads a JPEG file from the path supplied as the
    /// first argument and returns its bytes as a <see cref="CommandResult.BinaryPayload"/>,
    /// which the pipeline orchestrator transmits as a <c>BinaryTransfer</c> packet.
    /// <para>
    /// Usage: <c>sendimage &lt;absolute-or-relative path to .jpg&gt;</c>
    /// </para>
    /// </summary>
    public sealed class ImageTransferCommandHandler : ICommandHandler
    {
        // JPEG files always start with FF D8 FF
        private static ReadOnlySpan<byte> JpegMagic => [0xFF, 0xD8, 0xFF];

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            string? path = context.Command.Arguments.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = "Usage: sendimage <filepath>"
                });
            }

            if (!File.Exists(path))
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = $"File not found: {path}"
                });
            }

            byte[] fileBytes;
            try
            {
                fileBytes = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = $"Could not read file: {ex.Message}"
                });
            }

            if (fileBytes.Length < JpegMagic.Length ||
                !fileBytes.AsSpan(0, JpegMagic.Length).SequenceEqual(JpegMagic))
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = "File is not a valid JPEG (missing FF D8 FF header)."
                });
            }

            return Task.FromResult(new CommandResult
            {
                Success = true,
                Message = $"Sending {fileBytes.Length:N0}-byte JPEG.",
                BinaryPayload = fileBytes
            });
        }
    }
}
