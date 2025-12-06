using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CanvasSnap.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace CanvasSnap.Services;

/// <summary>
/// 画像処理サービスの実装
/// ImageSharpを使用したマスク適用（黒塗り）とPNGバイナリ生成を提供
/// Requirements: 4.1-4.5
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    /// <summary>
    /// 画像にマスク領域を適用（黒塗り）
    /// Requirements: 4.3 (マスク機能が有効な場合、指定領域を黒で塗りつぶす)
    /// </summary>
    /// <param name="imageData">元画像データ（PNG形式）</param>
    /// <param name="maskRegions">マスク領域（相対座標）</param>
    /// <returns>マスク適用後のPNG画像データ（byte[]）</returns>
    /// <exception cref="CanvasSnap.Exceptions.ImageProcessingException">
    /// 画像データが無効、またはマスク領域が境界外の場合
    /// </exception>
    public async Task<byte[]> ApplyMaskAsync(byte[] imageData, IEnumerable<MaskRegion> maskRegions)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(maskRegions);

        // メモリストリームから画像を読み込み
        using var inputStream = new MemoryStream(imageData);
        Image<Rgba32> image;

        try
        {
            image = await Image.LoadAsync<Rgba32>(inputStream);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new Exceptions.ImageProcessingException(
                "画像データの読み込みに失敗しました。有効なPNG形式の画像データを指定してください。",
                ex);
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentNullException)
        {
            throw new Exceptions.ImageProcessingException(
                "画像データの読み込みに失敗しました。入力ストリームが無効です。",
                ex);
        }

        using (image)
        {
            // マスク領域の検証（Requirements: 4.2 境界チェックとサイズ検証）
            ValidateMaskRegions(maskRegions, image.Width, image.Height);

            // マスク領域を黒塗り（Requirements: 4.3 複数マスク領域の順次処理）
            image.Mutate(ctx =>
            {
                foreach (var mask in maskRegions)
                {
                    var rect = new Rectangle(mask.X, mask.Y, mask.Width, mask.Height);
                    ctx.Fill(Color.Black, rect); // #000000で塗りつぶし
                }
            });

            // PNG形式で出力（メモリ効率的な処理）
            using var outputStream = new MemoryStream();
            await image.SaveAsPngAsync(outputStream);
            return outputStream.ToArray();
        }
    }

    /// <summary>
    /// マスク領域が画像境界内にあるか検証する
    /// Requirements: 4.2 (マスク領域の検証とエラーハンドリング)
    /// </summary>
    /// <param name="maskRegions">検証するマスク領域</param>
    /// <param name="imageWidth">画像の幅</param>
    /// <param name="imageHeight">画像の高さ</param>
    /// <exception cref="CanvasSnap.Exceptions.ImageProcessingException">
    /// マスク領域が画像境界外または無効な値の場合
    /// </exception>
    private static void ValidateMaskRegions(IEnumerable<MaskRegion> maskRegions, int imageWidth, int imageHeight)
    {
        foreach (var mask in maskRegions)
        {
            // サイズ検証
            if (mask.Width <= 0)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域の幅が0以下です。Width={mask.Width}");
            }

            if (mask.Height <= 0)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域の高さが0以下です。Height={mask.Height}");
            }

            // 境界検証
            if (mask.X < 0)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域のX座標が境界外です。X={mask.X} (画像幅: {imageWidth})");
            }

            if (mask.Y < 0)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域のY座標が境界外です。Y={mask.Y} (画像高さ: {imageHeight})");
            }

            if (mask.X + mask.Width > imageWidth)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域が境界外です（画像の右端を超えています）。X={mask.X}, Width={mask.Width}, 画像幅={imageWidth}");
            }

            if (mask.Y + mask.Height > imageHeight)
            {
                throw new Exceptions.ImageProcessingException(
                    $"マスク領域が境界外です（画像の下端を超えています）。Y={mask.Y}, Height={mask.Height}, 画像高さ={imageHeight}");
            }
        }
    }
}
