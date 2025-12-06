using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// ディスプレイ情報取得、座標変換、HiDPI対応を提供するサービスインターフェース
/// プラットフォーム固有実装（macOS: NSScreen API, Windows: Windows.Graphics.Display）を抽象化
/// Requirements: 2.6-2.9 (ディスプレイ情報管理), 3.1-3.4 (HiDPI対応), 13.4 (プラットフォーム抽象化)
/// </summary>
public interface IDisplayService
{
    /// <summary>
    /// 接続されているすべてのディスプレイ情報を取得
    /// Requirements: 2.6 (マルチディスプレイ環境での範囲選択)
    /// </summary>
    /// <returns>ディスプレイ情報のコレクション</returns>
    Task<IEnumerable<DisplayInfo>> GetAllDisplaysAsync();

    /// <summary>
    /// 指定されたキャプチャ領域が含まれるディスプレイ情報を取得
    /// Requirements: 2.9 (ディスプレイ可用性確認)
    /// </summary>
    /// <param name="region">キャプチャ領域（物理ピクセル座標）</param>
    /// <returns>
    /// ディスプレイが利用可能な場合はDisplayInfo、
    /// ディスプレイが切断されている等で利用不可の場合はnull
    /// </returns>
    Task<DisplayInfo?> GetDisplayInfoAsync(CaptureRegion region);

    /// <summary>
    /// 論理座標を物理座標に変換
    /// Requirements: 3.4 (座標を保存する際に実画素座標に変換)
    /// </summary>
    /// <param name="logical">論理座標（UI表示用）</param>
    /// <param name="scaleFactor">スケールファクタ（Retina: 2.0, 非Retina: 1.0）</param>
    /// <returns>物理座標（実画素ベース）</returns>
    PhysicalCoordinates LogicalToPhysical(LogicalCoordinates logical, double scaleFactor);

    /// <summary>
    /// 物理座標を論理座標に変換
    /// Requirements: 3.3 (設定UIで論理座標を表示)
    /// </summary>
    /// <param name="physical">物理座標（実画素ベース）</param>
    /// <param name="scaleFactor">スケールファクタ（Retina: 2.0, 非Retina: 1.0）</param>
    /// <returns>論理座標（UI表示用）</returns>
    LogicalCoordinates PhysicalToLogical(PhysicalCoordinates physical, double scaleFactor);

    /// <summary>
    /// ディスプレイ構成変更イベント
    /// ディスプレイの接続/切断、解像度変更、配置変更時に発火
    /// Requirements: 2.8 (ディスプレイ構成変更時の通知)
    /// </summary>
    event EventHandler DisplayConfigurationChanged;
}
