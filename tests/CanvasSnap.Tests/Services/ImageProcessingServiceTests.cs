using CanvasSnap.Models;
using CanvasSnap.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// IImageProcessingServiceの単体テスト
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5
/// </summary>
public class ImageProcessingServiceTests
{
    private readonly ImageProcessingService _service;

    public ImageProcessingServiceTests()
    {
        _service = new ImageProcessingService();
    }

    /// <summary>
    /// テスト用のPNG画像データを生成
    /// </summary>
    private static async Task<byte[]> CreateTestImageAsync(int width, int height, Rgba32 fillColor)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.BackgroundColor(fillColor));

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 単一マスク領域を適用し、その領域が黒塗りされることを検証
    /// Requirements: 4.3 (マスク機能が有効な場合、指定領域を黒で塗りつぶす)
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_SingleMask_FillsRegionWithBlack()
    {
        // Arrange
        var imageData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255)); // 白い画像
        var masks = new[] { new MaskRegion(10, 10, 20, 20) };

        // Act
        var result = await _service.ApplyMaskAsync(imageData, masks);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // 結果画像を検証
        using var resultImage = Image.Load<Rgba32>(result);

        // マスク領域の中心が黒であることを確認
        var centerPixel = resultImage[20, 20];
        Assert.Equal(0, centerPixel.R);
        Assert.Equal(0, centerPixel.G);
        Assert.Equal(0, centerPixel.B);

        // マスク領域外が白のままであることを確認
        var outsidePixel = resultImage[5, 5];
        Assert.Equal(255, outsidePixel.R);
        Assert.Equal(255, outsidePixel.G);
        Assert.Equal(255, outsidePixel.B);
    }

    /// <summary>
    /// 複数マスク領域を適用し、すべての領域が黒塗りされることを検証
    /// Requirements: 4.3 (複数マスク領域の順次処理)
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_MultipleMasks_FillsAllRegionsWithBlack()
    {
        // Arrange
        var imageData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255));
        var masks = new[]
        {
            new MaskRegion(10, 10, 20, 20),
            new MaskRegion(50, 50, 30, 30)
        };

        // Act
        var result = await _service.ApplyMaskAsync(imageData, masks);

        // Assert
        using var resultImage = Image.Load<Rgba32>(result);

        // 最初のマスク領域が黒
        var firstMaskPixel = resultImage[20, 20];
        Assert.Equal(0, firstMaskPixel.R);
        Assert.Equal(0, firstMaskPixel.G);
        Assert.Equal(0, firstMaskPixel.B);

        // 2番目のマスク領域が黒
        var secondMaskPixel = resultImage[60, 60];
        Assert.Equal(0, secondMaskPixel.R);
        Assert.Equal(0, secondMaskPixel.G);
        Assert.Equal(0, secondMaskPixel.B);
    }

    /// <summary>
    /// 空のマスクリストの場合、元画像がそのまま返される
    /// Requirements: 4.5 (マスク機能が無効な場合、マスク処理を行わずキャプチャ)
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_EmptyMasks_ReturnsImageUnchanged()
    {
        // Arrange
        var imageData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255));
        var masks = Array.Empty<MaskRegion>();

        // Act
        var result = await _service.ApplyMaskAsync(imageData, masks);

        // Assert
        using var resultImage = Image.Load<Rgba32>(result);

        // すべてのピクセルが白のまま
        var pixel = resultImage[50, 50];
        Assert.Equal(255, pixel.R);
        Assert.Equal(255, pixel.G);
        Assert.Equal(255, pixel.B);
    }

    /// <summary>
    /// 元画像データは変更されないことを検証
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_DoesNotModifyOriginalData()
    {
        // Arrange
        var originalData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255));
        var originalDataCopy = originalData.ToArray();
        var masks = new[] { new MaskRegion(10, 10, 20, 20) };

        // Act
        await _service.ApplyMaskAsync(originalData, masks);

        // Assert - 元データが変更されていないこと
        Assert.Equal(originalDataCopy, originalData);
    }

    /// <summary>
    /// PNG形式で出力されることを検証
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_ReturnsPngFormat()
    {
        // Arrange
        var imageData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255));
        var masks = new[] { new MaskRegion(10, 10, 20, 20) };

        // Act
        var result = await _service.ApplyMaskAsync(imageData, masks);

        // Assert - PNG magic number (89 50 4E 47)
        Assert.True(result.Length >= 8);
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x4E, result[2]); // N
        Assert.Equal(0x47, result[3]); // G
    }

    /// <summary>
    /// マスク領域が画像サイズ全体をカバーする場合も正常に処理される
    /// </summary>
    [Fact]
    public async Task ApplyMaskAsync_FullImageMask_FillsEntireImageWithBlack()
    {
        // Arrange
        var imageData = await CreateTestImageAsync(100, 100, new Rgba32(255, 255, 255));
        var masks = new[] { new MaskRegion(0, 0, 100, 100) };

        // Act
        var result = await _service.ApplyMaskAsync(imageData, masks);

        // Assert
        using var resultImage = Image.Load<Rgba32>(result);
        var pixel = resultImage[50, 50];
        Assert.Equal(0, pixel.R);
        Assert.Equal(0, pixel.G);
        Assert.Equal(0, pixel.B);
    }
}
