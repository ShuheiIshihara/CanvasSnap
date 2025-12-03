namespace CanvasSnap.Models;

/// <summary>
/// ディスプレイ情報を表す
/// HiDPI/Retina対応のためのスケールファクタやディスプレイ配置情報を含む
/// Requirements: 2.7 (ディスプレイ一覧取得), 3.1 (座標系とディスプレイ境界の把握)
/// </summary>
/// <param name="Id">ディスプレイ識別子（プラットフォーム固有）</param>
/// <param name="Name">ディスプレイ名</param>
/// <param name="X">X座標（プライマリディスプレイ左上を原点とした物理ピクセル）</param>
/// <param name="Y">Y座標（プライマリディスプレイ左上を原点とした物理ピクセル）</param>
/// <param name="Width">幅（物理ピクセル）</param>
/// <param name="Height">高さ（物理ピクセル）</param>
/// <param name="ScaleFactor">スケールファクタ（Retina: 2.0, 通常: 1.0）</param>
/// <param name="IsPrimary">プライマリディスプレイかどうか</param>
public record DisplayInfo(
    string Id,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    double ScaleFactor,
    bool IsPrimary
);
