using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CanvasSnap.Services;

/// <summary>
/// macOS環境での通知サービス実装
/// Requirements: 10.1 (キャプチャ成功時のOS標準通知)
/// Requirements: 10.2 (軽微なエラーのOS標準通知)
/// Requirements: 10.3 (クリティカルエラーのダイアログ表示)
/// Requirements: 10.4 (権限エラー時のシステム設定誘導ボタン)
/// </summary>
/// <remarks>
/// 実装ノート:
/// - Phase 1 MVP: osascript コマンドでAppleScriptを実行して通知・ダイアログを表示
/// - ShowNotificationAsync: `display notification` コマンドでOS標準通知を表示
/// - ShowCriticalErrorAsync: `display dialog` コマンドでダイアログを表示
/// - Phase 2検討: UNUserNotificationCenterとNSAlertのP/Invoke実装
/// </remarks>
public class MacNotificationService : INotificationService
{
    private readonly bool _isMacOS;
    private readonly IAppleScriptExecutor _appleScriptExecutor;

    public MacNotificationService()
        : this(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), new AppleScriptExecutor())
    {
    }

    internal MacNotificationService(bool isMacOS, IAppleScriptExecutor appleScriptExecutor)
    {
        _isMacOS = isMacOS;
        _appleScriptExecutor = appleScriptExecutor ?? throw new ArgumentNullException(nameof(appleScriptExecutor));
    }

    /// <summary>
    /// OS標準通知を表示
    /// </summary>
    public async Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info)
    {
        if (!_isMacOS)
        {
            // 非macOS環境ではダミー実装（テスト用）
            return;
        }

        // AppleScriptで通知を表示
        var script = BuildNotificationScript(message, type);
        await ExecuteSafelyAsync(script);
    }

    /// <summary>
    /// クリティカルエラーダイアログを表示（ブロッキング）
    /// </summary>
    public async Task ShowCriticalErrorAsync(string title, string message, string[]? actionButtons = null)
    {
        if (!_isMacOS)
        {
            // 非macOS環境ではダミー実装（テスト用）
            return;
        }

        // デフォルトボタンは「閉じる」
        var script = BuildDialogScript(title, message, actionButtons);
        await ExecuteSafelyAsync(script);
    }

    /// <summary>
    /// 通知タイプに応じたサブタイトルを取得
    /// </summary>
    private static string GetNotificationSubtitle(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => "成功",
            NotificationType.Warning => "警告",
            NotificationType.Error => "エラー",
            NotificationType.Info => "情報",
            _ => "情報"
        };
    }

    internal static string BuildNotificationScript(string message, NotificationType type)
    {
        var title = "CanvasSnap";
        var subtitle = GetNotificationSubtitle(type);
        return $"display notification \"{EscapeAppleScript(message)}\" with title \"{EscapeAppleScript(title)}\" subtitle \"{EscapeAppleScript(subtitle)}\"";
    }

    internal static string BuildDialogScript(string title, string message, string[]? actionButtons)
    {
        var buttons = actionButtons is { Length: > 0 } ? actionButtons : new[] { "閉じる" };
        var escapedButtons = Array.ConvertAll(buttons, EscapeAppleScript);
        var buttonList = string.Join(", ", Array.ConvertAll(escapedButtons, b => $"\"{b}\""));
        return $"display dialog \"{EscapeAppleScript(message)}\" with title \"{EscapeAppleScript(title)}\" buttons {{{buttonList}}} default button 1";
    }

    /// <summary>
    /// AppleScriptの特殊文字をエスケープ
    /// </summary>
    private static string EscapeAppleScript(string text)
    {
        // ダブルクォートとバックスラッシュをエスケープ
        return text
            .Replace("\\", "\\\\")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"")
            .Replace("\u0000", string.Empty);
    }

    /// <summary>
    /// AppleScriptを実行（失敗時は例外を握りつぶす）
    /// </summary>
    private async Task ExecuteSafelyAsync(string script)
    {
        try
        {
            await _appleScriptExecutor.ExecuteAsync(script);
        }
        catch (Exception)
        {
            // osascript実行失敗時は例外をスローせず、ログ記録のみ（Phase 2で実装）
            // 通知表示失敗はクリティカルではない
        }
    }

    /// <summary>
    /// シェル引数をエスケープ（osascript -e 用）
    /// </summary>
    internal static string EscapeShellArgument(string argument)
    {
        // ダブルクォート内の特殊文字をエスケープ
        return argument
            .Replace("\\", "\\\\")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`")
            .Replace("!", "\\!");
    }

    internal interface IAppleScriptExecutor
    {
        Task ExecuteAsync(string script);
    }

    internal class AppleScriptExecutor : IAppleScriptExecutor
    {
        public async Task ExecuteAsync(string script)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{EscapeShellArgument(script)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start osascript process");
            }

            var readOutTask = process.StandardOutput.ReadToEndAsync();
            var readErrTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(readOutTask, readErrTask, process.WaitForExitAsync());

            if (process.ExitCode != 0)
            {
                _ = readErrTask.Result;
            }
        }
    }
}
