using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// CanvasSnapアプリケーション固有の例外の基底クラス
/// すべてのカスタム例外はこのクラスを継承する
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class CanvasSnapException : Exception
{
    /// <summary>
    /// メッセージを指定して例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    public CanvasSnapException(string message) : base(message)
    {
    }

    /// <summary>
    /// メッセージと内部例外を指定して例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public CanvasSnapException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
