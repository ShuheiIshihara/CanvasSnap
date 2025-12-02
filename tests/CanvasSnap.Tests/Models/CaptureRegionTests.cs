using CanvasSnap.Models;
using Xunit;

namespace CanvasSnap.Tests.Models;

/// <summary>
/// CaptureRegionモデルのテスト
/// Requirements: 2.4 (キャプチャ領域の物理座標保存)
/// </summary>
public class CaptureRegionTests
{
    [Fact]
    public void CaptureRegion_ShouldBeCreated_WithValidValues()
    {
        // Arrange & Act
        var region = new CaptureRegion(100, 200, 1920, 1080);

        // Assert
        Assert.Equal(100, region.X);
        Assert.Equal(200, region.Y);
        Assert.Equal(1920, region.Width);
        Assert.Equal(1080, region.Height);
    }

    [Fact]
    public void CaptureRegion_ShouldSupportEqualityComparison()
    {
        // Arrange
        var region1 = new CaptureRegion(100, 200, 1920, 1080);
        var region2 = new CaptureRegion(100, 200, 1920, 1080);
        var region3 = new CaptureRegion(150, 200, 1920, 1080);

        // Act & Assert
        Assert.Equal(region1, region2);
        Assert.NotEqual(region1, region3);
    }

    [Fact]
    public void CaptureRegion_ShouldSupportNegativeCoordinates_ForSecondaryDisplay()
    {
        // Arrange & Act - セカンダリディスプレイが左側にある場合、負の座標値を持つ
        var region = new CaptureRegion(-1920, 0, 1920, 1080);

        // Assert
        Assert.Equal(-1920, region.X);
        Assert.Equal(0, region.Y);
        Assert.Equal(1920, region.Width);
        Assert.Equal(1080, region.Height);
    }

    [Theory]
    [InlineData(0, 0, 1920, 1080)]
    [InlineData(100, 200, 800, 600)]
    [InlineData(-1920, -1080, 3840, 2160)]
    public void CaptureRegion_ShouldAcceptVariousValidRegions(int x, int y, int width, int height)
    {
        // Arrange & Act
        var region = new CaptureRegion(x, y, width, height);

        // Assert
        Assert.Equal(x, region.X);
        Assert.Equal(y, region.Y);
        Assert.Equal(width, region.Width);
        Assert.Equal(height, region.Height);
    }

    [Fact]
    public void CaptureRegion_ShouldBeImmutable()
    {
        // Arrange
        var region = new CaptureRegion(100, 200, 1920, 1080);

        // Act - recordは不変なので、withを使って新しいインスタンスを作成
        var modifiedRegion = region with { Width = 1280 };

        // Assert - 元のインスタンスは変更されない
        Assert.Equal(1920, region.Width);
        Assert.Equal(1280, modifiedRegion.Width);
    }
}
