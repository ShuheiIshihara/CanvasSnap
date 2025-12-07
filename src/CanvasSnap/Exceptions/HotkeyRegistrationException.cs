using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// ホットキー登録が失敗した場合にスローされる例外
/// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
/// </summary>
public class HotkeyRegistrationException : CanvasSnapException
{
    /// <summary>
    /// ホットキー登録例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    public HotkeyRegistrationException(string message) : base(message)
    {
    }

    /// <summary>
    /// ホットキー登録例外を作成（内部例外付き）
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public HotkeyRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
