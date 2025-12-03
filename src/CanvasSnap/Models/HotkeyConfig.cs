using System;

namespace CanvasSnap.Models;

/// <summary>
/// ホットキー設定を表す
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせを受け付ける)
/// </summary>
/// <param name="Modifiers">修飾キーの組み合わせ（Control, Shift, Alt）</param>
/// <param name="KeyCode">キーコード（プラットフォーム固有のキーコード）</param>
public record HotkeyConfig(HotkeyModifiers Modifiers, int KeyCode);

/// <summary>
/// ホットキーの修飾キーを表すフラグ列挙型
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせ)
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>
    /// 修飾キーなし
    /// </summary>
    None = 0,

    /// <summary>
    /// Controlキー（macOSではCommandキー、WindowsではCtrlキー）
    /// </summary>
    Control = 1 << 0,

    /// <summary>
    /// Shiftキー
    /// </summary>
    Shift = 1 << 1,

    /// <summary>
    /// Altキー（macOSではOptionキー、WindowsではAltキー）
    /// </summary>
    Alt = 1 << 2
}
