using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// MacScreenCaptureServiceの単体テスト
/// Requirements: 1.1 (OS標準APIによるキャプチャ), 1.7 (macOS screencaptureコマンド)
/// </summary>
/// <remarks>
/// Phase 1 MVP: 単体テストはインターフェース契約とエラーハンドリングを検証
/// 実際のキャプチャ機能は統合テストまたは手動テストで確認
/// </remarks>
[Trait("Category", "RequiresDisplay")]
public class MacScreenCaptureServiceTests
{
    [Fact]
    public async Task CaptureRegionAsync_OnNonMacOS_ShouldReturnValidPngData()
    {
        // Arrange
        var service = new MacScreenCaptureService();
        var region = new CaptureRegion(0, 0, 100, 100);

        // Act
        var result = await service.CaptureRegionAsync(region);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // PNG magic number check
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
        Assert.Equal(0x4E, result[2]);
        Assert.Equal(0x47, result[3]);
    }

    [Fact]
    public async Task CaptureRegionAsync_ShouldAcceptDifferentRegions()
    {
        // Arrange
        var service = new MacScreenCaptureService();
        var region1 = new CaptureRegion(0, 0, 100, 100);
        var region2 = new CaptureRegion(100, 100, 200, 200);

        // Act
        var result1 = await service.CaptureRegionAsync(region1);
        var result2 = await service.CaptureRegionAsync(region2);

        // Assert - Both should return valid PNG data
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEmpty(result1);
        Assert.NotEmpty(result2);

        // PNG magic number check
        Assert.Equal(0x89, result1[0]);
        Assert.Equal(0x89, result2[0]);
    }

    [Fact]
    public void MacScreenCaptureService_ShouldImplementIScreenCaptureService()
    {
        // Arrange & Act
        var service = new MacScreenCaptureService();

        // Assert
        Assert.IsAssignableFrom<IScreenCaptureService>(service);
    }

    [Fact]
    public async Task CaptureRegionAsync_WithValidRegion_ShouldReturnNonEmptyData()
    {
        // Arrange
        var service = new MacScreenCaptureService();
        var region = new CaptureRegion(0, 0, 800, 600);

        // Act
        var result = await service.CaptureRegionAsync(region);

        // Assert
        Assert.True(result.Length > 0, "Captured image data should not be empty");
    }

    [Fact]
    public async Task CaptureRegionAsync_ShouldReturnPngFormat()
    {
        // Arrange
        var service = new MacScreenCaptureService();
        var region = new CaptureRegion(0, 0, 100, 100);

        // Act
        var result = await service.CaptureRegionAsync(region);

        // Assert
        // PNG signature verification
        Assert.True(result.Length >= 8, "PNG file must be at least 8 bytes (signature)");
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
        Assert.Equal(0x4E, result[2]);
        Assert.Equal(0x47, result[3]);
        Assert.Equal(0x0D, result[4]);
        Assert.Equal(0x0A, result[5]);
        Assert.Equal(0x1A, result[6]);
        Assert.Equal(0x0A, result[7]);
    }

    [Fact]
    public void MacScreenCaptureService_Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new MacScreenCaptureService());
        Assert.Null(exception);
    }
}
