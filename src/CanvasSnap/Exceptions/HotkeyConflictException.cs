using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// ホットキーがOS予約ショートカットまたは他のアプリケーションと競合している場合にスローされる例外
/// Requirements: 5.4 (OS予約ショートカットと競合時は登録を拒否)
/// </summary>
public class HotkeyConflictException : CanvasSnapException
{
    /// <summary>
    /// 競合したホットキー設定を取得
    /// </summary>
    public Models.HotkeyConfig ConflictingHotkey { get; }

    /// <summary>
    /// ホットキー競合例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="conflictingHotkey">競合したホットキー設定</param>
    public HotkeyConflictException(string message, Models.HotkeyConfig conflictingHotkey)
        : base(message)
    {
        ConflictingHotkey = conflictingHotkey;
    }

    /// <summary>
    /// ホットキー競合例外を作成（内部例外付き）
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="conflictingHotkey">競合したホットキー設定</param>
    /// <param name="innerException">内部例外</param>
    public HotkeyConflictException(string message, Models.HotkeyConfig conflictingHotkey, Exception innerException)
        : base(message, innerException)
    {
        ConflictingHotkey = conflictingHotkey;
    }
}
