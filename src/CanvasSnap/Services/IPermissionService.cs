using System.Threading.Tasks;
using CanvasSnap.Exceptions;

namespace CanvasSnap.Services;

/// <summary>
/// 権限管理サービスインターフェース
/// macOS環境でのScreen Recording権限とアクセシビリティ権限の管理を抽象化
/// Requirements: 12.1 (権限チェック), 12.2 (権限設定画面の表示)
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// すべての必要な権限がGrantedされているかチェック
    /// Requirements: 12.1 (画面録画権限とアクセシビリティ権限の両方をチェック)
    /// </summary>
    /// <returns>すべての権限がGrantedされている場合true、いずれか不足している場合false</returns>
    Task<bool> CheckPermissionsAsync();

    /// <summary>
    /// Screen Recording権限がGrantedされているかチェック
    /// Requirements: 12.1 (macOS Screen Recording権限チェック)
    /// </summary>
    /// <returns>Screen Recording権限がGrantedされている場合true、それ以外false</returns>
    Task<bool> CheckScreenRecordingPermissionAsync();

    /// <summary>
    /// アクセシビリティ権限がGrantedされているかチェック
    /// Requirements: 12.1 (macOS Accessibility権限チェック)
    /// </summary>
    /// <remarks>
    /// ホットキー機能（CGEvent API）のために必要
    /// </remarks>
    /// <returns>アクセシビリティ権限がGrantedされている場合true、それ以外false</returns>
    Task<bool> CheckAccessibilityPermissionAsync();

    /// <summary>
    /// システム設定の権限設定画面を開く
    /// Requirements: 12.2 (権限設定画面を開くよう案内)
    /// </summary>
    /// <param name="permissionType">開く権限設定の種類</param>
    /// <returns>非同期タスク</returns>
    Task OpenPermissionSettingsAsync(PermissionType permissionType);
}
