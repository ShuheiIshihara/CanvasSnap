using System.Threading.Tasks;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// 設定ファイルの読み書きを管理するサービス
/// Requirements: 7.1, 7.2, 7.3, 7.4
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 設定ファイルを読み込む
    /// Requirements: 7.1 (アプリケーション起動時に設定ファイルを自動的に読み込む)
    /// </summary>
    /// <returns>設定オブジェクト、読み込み失敗時はデフォルト設定</returns>
    Task<CaptureSettings> LoadSettingsAsync();

    /// <summary>
    /// 設定をファイルに保存
    /// Requirements: 7.2 (ユーザーが設定を保存した時にJSON形式で設定ファイルに書き込む)
    /// </summary>
    /// <param name="settings">保存する設定</param>
    Task SaveSettingsAsync(CaptureSettings settings);

    /// <summary>
    /// デフォルト設定を取得
    /// </summary>
    /// <returns>デフォルトの設定オブジェクト</returns>
    CaptureSettings GetDefaultSettings();

    /// <summary>
    /// 設定ファイルパスを取得
    /// Requirements: 7.3 (macOS: ~/Library/Application Support/CanvasSnap/config.json)
    /// Requirements: 7.4 (Windows: %AppData%\CanvasSnap\config.json)
    /// </summary>
    /// <returns>プラットフォーム固有の設定ファイルパス</returns>
    string GetConfigFilePath();
}
