namespace CanvasSnap.Models;

/// <summary>
/// マスク（黒塗り）する矩形領域を表す（相対座標）
/// キャプチャ領域の左上を原点(0,0)とする相対座標で管理
/// Requirements: 4.2 (マスク座標をキャプチャ領域左上を原点とする相対座標で保存)
/// </summary>
/// <param name="X">X座標（キャプチャ領域左上からの相対位置）</param>
/// <param name="Y">Y座標（キャプチャ領域左上からの相対位置）</param>
/// <param name="Width">幅（ピクセル）</param>
/// <param name="Height">高さ（ピクセル）</param>
public record MaskRegion(int X, int Y, int Width, int Height);
