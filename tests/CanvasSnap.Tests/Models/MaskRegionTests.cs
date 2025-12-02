using CanvasSnap.Models;
using Xunit;

namespace CanvasSnap.Tests.Models;

/// <summary>
/// MaskRegionモデルのテスト
/// Requirements: 4.2 (マスク座標をキャプチャ領域左上を原点とする相対座標で保存)
/// </summary>
public class MaskRegionTests
{
    [Fact]
    public void MaskRegion_ShouldBeCreated_WithValidValues()
    {
        // Arrange & Act
        var mask = new MaskRegion(50, 100, 200, 50);

        // Assert
        Assert.Equal(50, mask.X);
        Assert.Equal(100, mask.Y);
        Assert.Equal(200, mask.Width);
        Assert.Equal(50, mask.Height);
    }

    [Fact]
    public void MaskRegion_ShouldSupportEqualityComparison()
    {
        // Arrange
        var mask1 = new MaskRegion(50, 100, 200, 50);
        var mask2 = new MaskRegion(50, 100, 200, 50);
        var mask3 = new MaskRegion(60, 100, 200, 50);

        // Act & Assert
        Assert.Equal(mask1, mask2);
        Assert.NotEqual(mask1, mask3);
    }

    [Fact]
    public void MaskRegion_ShouldUseRelativeCoordinates_FromCaptureRegionOrigin()
    {
        // Arrange & Act - キャプチャ領域の左上を(0,0)とする相対座標
        var mask = new MaskRegion(0, 0, 100, 50);

        // Assert - (0,0)はキャプチャ領域の左上隅を示す
        Assert.Equal(0, mask.X);
        Assert.Equal(0, mask.Y);
    }

    [Theory]
    [InlineData(0, 0, 100, 50)]
    [InlineData(50, 100, 200, 50)]
    [InlineData(200, 300, 150, 75)]
    public void MaskRegion_ShouldAcceptVariousValidMasks(int x, int y, int width, int height)
    {
        // Arrange & Act
        var mask = new MaskRegion(x, y, width, height);

        // Assert
        Assert.Equal(x, mask.X);
        Assert.Equal(y, mask.Y);
        Assert.Equal(width, mask.Width);
        Assert.Equal(height, mask.Height);
    }

    [Fact]
    public void MaskRegion_ShouldBeImmutable()
    {
        // Arrange
        var mask = new MaskRegion(50, 100, 200, 50);

        // Act - recordは不変なので、withを使って新しいインスタンスを作成
        var modifiedMask = mask with { Width = 300 };

        // Assert - 元のインスタンスは変更されない
        Assert.Equal(200, mask.Width);
        Assert.Equal(300, modifiedMask.Width);
    }

    [Fact]
    public void MaskRegion_ShouldSupportMultipleMasksInCollection()
    {
        // Arrange - 複数のマスク領域をサポート（Req 4.2）
        var masks = new[]
        {
            new MaskRegion(50, 100, 200, 50),
            new MaskRegion(300, 150, 100, 30),
            new MaskRegion(500, 200, 150, 40)
        };

        // Act & Assert
        Assert.Equal(3, masks.Length);
        Assert.All(masks, mask =>
        {
            Assert.True(mask.Width > 0);
            Assert.True(mask.Height > 0);
        });
    }
}
