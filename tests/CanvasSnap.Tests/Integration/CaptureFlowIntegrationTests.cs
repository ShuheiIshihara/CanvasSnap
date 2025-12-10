using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Integration;

/// <summary>
/// キャプチャフロー統合テスト
/// Task 13.2: CaptureOrchestrator + モックサービスでエンドツーエンド実行
/// Requirements: 11.1 (パフォーマンス)
/// </summary>
public class CaptureFlowIntegrationTests : IDisposable
{
    private readonly Mock<IPermissionService> _mockPermission;
    private readonly Mock<IDisplayService> _mockDisplay;
    private readonly Mock<IScreenCaptureService> _mockScreenCapture;
    private readonly Mock<IImageProcessingService> _mockImageProcessing;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<ILogger<CaptureOrchestrator>> _mockLogger;
    private readonly CaptureOrchestrator _orchestrator;
    private readonly string _tempDirectory;

    public CaptureFlowIntegrationTests()
    {
        _mockPermission = new Mock<IPermissionService>();
        _mockDisplay = new Mock<IDisplayService>();
        _mockScreenCapture = new Mock<IScreenCaptureService>();
        _mockImageProcessing = new Mock<IImageProcessingService>();
        _mockNotification = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<CaptureOrchestrator>>();

        _orchestrator = new CaptureOrchestrator(
            _mockPermission.Object,
            _mockDisplay.Object,
            _mockScreenCapture.Object,
            _mockImageProcessing.Object,
            _mockNotification.Object,
            _mockLogger.Object
        );

        // テスト用一時ディレクトリ
        _tempDirectory = Path.Combine(Path.GetTempPath(), "CanvasSnapTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 正常フロー: 権限OK → キャプチャ成功 → マスクなし → 保存成功 → 通知
    /// </summary>
    [Fact]
    public async Task CaptureFlow_Success_WithoutMask_ShouldCompleteEndToEnd()
    {
        // Arrange
        var region = new CaptureRegion(100, 100, 800, 600);
        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = _tempDirectory,
            IsMaskEnabled = false
        };

        var fakeImageData = new byte[] { 137, 80, 78, 71 }; // PNG header

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(true);

        _mockDisplay.Setup(d => d.GetDisplayInfoAsync(region))
            .ReturnsAsync(new DisplayInfo("1", "Primary", 0, 0, 1920, 1080, 2.0, true));

        _mockScreenCapture.Setup(s => s.CaptureRegionAsync(region))
            .ReturnsAsync(fakeImageData);

        _mockNotification.Setup(n => n.ShowNotificationAsync(It.IsAny<string>(), NotificationType.Info))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.StartsWith(Path.Combine(_tempDirectory, "screenshot_"), result.Value);
        Assert.True(File.Exists(result.Value));

        _mockPermission.Verify(p => p.CheckPermissionsAsync(), Times.Once);
        _mockDisplay.Verify(d => d.GetDisplayInfoAsync(region), Times.Once);
        _mockScreenCapture.Verify(s => s.CaptureRegionAsync(region), Times.Once);
        _mockImageProcessing.Verify(i => i.ApplyMaskAsync(It.IsAny<byte[]>(), It.IsAny<IEnumerable<MaskRegion>>()), Times.Never);
        _mockNotification.Verify(n => n.ShowNotificationAsync("スクリーンショットを保存しました", NotificationType.Info), Times.Once);

        // Cleanup
        if (File.Exists(result.Value))
        {
            File.Delete(result.Value);
        }
    }

    /// <summary>
    /// 正常フロー: 権限OK → キャプチャ成功 → マスク適用 → 保存成功 → 通知
    /// </summary>
    [Fact]
    public async Task CaptureFlow_Success_WithMask_ShouldApplyMaskAndComplete()
    {
        // Arrange
        var region = new CaptureRegion(100, 100, 800, 600);
        var maskRegions = new[] { new MaskRegion(50, 50, 100, 50) };
        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = maskRegions,
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = _tempDirectory,
            IsMaskEnabled = true
        };

        var fakeImageData = new byte[] { 137, 80, 78, 71 };
        var fakeMaskedImageData = new byte[] { 137, 80, 78, 71, 13, 10 };

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(true);

        _mockDisplay.Setup(d => d.GetDisplayInfoAsync(region))
            .ReturnsAsync(new DisplayInfo("1", "Primary", 0, 0, 1920, 1080, 2.0, true));

        _mockScreenCapture.Setup(s => s.CaptureRegionAsync(region))
            .ReturnsAsync(fakeImageData);

        _mockImageProcessing.Setup(i => i.ApplyMaskAsync(fakeImageData, It.IsAny<IEnumerable<MaskRegion>>()))
            .ReturnsAsync(fakeMaskedImageData);

        _mockNotification.Setup(n => n.ShowNotificationAsync(It.IsAny<string>(), NotificationType.Info))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(File.Exists(result.Value));

        _mockImageProcessing.Verify(i => i.ApplyMaskAsync(fakeImageData, maskRegions), Times.Once);
        _mockNotification.Verify(n => n.ShowNotificationAsync("スクリーンショットを保存しました", NotificationType.Info), Times.Once);

        // Cleanup
        if (File.Exists(result.Value))
        {
            File.Delete(result.Value);
        }
    }

