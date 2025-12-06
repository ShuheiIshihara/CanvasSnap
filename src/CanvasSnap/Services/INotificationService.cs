using System.Threading.Tasks;

namespace CanvasSnap.Services;

/// <summary>
/// 通知タイプ
/// Requirements: 10.1, 10.2, 10.3, 10.4
/// </summary>
public enum NotificationType
{
    /// <summary>情報通知</summary>
    Info,
    /// <summary>成功通知</summary>
    Success,
    /// <summary>警告通知</summary>
    Warning,
    /// <summary>エラー通知</summary>
    Error
}

/// <summary>
/// 通知サービスのインターフェース
/// OS標準通知およびダイアログ表示を抽象化
/// Requirements: 10.1, 10.2, 10.3, 10.4
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// OS標準通知を表示（軽微なエラー、成功メッセージ）
    /// Requirements: 10.1 (キャプチャ成功時にOS標準通知で成功メッセージを表示)
    /// Requirements: 10.2 (軽微なエラー発生時にOS標準通知でエラー内容を表示)
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="type">通知タイプ（デフォルト: Info）</param>
    Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info);

    /// <summary>
    /// クリティカルエラーダイアログを表示（ブロッキング）
    /// Requirements: 10.3 (クリティカルなエラー発生時にダイアログで明示し対処方法を案内)
    /// Requirements: 10.4 (画面キャプチャ権限がない場合、システム設定を開くボタンを表示)
    /// </summary>
    /// <param name="title">ダイアログのタイトル</param>
    /// <param name="message">ダイアログの本文</param>
    /// <param name="actionButtons">アクションボタン（オプション）</param>
    Task ShowCriticalErrorAsync(string title, string message, string[]? actionButtons = null);
}
