using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<ILogger<CaptureOrchestrator>> _mockLogger;
    private readonly CaptureOrchestrator _orchestrator;

    public CaptureOrchestratorTests()
    {
        _mockPermissionService = new Mock<IPermissionService>();
        _mockDisplayService = new Mock<IDisplayService>();
        _mockScreenCaptureService = new Mock<IScreenCaptureService>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<CaptureOrchestrator>>();

        _orchestrator = new CaptureOrchestrator(
            _mockPermissionService.Object,
            _mockDisplayService.Object,
            _mockScreenCaptureService.Object,
            _mockImageProcessingService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
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

    /// <summary>
    /// GenerateFilePathメソッドのテスト
    /// Requirements: 6.2 (ファイル名形式: screenshot_yyyyMMdd_HHmmssfff.png)
    /// </summary>
    [Fact]
    public void GenerateFilePath_ShouldGenerateCorrectFileNameFormat()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CanvasSnapTest_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);

        try
        {
            // Act
            var filePath = CaptureOrchestrator.GenerateFilePath(testDir);

            // Assert
            var fileName = Path.GetFileName(filePath);
            Assert.StartsWith("screenshot_", fileName);
            Assert.EndsWith(".png", fileName);
            // ファイル名の形式: screenshot_yyyyMMdd_HHmmssfff.png
            Assert.Matches(@"^screenshot_\d{8}_\d{9}\.png$", fileName);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    /// <summary>
    /// 同名ファイルが存在する場合の連番付与テスト
    /// Requirements: 6.3 (同名ファイル存在時の_001, _002連番付与)
    /// </summary>
    [Fact]
    public void GenerateFilePath_WhenFileExists_ShouldAppendSequenceNumber()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CanvasSnapTest_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        var fixedTimestamp = new DateTime(2024, 01, 02, 03, 04, 05, 678, DateTimeKind.Utc).AddTicks(9000); // 678ms + 9 ticks -> fff matches

        try
        {
            // 最初のファイルパスを生成して作成
            var firstPath = CaptureOrchestrator.GenerateFilePath(testDir, fixedTimestamp);
            File.WriteAllText(firstPath, "test");

            // 同じタイムスタンプを指定して2回目を生成し、連番付与を強制
            var secondPath = CaptureOrchestrator.GenerateFilePath(testDir, fixedTimestamp);

            // Assert
            Assert.NotEqual(firstPath, secondPath);

            // 2回目のファイル名には連番が含まれるはず
            var secondFileName = Path.GetFileName(secondPath);
            // screenshot_yyyyMMdd_HHmmssfff_001.png のような形式
            Assert.Matches(@"^screenshot_\d{8}_\d{9}_\d{3}\.png$", secondFileName);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    /// <summary>
    /// 複数の同名ファイルが存在する場合の連番インクリメントテスト
    /// Requirements: 6.3 (同名ファイル存在時の_001, _002連番付与)
    /// </summary>
    [Fact]
    public void GenerateFilePath_WhenMultipleFilesExist_ShouldIncrementSequenceNumber()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CanvasSnapTest_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        var fixedTimestamp = new DateTime(2024, 05, 06, 07, 08, 09, 123, DateTimeKind.Utc).AddTicks(4000);

        try
        {
            // 同じタイムスタンプのファイルを手動で作成してシミュレート
            var baseTimestamp = fixedTimestamp.ToString("yyyyMMdd_HHmmssfff");
            var firstFile = Path.Combine(testDir, $"screenshot_{baseTimestamp}.png");
            var secondFile = Path.Combine(testDir, $"screenshot_{baseTimestamp}_001.png");

            File.WriteAllText(firstFile, "test1");
            File.WriteAllText(secondFile, "test2");

            // Act - 同じタイムスタンプのGenerateFilePathは_002を返すはず
            var thirdPath = CaptureOrchestrator.GenerateFilePath(testDir, fixedTimestamp);

            // Assert - 生成されたファイルパスが既存ファイルと異なることを確認
            Assert.NotEqual(firstFile, thirdPath);
            Assert.NotEqual(secondFile, thirdPath);

            // ファイル名の形式が正しいことを確認
            var thirdFileName = Path.GetFileName(thirdPath);
            Assert.Matches(@"^screenshot_\d{8}_\d{9}_002\.png$", thirdFileName);

            // 実際にファイルが重複していないことを確認
            Assert.False(File.Exists(thirdPath), "生成されたパスのファイルは存在しないべき");
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    /// <summary>
    /// ディレクトリが存在しない場合のテスト
    /// Requirements: 6.4 (保存先ディレクトリの自動作成)
    /// Note: この機能はExecuteCaptureAsyncでテスト済み
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenDirectoryDoesNotExist_ShouldCreateDirectory()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CanvasSnapTest_" + Guid.NewGuid());
        var settings = CreateDefaultSettings();
        settings = settings with { SaveDirectory = testDir };

        var displayInfo = new DisplayInfo("1", "Display1", 0, 0, 1920, 1080, 2.0, true);
        var imageData = new byte[] { 1, 2, 3, 4 };

        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(true);
        _mockDisplayService.Setup(x => x.GetDisplayInfoAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(displayInfo);
        _mockScreenCaptureService.Setup(x => x.CaptureRegionAsync(It.IsAny<CaptureRegion>()))
            .ReturnsAsync(imageData);

        try
        {
            // Act
            var result = await _orchestrator.ExecuteCaptureAsync(settings);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(Directory.Exists(testDir), "ディレクトリが作成されているべき");
            Assert.True(File.Exists(result.Value), "ファイルが作成されているべき");
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    /// <summary>
    /// ログ記録のテスト - 権限拒否時のエラーログ
    /// Requirements: 6.7, 10.2 (エラーハンドリングとログ記録)
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenPermissionDenied_ShouldLogError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ReturnsAsync(false);

        // Act
        await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Permission denied")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ログ記録のテスト - キャプチャ失敗時の警告ログ
    /// Requirements: 6.7, 10.2 (エラーハンドリングとログ記録)
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenScreenCaptureFails_ShouldLogWarning()
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
        await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Screen capture failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ログ記録のテスト - 保存失敗時のエラーログ
    /// Requirements: 6.7, 6.8, 10.3 (保存エラーのログ記録)
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenFileSaveFails_ShouldLogError()
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
        await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File save failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ログ記録のテスト - 予期しないエラー時のエラーログ
    /// Requirements: 10.3 (予期しないエラーのログ記録)
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenUnexpectedErrorOccurs_ShouldLogError()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        _mockPermissionService.Setup(x => x.CheckPermissionsAsync())
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ログ記録のテスト - 成功時の情報ログ
    /// Requirements: 10.1 (成功時のログ記録)
    /// </summary>
    [Fact]
    public async Task ExecuteCaptureAsync_WhenSuccessful_ShouldLogInformation()
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
        await _orchestrator.ExecuteCaptureAsync(settings);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Screenshot saved successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
