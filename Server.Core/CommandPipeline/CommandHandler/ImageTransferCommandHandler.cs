// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles the "sendimage" command. Reads a JPEG file from the <c>Images</c> subfolder
    /// next to the application executable and returns its bytes as a
    /// <see cref="CommandResult.BinaryPayload"/>, which the pipeline orchestrator transmits
    /// as a <c>BinaryTransfer</c> packet. The <c>.jpg</c> extension is appended automatically.
    /// <para>
    /// Usage: <c>sendimage &lt;filename without extension&gt;</c>
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
                    Message = "Usage: sendimage <filename>"
                });
            }

            string imagesFolder = Path.Combine(AppContext.BaseDirectory, "Images");
            string resolvedPath = Path.Combine(imagesFolder, path + ".jpg");

            if (!File.Exists(resolvedPath))
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = $"Image not found: {resolvedPath}"
                });
            }

            byte[] fileBytes;
            try
            {
                fileBytes = File.ReadAllBytes(resolvedPath);
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
                Message = $"Sending {fileBytes.Length:N0}-byte JPEG ({path}.jpg).",
                BinaryPayload = fileBytes
            });
        }
    }
}
