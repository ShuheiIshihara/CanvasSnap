namespace CanvasSnap.Models;

/// <summary>
/// 物理ピクセル座標を表す（実画素ベース）
/// HiDPI/Retinaディスプレイでの実際のピクセル位置を表現
/// Requirements: 3.4 (座標変換用型の定義)
/// </summary>
/// <param name="X">X座標（物理ピクセル）</param>
/// <param name="Y">Y座標（物理ピクセル）</param>
public record PhysicalCoordinates(int X, int Y);

/// <summary>
/// 論理ピクセル座標を表す（UI表示用）
/// OSやUIフレームワークが扱う論理的なピクセル位置を表現
/// Requirements: 3.3 (設定UIでの座標表示)
/// </summary>
/// <param name="X">X座標（論理ピクセル）</param>
/// <param name="Y">Y座標（論理ピクセル）</param>
public record LogicalCoordinates(int X, int Y);
