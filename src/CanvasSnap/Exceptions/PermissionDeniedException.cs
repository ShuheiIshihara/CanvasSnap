using System;

namespace CanvasSnap.Exceptions;

/// <summary>
/// 権限の種類を表す列挙型
/// Requirements: 12.1, 12.2 (権限管理)
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// スクリーンレコーディング権限（macOS Screen Recording）
    /// スクリーンキャプチャに必要
    /// </summary>
    ScreenRecording,

    /// <summary>
    /// アクセシビリティ権限（macOS Accessibility）
    /// グローバルホットキー登録に必要
    /// </summary>
    Accessibility
}

/// <summary>
/// 権限が拒否された場合に発生する例外
/// ユーザーにシステム設定を開くよう促す必要がある
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class PermissionDeniedException : CanvasSnapException
{
    /// <summary>
    /// 拒否された権限の種類
    /// </summary>
    public PermissionType PermissionType { get; }

    /// <summary>
    /// メッセージと権限種類を指定して権限拒否例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="permissionType">拒否された権限の種類</param>
    public PermissionDeniedException(string message, PermissionType permissionType) : base(message)
    {
        PermissionType = permissionType;
    }

    /// <summary>
    /// メッセージ、権限種類、内部例外を指定して権限拒否例外を作成
    /// </summary>
    /// <param name="message">例外メッセージ</param>
    /// <param name="permissionType">拒否された権限の種類</param>
    /// <param name="innerException">内部例外</param>
    public PermissionDeniedException(string message, PermissionType permissionType, Exception innerException)
        : base(message, innerException)
    {
        PermissionType = permissionType;
    }
}
