using System;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// ホットキーサービスインターフェース
/// グローバルホットキーの登録・解除とホットキー押下イベントの通知を抽象化
/// Requirements: 5.1 (ホットキー押下で即座にキャプチャ処理開始)
/// Requirements: 5.2 (デフォルトホットキー設定)
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせ)
/// Requirements: 5.4 (OS予約ショートカットと競合時は登録拒否)
/// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// ホットキーが押下されたときに発火するイベント
    /// Requirements: 5.1 (ホットキー押下で即座にキャプチャ処理開始)
    /// </summary>
    event EventHandler? HotkeyPressed;

    /// <summary>
    /// グローバルホットキーを登録
    /// Requirements: 5.2 (デフォルトホットキー設定)
    /// Requirements: 5.3 (修飾キーと任意のキーの組み合わせ)
    /// </summary>
    /// <param name="config">ホットキー設定（修飾キーとキーコード）</param>
    /// <returns>非同期タスク</returns>
    /// <exception cref="HotkeyConflictException">
    /// ホットキーがOS予約ショートカットまたは他のアプリケーションと競合している場合
    /// Requirements: 5.4 (OS予約ショートカットと競合時は登録拒否)
    /// </exception>
    /// <exception cref="HotkeyRegistrationException">
    /// ホットキー登録が失敗した場合（システムリソース不足、権限不足等）
    /// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
    /// </exception>
    /// <remarks>
    /// - macOS: CGEvent APIを使用してグローバルホットキーを登録
    /// - Windows: RegisterHotKey APIを使用（Phase 2予定）
    /// - 既にホットキーが登録されている場合は、古い登録を解除してから新しいホットキーを登録
    /// - IMEがオンの状態でもホットキーに反応する（Req 5.5）
    /// </remarks>
    Task RegisterHotkeyAsync(HotkeyConfig config);

    /// <summary>
    /// グローバルホットキーを解除
    /// </summary>
    /// <returns>非同期タスク</returns>
    /// <remarks>
    /// - ホットキーが登録されていない場合でも例外をスローしない
    /// - リソースのクリーンアップを確実に実行（イベントタップ停止、スレッド終了等）
    /// </remarks>
    Task UnregisterHotkeyAsync();
}
