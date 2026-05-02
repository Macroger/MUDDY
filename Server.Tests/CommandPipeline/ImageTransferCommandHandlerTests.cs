using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

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
    private string _imagesDir = null!;
    private List<string> _createdFiles = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _handler = new ImageTransferCommandHandler();
        _imagesDir = Path.Combine(AppContext.BaseDirectory, "Images");
        Directory.CreateDirectory(_imagesDir);
        _createdFiles = [];
    }

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (string file in _createdFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
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

    /// <summary>Creates a JPEG file in the Images folder with the correct magic bytes.
    /// Returns the bare filename without extension — pass this directly to the handler.</summary>
    private string CreateFakeJpeg(int sizeBytes)
    {
        string name = Guid.NewGuid().ToString("N");
        string path = Path.Combine(_imagesDir, name + ".jpg");
        byte[] data = new byte[sizeBytes];
        data[0] = 0xFF;
        data[1] = 0xD8;
        data[2] = 0xFF;
        File.WriteAllBytes(path, data);
        _createdFiles.Add(path);
        return name;
    }

    /// <summary>Creates a file with non-JPEG content (PNG magic bytes) but a .jpg extension
    /// in the Images folder. Returns the bare filename without extension.</summary>
    private string CreateNonJpegFile(int sizeBytes)
    {
        string name = Guid.NewGuid().ToString("N");
        string path = Path.Combine(_imagesDir, name + ".jpg");
        byte[] data = new byte[sizeBytes];
        // PNG magic: 89 50 4E 47
        data[0] = 0x89;
        data[1] = 0x50;
        data[2] = 0x4E;
        data[3] = 0x47;
        File.WriteAllBytes(path, data);
        _createdFiles.Add(path);
        return name;
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
        var result = await _handler.ExecuteAsync(BuildContext("nonexistent_image_xyz"));

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
        string name = CreateNonJpegFile(sizeBytes: 1024);

        var result = await _handler.ExecuteAsync(BuildContext(name));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "FF D8 FF");
        Assert.IsNull(result.BinaryPayload);
    }

    [TestMethod]
    public async Task Execute_ValidJpeg_ReturnsBinaryPayload()
    {
        string name = CreateFakeJpeg(sizeBytes: 1024);

        var result = await _handler.ExecuteAsync(BuildContext(name));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BinaryPayload);
        Assert.HasCount(1024, result.BinaryPayload);
        // Confirm BinaryPayload carries the correct magic bytes
        Assert.AreEqual(0xFF, result.BinaryPayload[0]);
        Assert.AreEqual(0xD8, result.BinaryPayload[1]);
        Assert.AreEqual(0xFF, result.BinaryPayload[2]);
    }

    [TestMethod]
    public async Task Execute_LargeJpeg_ReturnsBinaryPayloadOverOneMB()
    {
        const int oneMB = 1024 * 1024;
        string name = CreateFakeJpeg(sizeBytes: oneMB + 512);

        var result = await _handler.ExecuteAsync(BuildContext(name));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BinaryPayload);
        Assert.IsGreaterThan(oneMB, result.BinaryPayload.Length,
            $"Expected payload > 1 MB, got {result.BinaryPayload.Length} bytes.");
    }
}
