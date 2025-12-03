using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// 設定ファイルの読み書きで発生する例外
/// JSONパースエラーやファイルI/Oエラーを表す
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class SettingsException : CanvasSnapException
{
    /// <summary>
    /// メッセージを指定して設定例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    public SettingsException(string message) : base(message)
    {
    }

    /// <summary>
    /// メッセージと内部例外を指定して設定例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public SettingsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
