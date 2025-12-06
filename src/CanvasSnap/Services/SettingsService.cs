using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// 設定ファイルの読み書きを実装するサービス
/// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string? _customConfigDirectory;
    private readonly INotificationService? _notificationService;

    /// <summary>
    /// デフォルトコンストラクタ（通常使用）
    /// </summary>
    public SettingsService()
    {
    }

    /// <summary>
    /// テスト用コンストラクタ（カスタムディレクトリを指定）
    /// </summary>
    /// <param name="customConfigDirectory">カスタム設定ディレクトリ（テスト用）</param>
    public SettingsService(string customConfigDirectory)
    {
        _customConfigDirectory = customConfigDirectory;
    }

    /// <summary>
    /// テスト用コンストラクタ（カスタムディレクトリと通知サービスを指定）
    /// </summary>
    /// <param name="customConfigDirectory">カスタム設定ディレクトリ（テスト用）</param>
    /// <param name="notificationService">通知サービス（破損検出時の通知用）</param>
    public SettingsService(string customConfigDirectory, INotificationService notificationService)
    {
        _customConfigDirectory = customConfigDirectory;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 本番用コンストラクタ（通知サービスを指定）
    /// </summary>
    /// <param name="notificationService">通知サービス（破損検出時の通知用）</param>
    public SettingsService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// 設定ファイルを読み込む
    /// Requirements: 7.1 (アプリケーション起動時に設定ファイルを自動的に読み込む)
    /// Requirements: 7.5 (設定ファイルのJSONパースに失敗した場合はデフォルト設定で起動)
    /// Requirements: 7.7 (設定ファイル破損により復元した場合、ユーザーに通知し設定の再構成を促す)
    /// </summary>
    public async Task<CaptureSettings> LoadSettingsAsync()
    {
        var configPath = GetConfigFilePath();

        if (!File.Exists(configPath))
        {
            return GetDefaultSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var settings = JsonSerializer.Deserialize<CaptureSettings>(json);

            if (settings == null)
            {
                return GetDefaultSettings();
            }

            return settings;
        }
        catch (JsonException)
        {
            // Requirements: 7.6 (破損ファイルを.backup拡張子でリネームし保持)
            var backupPath = configPath + ".backup";
            File.Move(configPath, backupPath, overwrite: true);

            // Requirements: 7.7 (設定ファイル破損により復元した場合、ユーザーに通知し設定の再構成を促す)
            await NotifySettingsCorruptionAsync();

            // Requirements: 7.5 (デフォルト設定で起動)
            return GetDefaultSettings();
        }
        catch (Exception)
        {
            // その他のエラー（IOエラーなど）もデフォルト設定で継続
            return GetDefaultSettings();
        }
    }

    /// <summary>
    /// 設定ファイル破損時にユーザーに通知を発行
    /// Requirements: 7.7 (設定ファイル破損により復元した場合、ユーザーに通知し設定の再構成を促す)
    /// </summary>
    private async Task NotifySettingsCorruptionAsync()
    {
        if (_notificationService != null)
        {
            await _notificationService.ShowNotificationAsync(
                "設定ファイルが破損していたため、デフォルト設定で起動しました。設定を再構成してください。",
                NotificationType.Warning);
        }
    }

    /// <summary>
    /// 設定をファイルに保存
    /// Requirements: 7.2 (ユーザーが設定を保存した時にJSON形式で設定ファイルに書き込む)
    /// </summary>
    public async Task SaveSettingsAsync(CaptureSettings settings)
    {
        var configPath = GetConfigFilePath();
        var configDir = Path.GetDirectoryName(configPath);

        // ディレクトリが存在しない場合は作成
        if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true // 読みやすいように整形
        };

        var json = JsonSerializer.Serialize(settings, options);
        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// デフォルト設定を取得
    /// デフォルトホットキー: Cmd+Shift+S (macOS) / Ctrl+Shift+S (Windows)
    /// デフォルト保存先: ピクチャフォルダ
    /// Requirements: 6.5 (デフォルト保存先としてピクチャフォルダを使用)
    /// Requirements: 5.2 (デフォルトホットキーとしてCmd+Shift+S / Ctrl+Shift+Sを設定)
    /// </summary>
    public CaptureSettings GetDefaultSettings()
    {
        // デフォルトのキャプチャ領域（一般的なゲームCanvas: 1200x720）
        var defaultRegion = new CaptureRegion(
            X: 0,
            Y: 0,
            Width: 1200,
            Height: 720
        );

        // デフォルトホットキー: Cmd+Shift+S (macOS) / Ctrl+Shift+S (Windows)
        // KeyCode 1 は 'S' キーを表す（プラットフォーム固有の値は後で調整）
        var defaultHotkey = new HotkeyConfig(
            Modifiers: HotkeyModifiers.Control | HotkeyModifiers.Shift,
            KeyCode: 1 // 'S' キー（実際のキーコードはプラットフォーム依存）
        );

        // デフォルト保存先: ピクチャフォルダ
        var defaultSaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        return new CaptureSettings
        {
            Region = defaultRegion,
            HotkeyConfig = defaultHotkey,
            SaveDirectory = defaultSaveDirectory,
            IsMaskEnabled = false,
            MaskRegions = Array.Empty<MaskRegion>()
        };
    }

    /// <summary>
    /// 設定ファイルパスを取得
    /// Requirements: 7.3 (macOS: ~/Library/Application Support/CanvasSnap/config.json)
    /// Requirements: 7.4 (Windows: %AppData%\CanvasSnap\config.json)
    /// </summary>
    public string GetConfigFilePath()
    {
        // テスト用のカスタムディレクトリが指定されている場合はそれを使用
        if (!string.IsNullOrEmpty(_customConfigDirectory))
        {
            return Path.Combine(_customConfigDirectory, "config.json");
        }

        string baseDirectory;

        if (OperatingSystem.IsMacOS())
        {
            // macOS: ~/Library/Application Support/CanvasSnap
            baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CanvasSnap"
            );
        }
        else if (OperatingSystem.IsWindows())
        {
            // Windows: %AppData%\CanvasSnap
            baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CanvasSnap"
            );
        }
        else
        {
            throw new PlatformNotSupportedException("このプラットフォームはサポートされていません");
        }

        return Path.Combine(baseDirectory, "config.json");
    }
}
