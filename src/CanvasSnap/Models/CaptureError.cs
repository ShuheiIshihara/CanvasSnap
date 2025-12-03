namespace CanvasSnap.Models;

/// <summary>
/// キャプチャ処理で発生しうるエラーの種類を表す
/// Result&lt;T, E&gt;型のエラー値として使用される
/// Requirements: 2.7 (エラーハンドリング), 3.1 (エラー状態の区別)
/// </summary>
public enum CaptureError
{
    /// <summary>
    /// スクリーンレコーディングまたはアクセシビリティ権限が拒否された
    /// ユーザーにシステム設定を開くよう促す必要がある
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// 指定されたディスプレイが利用できない
    /// ディスプレイ切断やディスプレイ構成変更時に発生
    /// </summary>
    DisplayUnavailable,

    /// <summary>
    /// スクリーンキャプチャ処理自体が失敗した
    /// screencaptureコマンドのエラーやAPI呼び出しの失敗
    /// </summary>
    CaptureFailed,

    /// <summary>
    /// ファイル保存に失敗した
    /// ディスク容量不足、書き込み権限なし、パスが無効など
    /// </summary>
    SaveFailed,

    /// <summary>
    /// その他の不明なエラー
    /// 予期しない例外が発生した場合
    /// </summary>
    Unknown
}
