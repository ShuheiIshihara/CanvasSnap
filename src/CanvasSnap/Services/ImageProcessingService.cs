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
    public async Task<byte[]> ApplyMaskAsync(byte[] imageData, IEnumerable<MaskRegion> maskRegions)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(maskRegions);

        // メモリストリームから画像を読み込み
        using var inputStream = new MemoryStream(imageData);
        using var image = await Image.LoadAsync<Rgba32>(inputStream);

        // マスク領域を黒塗り
        image.Mutate(ctx =>
        {
            foreach (var mask in maskRegions)
            {
                var rect = new Rectangle(mask.X, mask.Y, mask.Width, mask.Height);
                ctx.Fill(Color.Black, rect);
            }
        });

        // PNG形式で出力
        using var outputStream = new MemoryStream();
        await image.SaveAsPngAsync(outputStream);
        return outputStream.ToArray();
    }
}
