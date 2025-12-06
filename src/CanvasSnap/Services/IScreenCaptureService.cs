using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// スクリーンキャプチャサービスインターフェース
/// OS固有の画面キャプチャAPIを抽象化し、プラットフォーム間で一貫したキャプチャ機能を提供
/// Requirements: 1.1 (OS標準APIによるキャプチャ), 1.7 (macOS screencaptureコマンド), 1.8 (Windows BitBlt/WGC)
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// 指定された矩形領域のスクリーンキャプチャを実行
    /// </summary>
    /// <param name="region">物理ピクセル座標の矩形領域（プライマリディスプレイ左上を原点とする）</param>
    /// <returns>PNG形式の画像データ（byte配列）</returns>
    /// <exception cref="ScreenCaptureException">キャプチャ処理が失敗した場合（screencaptureコマンド失敗、API呼び出しエラー等）</exception>
    /// <exception cref="PermissionDeniedException">Screen Recording権限が付与されていない場合</exception>
    /// <remarks>
    /// - macOS: screencaptureコマンドを使用（Phase 1 MVP）
    /// - Windows: BitBltまたはWindows.Graphics.Capture APIを使用（Phase 2予定）
    /// - 返却される画像データはPNG形式のバイナリ
    /// - パフォーマンス目標: 0.5秒以内（Req 11.1）
    /// </remarks>
    Task<byte[]> CaptureRegionAsync(CaptureRegion region);
}
