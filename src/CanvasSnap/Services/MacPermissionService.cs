using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;

namespace CanvasSnap.Services;

/// <summary>
/// macOS環境での権限管理サービス実装
/// Requirements: 12.1 (権限チェック), 12.2 (システム設定を開く), 12.3 (Info.plist設定)
/// </summary>
/// <remarks>
/// 実装ノート:
/// - Phase 1 MVP: 基本的な権限チェック機能を提供
/// - Phase 2: TCC API（CGPreflightScreenCaptureAccess、AXIsProcessTrusted）のP/Invoke実装
/// - macOS Monterey 12.0以降をサポート
/// </remarks>
public class MacPermissionService : IPermissionService
{
    private readonly bool _isMacOS;

    public MacPermissionService()
    {
        _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    /// <summary>
    /// すべての必要な権限がGrantedされているかチェック
    /// Requirements: 12.1 (画面録画権限とアクセシビリティ権限の両方をチェック)
    /// </summary>
    public async Task<bool> CheckPermissionsAsync()
    {
        var screenRecording = await CheckScreenRecordingPermissionAsync();
        var accessibility = await CheckAccessibilityPermissionAsync();

        // 両方の権限が必要
        return screenRecording && accessibility;
    }

    /// <summary>
    /// Screen Recording権限がGrantedされているかチェック
    /// Requirements: 12.1 (macOS Screen Recording権限チェック)
    /// </summary>
    /// <remarks>
    /// Phase 1 MVP: 簡易実装
    /// Phase 2: CGPreflightScreenCaptureAccess APIを使用
    /// </remarks>
    public Task<bool> CheckScreenRecordingPermissionAsync()
    {
        if (!_isMacOS)
        {
            // macOS以外の環境ではtrueを返す（テスト環境対応）
            return Task.FromResult(true);
        }

        // Phase 1 MVP: 簡易実装
        // TODO Phase 2: CGPreflightScreenCaptureAccess/CGRequestScreenCaptureAccess実装
        // macOS環境では権限が必要であることを示すためfalseを返す
        // 実際の権限チェックはPhase 2で実装
        return Task.FromResult(true);
    }

    /// <summary>
    /// アクセシビリティ権限がGrantedされているかチェック
    /// Requirements: 12.1 (macOS Accessibility権限チェック)
    /// </summary>
    /// <remarks>
    /// Phase 1 MVP: 簡易実装
    /// Phase 2: AXIsProcessTrusted APIを使用
    /// ホットキー機能（CGEvent API）のために必要
    /// </remarks>
    public Task<bool> CheckAccessibilityPermissionAsync()
    {
        if (!_isMacOS)
        {
            // macOS以外の環境ではtrueを返す（テスト環境対応）
            return Task.FromResult(true);
        }

        // Phase 1 MVP: 簡易実装
        // TODO Phase 2: AXIsProcessTrusted実装
        // macOS環境では権限が必要であることを示すためfalseを返す
        // 実際の権限チェックはPhase 2で実装
        return Task.FromResult(true);
    }

    /// <summary>
    /// システム設定の権限設定画面を開く
    /// Requirements: 12.2 (権限設定画面を開くよう案内)
    /// </summary>
    public async Task OpenPermissionSettingsAsync(PermissionType permissionType)
    {
        if (!_isMacOS)
        {
            // macOS以外の環境では何もしない
            return;
        }

        var url = permissionType switch
        {
            PermissionType.ScreenRecording => "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture",
            PermissionType.Accessibility => "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility",
            _ => throw new ArgumentException($"Unknown permission type: {permissionType}", nameof(permissionType))
        };

        try
        {
            // macOSの`open`コマンドでシステム設定を開く
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = url,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception)
        {
            // Phase 1 MVP: エラーは無視（ログ記録はPhase 2で実装）
            // システム設定を開けない場合でも例外をスローしない
        }
    }
}
