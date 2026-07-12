using System;
using System.IO;

namespace CanvasSnap.Helpers;

/// <summary>
/// ログファイルの出力先パスを解決する
/// Requirements: 11.3 (ログ出力とデバッグ支援)
/// </summary>
/// <remarks>
/// - macOS: ~/Library/Logs/CanvasSnap/app.log
/// - Windows: %LocalAppData%\CanvasSnap\Logs\app.log
/// Serilog の RollingInterval.Day により、実際のファイル名には日付が付与される（例: app20260712.log）
/// </remarks>
public static class LogPathProvider
{
    /// <summary>
    /// ログディレクトリの絶対パスを取得する
    /// </summary>
    public static string GetLogDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Logs", "CanvasSnap");
        }

        // Windows / その他: LocalApplicationData 配下
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "CanvasSnap", "Logs");
    }

    /// <summary>
    /// ログファイルの絶対パス（app.log）を取得する
    /// </summary>
    public static string GetLogFilePath()
    {
        return Path.Combine(GetLogDirectory(), "app.log");
    }
}
