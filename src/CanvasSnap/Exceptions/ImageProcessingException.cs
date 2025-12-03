using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// 画像処理（マスク適用など）で発生する例外
/// ImageSharpによる画像操作のエラーを表す
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class ImageProcessingException : CanvasSnapException
{
    /// <summary>
    /// メッセージを指定して画像処理例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    public ImageProcessingException(string message) : base(message)
    {
    }

    /// <summary>
    /// メッセージと内部例外を指定して画像処理例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public ImageProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
