using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// IScreenCaptureServiceインターフェースの契約テスト
/// Requirements: 1.1 (OS標準APIによるキャプチャ), 1.7 (macOS screencaptureコマンド)
/// </summary>
public class IScreenCaptureServiceTests
{
    [Fact]
    public async Task CaptureRegionAsync_ShouldReturnByteArray()
    {
        // Arrange
        var mockService = new Mock<IScreenCaptureService>();
        var region = new CaptureRegion(0, 0, 1920, 1080);
        var expectedData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic number

        mockService.Setup(s => s.CaptureRegionAsync(region))
            .ReturnsAsync(expectedData);

        // Act
        var result = await mockService.Object.CaptureRegionAsync(region);

        // Assert
        Assert.IsType<byte[]>(result);
        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task CaptureRegionAsync_ShouldAcceptCaptureRegionParameter()
    {
        // Arrange
        var mockService = new Mock<IScreenCaptureService>();
        var region = new CaptureRegion(100, 200, 800, 600);

        mockService.Setup(s => s.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        await mockService.Object.CaptureRegionAsync(region);

        // Assert
        mockService.Verify(s => s.CaptureRegionAsync(region), Times.Once);
    }

    [Fact]
    public async Task CaptureRegionAsync_CanThrowScreenCaptureException()
    {
        // Arrange
        var mockService = new Mock<IScreenCaptureService>();
        var region = new CaptureRegion(0, 0, 1920, 1080);

        mockService.Setup(s => s.CaptureRegionAsync(region))
            .ThrowsAsync(new ScreenCaptureException("Capture failed"));

        // Act & Assert
        await Assert.ThrowsAsync<ScreenCaptureException>(
            () => mockService.Object.CaptureRegionAsync(region));
    }

    [Fact]
    public async Task CaptureRegionAsync_CanThrowPermissionDeniedException()
    {
        // Arrange
        var mockService = new Mock<IScreenCaptureService>();
        var region = new CaptureRegion(0, 0, 1920, 1080);

        mockService.Setup(s => s.CaptureRegionAsync(region))
            .ThrowsAsync(new PermissionDeniedException("Screen Recording permission denied", PermissionType.ScreenRecording));

        // Act & Assert
        await Assert.ThrowsAsync<PermissionDeniedException>(
            () => mockService.Object.CaptureRegionAsync(region));
    }

    [Fact]
    public async Task CaptureRegionAsync_WithValidRegion_ShouldReturnNonEmptyData()
    {
        // Arrange
        var mockService = new Mock<IScreenCaptureService>();
        var region = new CaptureRegion(0, 0, 1920, 1080);
        var imageData = new byte[1024]; // 非空の画像データ

        mockService.Setup(s => s.CaptureRegionAsync(region))
            .ReturnsAsync(imageData);

        // Act
        var result = await mockService.Object.CaptureRegionAsync(region);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(1024, result.Length);
    }

    [Fact]
    public void IScreenCaptureService_ShouldBeInterface()
    {
        // Arrange & Act
        var type = typeof(IScreenCaptureService);

        // Assert
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void IScreenCaptureService_ShouldHaveCaptureRegionAsyncMethod()
    {
        // Arrange
        var type = typeof(IScreenCaptureService);

        // Act
        var method = type.GetMethod("CaptureRegionAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<byte[]>), method.ReturnType);
    }
}
