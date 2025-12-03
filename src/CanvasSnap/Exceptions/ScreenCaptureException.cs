using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// スクリーンキャプチャ処理で発生する例外
/// screencaptureコマンドの失敗やAPI呼び出しエラーを表す
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class ScreenCaptureException : CanvasSnapException
{
    /// <summary>
    /// メッセージを指定してスクリーンキャプチャ例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    public ScreenCaptureException(string message) : base(message)
    {
    }

    /// <summary>
    /// メッセージと内部例外を指定してスクリーンキャプチャ例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public ScreenCaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