    /// <summary>
    /// エラーフロー: 権限不足 → クリティカルエラーダイアログ → Result.Error
    /// </summary>
    [Fact]
    public async Task CaptureFlow_PermissionDenied_ShouldReturnErrorAndShowDialog()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 100, 800, 600),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = _tempDirectory,
            IsMaskEnabled = false
        };

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(false);

        _mockNotification.Setup(n => n.ShowCriticalErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.PermissionDenied, result.Error);

        _mockPermission.Verify(p => p.CheckPermissionsAsync(), Times.Once);
        _mockScreenCapture.Verify(s => s.CaptureRegionAsync(It.IsAny<CaptureRegion>()), Times.Never);
        _mockNotification.Verify(n => n.ShowCriticalErrorAsync(
            "権限エラー",
            It.Is<string>(s => s.Contains("必要な権限が付与されていません")),
            It.IsAny<string[]>()), Times.Once);
    }

    /// <summary>
    /// エラーフロー: ディスプレイ切断 → 軽微エラー通知 → Result.Error
    /// </summary>
    [Fact]
    public async Task CaptureFlow_DisplayUnavailable_ShouldReturnErrorAndShowNotification()
    {
        // Arrange
        var region = new CaptureRegion(100, 100, 800, 600);
        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = _tempDirectory,
            IsMaskEnabled = false
        };

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(true);

        _mockDisplay.Setup(d => d.GetDisplayInfoAsync(region))
            .ReturnsAsync((DisplayInfo?)null);

        _mockNotification.Setup(n => n.ShowNotificationAsync(It.IsAny<string>(), NotificationType.Error))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.DisplayUnavailable, result.Error);

        _mockDisplay.Verify(d => d.GetDisplayInfoAsync(region), Times.Once);
        _mockScreenCapture.Verify(s => s.CaptureRegionAsync(It.IsAny<CaptureRegion>()), Times.Never);
        _mockNotification.Verify(n => n.ShowNotificationAsync(
            "キャプチャ中止: ディスプレイが利用できません",
            NotificationType.Error), Times.Once);
    }

    /// <summary>
    /// エラーフロー: キャプチャ失敗 → 軽微エラー通知 → Result.Error
    /// </summary>
    [Fact]
    public async Task CaptureFlow_CaptureFailed_ShouldReturnErrorAndShowNotification()
    {
        // Arrange
        var region = new CaptureRegion(100, 100, 800, 600);
        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = _tempDirectory,
            IsMaskEnabled = false
        };

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(true);

        _mockDisplay.Setup(d => d.GetDisplayInfoAsync(region))
            .ReturnsAsync(new DisplayInfo("1", "Primary", 0, 0, 1920, 1080, 2.0, true));

        _mockScreenCapture.Setup(s => s.CaptureRegionAsync(region))
            .ThrowsAsync(new ScreenCaptureException("screencapture failed"));

        _mockNotification.Setup(n => n.ShowNotificationAsync(It.IsAny<string>(), NotificationType.Error))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.CaptureFailed, result.Error);

        _mockNotification.Verify(n => n.ShowNotificationAsync(
            "キャプチャに失敗しました",
            NotificationType.Error), Times.Once);
    }

    /// <summary>
    /// エラーフロー: ファイル保存失敗 → クリティカルエラーダイアログ → Result.Error
    /// </summary>
    [Fact]
    public async Task CaptureFlow_SaveFailed_ShouldReturnErrorAndShowDialog()
    {
        // Arrange
        var region = new CaptureRegion(100, 100, 800, 600);
        var readOnlyDirectory = Path.Combine(Path.GetTempPath(), "CanvasSnapTests", "ReadOnly", Guid.NewGuid().ToString());
        Directory.CreateDirectory(readOnlyDirectory);

        // Windows/macOSで読み取り専用に設定
        if (OperatingSystem.IsWindows())
        {
            var dirInfo = new DirectoryInfo(readOnlyDirectory);
            dirInfo.Attributes = FileAttributes.ReadOnly;
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            // chmod 555 (読み取り専用)
            System.Diagnostics.Process.Start("chmod", $"555 \"{readOnlyDirectory}\"")?.WaitForExit();
        }

        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = readOnlyDirectory,
            IsMaskEnabled = false
        };

        var fakeImageData = new byte[] { 137, 80, 78, 71 };

        _mockPermission.Setup(p => p.CheckPermissionsAsync())
            .ReturnsAsync(true);

        _mockDisplay.Setup(d => d.GetDisplayInfoAsync(region))
            .ReturnsAsync(new DisplayInfo("1", "Primary", 0, 0, 1920, 1080, 2.0, true));

        _mockScreenCapture.Setup(s => s.CaptureRegionAsync(region))
            .ReturnsAsync(fakeImageData);

        _mockNotification.Setup(n => n.ShowCriticalErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CaptureError.SaveFailed, result.Error);

        _mockNotification.Verify(n => n.ShowCriticalErrorAsync(
            "保存エラー",
            It.Is<string>(s => s.Contains("ファイルの保存に失敗しました")),
            It.IsAny<string[]>()), Times.Once);

        // Cleanup
        if (OperatingSystem.IsWindows())
        {
            var dirInfo = new DirectoryInfo(readOnlyDirectory);
            dirInfo.Attributes = FileAttributes.Normal;
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            System.Diagnostics.Process.Start("chmod", $"755 \"{readOnlyDirectory}\"")?.WaitForExit();
        }

        try
        {
            Directory.Delete(readOnlyDirectory, true);
        }
        catch
        {
            // ベストエフォート
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // ベストエフォート
            }
        }
    }
}
