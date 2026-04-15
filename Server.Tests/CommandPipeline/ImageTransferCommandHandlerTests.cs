using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Shared.Identity;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for <see cref="ImageTransferCommandHandler"/>.
/// Exercises argument validation, JPEG magic-byte detection, and the happy path.
/// Temp files are created on disk because the handler uses <see cref="File"/> directly.
/// </summary>
[TestClass]
public class ImageTransferCommandHandlerTests
{
    private ImageTransferCommandHandler _handler = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _handler = new ImageTransferCommandHandler();
        _tempDir = Path.Combine(Path.GetTempPath(), $"ImgHandlerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static CommandContext BuildContext(params string[] args) =>
        new CommandContext(
            command: new ParsedCommand { CommandType = "sendimage", Arguments = args },
            playerState: null,
            worldState: null,
            success: true,
            errorMessage: null);

    /// <summary>Creates a JPEG file with the correct magic bytes and the specified total size.</summary>
    private string CreateFakeJpeg(int sizeBytes)
    {
        string path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.jpg");
        byte[] data = new byte[sizeBytes];
        // Write JPEG magic: FF D8 FF
        data[0] = 0xFF;
        data[1] = 0xD8;
        data[2] = 0xFF;
        File.WriteAllBytes(path, data);
        return path;
    }

    /// <summary>Creates a non-JPEG file (PNG magic bytes) at the specified size.</summary>
    private string CreateNonJpegFile(int sizeBytes)
    {
        string path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.png");
        byte[] data = new byte[sizeBytes];
        // PNG magic: 89 50 4E 47
        data[0] = 0x89;
        data[1] = 0x50;
        data[2] = 0x4E;
        data[3] = 0x47;
        File.WriteAllBytes(path, data);
        return path;
    }

    // -------------------------------------------------------------------------
    // Argument validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_NoArguments_ReturnsUsageError()
    {
        var result = await _handler.ExecuteAsync(BuildContext());

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "Usage");
        Assert.IsNull(result.BinaryPayload);
    }

    [TestMethod]
    public async Task Execute_FileDoesNotExist_ReturnsError()
    {
        var result = await _handler.ExecuteAsync(BuildContext(@"C:\definitely\not\real.jpg"));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "not found");
        Assert.IsNull(result.BinaryPayload);
    }

    // -------------------------------------------------------------------------
    // JPEG validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_NonJpegFile_ReturnsError()
    {
        string path = CreateNonJpegFile(sizeBytes: 1024);

        var result = await _handler.ExecuteAsync(BuildContext(path));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "FF D8 FF");
        Assert.IsNull(result.BinaryPayload);
    }

    [TestMethod]
    public async Task Execute_ValidJpeg_ReturnsBinaryPayload()
    {
        string path = CreateFakeJpeg(sizeBytes: 1024);

        var result = await _handler.ExecuteAsync(BuildContext(path));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BinaryPayload);
        Assert.AreEqual(1024, result.BinaryPayload.Length);
        // Confirm BinaryPayload carries the correct magic bytes
        Assert.AreEqual(0xFF, result.BinaryPayload[0]);
        Assert.AreEqual(0xD8, result.BinaryPayload[1]);
        Assert.AreEqual(0xFF, result.BinaryPayload[2]);
    }

    [TestMethod]
    public async Task Execute_LargeJpeg_ReturnsBinaryPayloadOverOneMB()
    {
        const int oneMB = 1024 * 1024;
        string path = CreateFakeJpeg(sizeBytes: oneMB + 512);

        var result = await _handler.ExecuteAsync(BuildContext(path));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BinaryPayload);
        Assert.IsTrue(result.BinaryPayload.Length > oneMB,
            $"Expected payload > 1 MB, got {result.BinaryPayload.Length} bytes.");
    }
}
