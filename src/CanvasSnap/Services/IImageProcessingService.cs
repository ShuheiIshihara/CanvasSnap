using System.Collections.Generic;
using System.Threading.Tasks;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// 画像処理サービスのインターフェース
/// マスク適用（黒塗り）とPNGバイナリ生成を提供
/// Requirements: 4.1-4.5
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// 画像にマスク領域を適用（黒塗り）
    /// Requirements: 4.3 (マスク機能が有効な場合、指定領域を黒で塗りつぶす)
    /// Requirements: 4.4 (マスク機能の有効/無効をチェックボックスで切り替え可能)
    /// Requirements: 4.5 (マスク機能が無効な場合、マスク処理を行わずキャプチャ)
    /// </summary>
    /// <param name="imageData">元画像データ（PNG形式）</param>
    /// <param name="maskRegions">マスク領域（相対座標）</param>
    /// <returns>マスク適用後のPNG画像データ（byte[]）</returns>
    Task<byte[]> ApplyMaskAsync(byte[] imageData, IEnumerable<MaskRegion> maskRegions);
}
