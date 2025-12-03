using System;

namespace CanvasSnap.Models;

/// <summary>
/// キャプチャ設定の集約ルート
/// すべてのキャプチャ関連設定を保持し、JSON形式でシリアライズ可能
/// Requirements: 7.2 (設定をJSON形式で保存)
/// </summary>
public record CaptureSettings
{
    /// <summary>
    /// キャプチャする矩形領域（物理ピクセル座標）
    /// </summary>
    public required CaptureRegion Region { get; init; }

    /// <summary>
    /// マスク（黒塗り）する領域のリスト（相対座標）
    /// デフォルトは空配列
    /// </summary>
    public MaskRegion[] MaskRegions { get; init; } = Array.Empty<MaskRegion>();

    /// <summary>
    /// ホットキー設定（修飾キー + キーコード）
    /// </summary>
    public required HotkeyConfig HotkeyConfig { get; init; }

    /// <summary>
    /// 保存先ディレクトリパス
    /// Requirements: 6.4 (ユーザーが設定で指定したフォルダに保存)
    /// </summary>
    public required string SaveDirectory { get; init; }

    /// <summary>
    /// マスク機能の有効/無効
    /// Requirements: 4.4 (マスク機能の有効/無効をチェックボックスで切り替え)
    /// </summary>
    public bool IsMaskEnabled { get; init; }
}
