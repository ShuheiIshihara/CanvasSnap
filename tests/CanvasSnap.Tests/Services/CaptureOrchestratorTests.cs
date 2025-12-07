using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// CaptureOrchestratorの単体テスト
/// Requirements: 1.1 (安全なスクリーンキャプチャ)
/// Requirements: 6.1 (キャプチャ完了時のファイル保存)
/// Requirements: 6.6 (保存成功時の通知)
/// Requirements: 10.1 (成功通知)
/// </summary>
public class CaptureOrchestratorTests
{
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<IDisplayService> _mockDisplayService;
    private readonly Mock<IScreenCaptureService> _mockScreenCaptureService;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly CaptureOrchestrator _orchestrator;

    public CaptureOrchestratorTests()
    {
        _mockPermissionService = new Mock<IPermissionService>();
        _mockDisplayService = new Mock<IDisplayService>();
        _mockScreenCaptureService = new Mock<IScreenCaptureService>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockNotificationService = new Mock<INotificationService>();

        _orchestrator = new CaptureOrchestrator(
            _mockPermissionService.Object,
            _mockDisplayService.Object,
            _mockScreenCaptureService.Object,
            _mockImageProcessingService.Object,
            _mockNotificationService.Object);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenPermissionDenied_ShouldReturnPermissionDeniedError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.PermissionDenied, result.Error);
        _mockNotificationService.Verify(
            x => x.ShowCriticalErrorAsync(It.IsAny<string>(), It.IsAny<string>(), null),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenDisplayUnavailable_ShouldReturnDisplayUnavailableError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync((DisplayInfo?)null);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.DisplayUnavailable, result.Error);
        _mockNotificationService.Verify(
            x => x.ShowNotificationAsync(It.IsAny<string>(), NotificationType.Error),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenCaptureSucceeds_ShouldReturnSuccessWithFilePath()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);
        var imageData = new byte[] { 1, 2, 3, 4 };

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(imageData);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith(".png", result.Value);
        _mockNotificationService.Verify(
            x => x.ShowNotificationAsync("スクリーンショットを保存しました", NotificationType.Info),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenMaskEnabled_ShouldApplyMask()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        settings = settings with
        {
            IsMaskEnabled = true,
            MaskRegions = new[] { new MaskRegion(10, 10, 100, 100) }
        };
        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);
        var imageData = new byte[] { 1, 2, 3, 4 };
        var maskedData = new byte[] { 5, 6, 7, 8 };

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(imageData);
        _mockImageProcessingService.Setup(x => x.ApplyMaskAsync(imageData, It.IsAny<MaskRegion[]>()))
            .ReturnsAsync(maskedData);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsSuccess);
        _mockImageProcessingService.Verify(
            x => x.ApplyMaskAsync(imageData, It.Is<MaskRegion[]>(m => m.Length == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenMaskDisabled_ShouldSkipMask()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        settings = settings with { IsMaskEnabled = false };
        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);
        var imageData = new byte[] { 1, 2, 3, 4 };

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(imageData);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsSuccess);
        _mockImageProcessingService.Verify(
            x => x.ApplyMaskAsync(It.IsAny<byte[]>(), It.IsAny<MaskRegion[]>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenScreenCaptureFails_ShouldReturnCaptureFailedError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ThrowsAsync(new ScreenCaptureException("Capture failed"));

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.CaptureFailed, result.Error);
        _mockNotificationService.Verify(
            x => x.ShowNotificationAsync("キャプチャに失敗しました", NotificationType.Error),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenFileSaveFails_ShouldReturnSaveFailedError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        settings = settings with { SaveDirectory = "/invalid/path" };
        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);
        var imageData = new byte[] { 1, 2, 3, 4 };

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(imageData);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.SaveFailed, result.Error);
        _mockNotificationService.Verify(
            x => x.ShowCriticalErrorAsync("保存エラー", It.IsAny<string>(), null),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCaptureAsync_WhenUnexpectedErrorOccurs_ShouldReturnUnknownError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.Unknown, result.Error);
        _mockNotificationService.Verify(
            x => x.ShowCriticalErrorAsync("エラー", "予期しないエラーが発生しました", null),
            Times.Once);
    }

    private static CaptureSettings CreateDefaultSettings()
    {
        return new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 1200, 720),
            MaskRegions = Array.Empty<MaskRegion>(),
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53),
            SaveDirectory = Path.GetTempPath(),
            IsMaskEnabled = false
        };
    }
}
