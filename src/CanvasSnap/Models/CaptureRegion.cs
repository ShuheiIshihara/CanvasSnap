namespace CanvasSnap.Models;

/// <summary>
/// キャプチャする矩形領域を表す（物理ピクセル座標）
/// プライマリディスプレイ左上を原点(0,0)とする絶対座標で管理
/// Requirements: 2.4 (選択された領域の座標を実画素ベースで保存)
/// </summary>
/// <param name="X">X座標（プライマリディスプレイ左上からの物理ピクセル）</param>
/// <param name="Y">Y座標（プライマリディスプレイ左上からの物理ピクセル）</param>
/// <param name="Width">幅（物理ピクセル）</param>
/// <param name="Height">高さ（物理ピクセル）</param>
public record CaptureRegion(int X, int Y, int Width, int Height);
