using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Integration;

/// <summary>
/// 設定永続化フロー統合テスト
/// Task 13.2: SaveSettings → LoadSettings ラウンドトリップ検証
/// Requirements: 11.1 (パフォーマンス)
/// </summary>
public class SettingsPersistenceIntegrationTests : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly string _testConfigDirectory;
    private readonly string _testConfigFilePath;

    public SettingsPersistenceIntegrationTests()
    {
        _testConfigDirectory = Path.Combine(Path.GetTempPath(), "CanvasSnapTests", "Settings", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigDirectory);

        _testConfigFilePath = Path.Combine(_testConfigDirectory, "config.json");
        _settingsService = new SettingsService(_testConfigFilePath);
    }

    /// <summary>
    /// ラウンドトリップテスト: 設定を保存し、読み込んで、元の設定と一致することを検証
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_RoundTrip_ShouldPreserveAllProperties()
    {
        // Arrange
        var originalSettings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 800, 600),
            MaskRegions =
            [
                new MaskRegion(50, 50, 100, 50),
                new MaskRegion(200, 300, 150, 75)
            ],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = true
        };

        // Act: 保存
        await _settingsService.SaveSettingsAsync(originalSettings);

        // Assert: ファイルが作成されたことを確認
        Assert.True(File.Exists(_testConfigFilePath));

        // Act: 読み込み
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert: ラウンドトリップで全プロパティが保持されている
        Assert.NotNull(loadedSettings);
        Assert.Equal(originalSettings.Region.X, loadedSettings.Region.X);
        Assert.Equal(originalSettings.Region.Y, loadedSettings.Region.Y);
        Assert.Equal(originalSettings.Region.Width, loadedSettings.Region.Width);
        Assert.Equal(originalSettings.Region.Height, loadedSettings.Region.Height);

        Assert.Equal(originalSettings.MaskRegions.Length, loadedSettings.MaskRegions.Length);
        for (int i = 0; i < originalSettings.MaskRegions.Length; i++)
        {
            Assert.Equal(originalSettings.MaskRegions[i].X, loadedSettings.MaskRegions[i].X);
            Assert.Equal(originalSettings.MaskRegions[i].Y, loadedSettings.MaskRegions[i].Y);
            Assert.Equal(originalSettings.MaskRegions[i].Width, loadedSettings.MaskRegions[i].Width);
            Assert.Equal(originalSettings.MaskRegions[i].Height, loadedSettings.MaskRegions[i].Height);
        }

        Assert.Equal(originalSettings.HotkeyConfig.Modifiers, loadedSettings.HotkeyConfig.Modifiers);
        Assert.Equal(originalSettings.HotkeyConfig.KeyCode, loadedSettings.HotkeyConfig.KeyCode);
        Assert.Equal(originalSettings.SaveDirectory, loadedSettings.SaveDirectory);
        Assert.Equal(originalSettings.IsMaskEnabled, loadedSettings.IsMaskEnabled);
    }

    /// <summary>
    /// マスク領域なしの設定のラウンドトリップテスト
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_RoundTrip_WithoutMaskRegions_ShouldPreserveEmptyArray()
    {
        // Arrange
        var originalSettings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 800, 600),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = false
        };

        // Act
        await _settingsService.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Empty(loadedSettings.MaskRegions);
        Assert.False(loadedSettings.IsMaskEnabled);
    }

    /// <summary>
    /// 複数回の保存・読み込みサイクルで設定が一貫性を保つことを検証
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_MultipleSaveCycles_ShouldMaintainConsistency()
    {
        // Arrange
        var settings1 = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 800, 600),
            MaskRegions = [new MaskRegion(50, 50, 100, 50)],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = true
        };

        var settings2 = new CaptureSettings
        {
            Region = new CaptureRegion(200, 300, 1024, 768),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Alt | HotkeyModifiers.Shift, 67),
            SaveDirectory = "/Users/test/Documents",
            IsMaskEnabled = false
        };

        // Act: 1回目の保存・読み込み
        await _settingsService.SaveSettingsAsync(settings1);
        var loaded1 = await _settingsService.LoadSettingsAsync();

        // Assert: 1回目の検証
        Assert.Equal(settings1.Region.X, loaded1.Region.X);
        Assert.Equal(settings1.SaveDirectory, loaded1.SaveDirectory);

        // Act: 2回目の保存・読み込み（上書き）
        await _settingsService.SaveSettingsAsync(settings2);
        var loaded2 = await _settingsService.LoadSettingsAsync();

        // Assert: 2回目の検証（settings1ではなくsettings2が読み込まれる）
        Assert.Equal(settings2.Region.X, loaded2.Region.X);
        Assert.Equal(settings2.Region.Width, loaded2.Region.Width);
        Assert.Equal(settings2.SaveDirectory, loaded2.SaveDirectory);
        Assert.Empty(loaded2.MaskRegions);
        Assert.False(loaded2.IsMaskEnabled);
    }

    /// <summary>
    /// 初回読み込み（ファイル不在）でデフォルト設定が返されることを検証
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_FirstLoad_ShouldReturnDefaultSettings()
    {
        // Arrange: ファイルが存在しない状態
        Assert.False(File.Exists(_testConfigFilePath));

        // Act
        var settings = await _settingsService.LoadSettingsAsync();

        // Assert: デフォルト設定が返される
        Assert.NotNull(settings);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyConfig.Modifiers);
        Assert.Equal(1, settings.HotkeyConfig.KeyCode); // 'S' キー（プラットフォーム依存）
        Assert.Contains("Pictures", settings.SaveDirectory);
        Assert.Empty(settings.MaskRegions);
        Assert.False(settings.IsMaskEnabled);
    }

    /// <summary>
    /// 破損したJSON設定ファイルの処理
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_CorruptedJson_ShouldBackupAndReturnDefault()
    {
        // Arrange: 破損したJSONファイルを作成
        var corruptedJson = "{ invalid json content }}}";
        await File.WriteAllTextAsync(_testConfigFilePath, corruptedJson);

        // Act
        var settings = await _settingsService.LoadSettingsAsync();

        // Assert: デフォルト設定が返される
        Assert.NotNull(settings);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyConfig.Modifiers);

        // Assert: バックアップファイルが作成されている
        var backupFilePath = _testConfigFilePath + ".backup";
        Assert.True(File.Exists(backupFilePath));

        var backupContent = await File.ReadAllTextAsync(backupFilePath);
        Assert.Equal(corruptedJson, backupContent);
    }

    /// <summary>
    /// JSON整形（インデント）が保存されることを検証
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_SavedJson_ShouldBeIndented()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 800, 600),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = false
        };

        // Act
        await _settingsService.SaveSettingsAsync(settings);

        // Assert: ファイル内容が整形されている（インデント付き）
        var jsonContent = await File.ReadAllTextAsync(_testConfigFilePath);
        Assert.Contains("\n", jsonContent); // 改行を含む
        Assert.Contains("  ", jsonContent); // インデントを含む
    }

    /// <summary>
    /// パス検証: 保存先ディレクトリが存在しない場合に作成されることを検証
    /// </summary>
    [Fact]
    public async Task SettingsPersistence_SaveToNonExistentDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var nestedDirectory = Path.Combine(_testConfigDirectory, "nested", "deep", "path");
        var nestedConfigPath = Path.Combine(nestedDirectory, "config.json");

        var settingsService = new SettingsService(nestedConfigPath);
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 800, 600),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = false
        };

        Assert.False(Directory.Exists(nestedDirectory));

        // Act
        await settingsService.SaveSettingsAsync(settings);

        // Assert: ディレクトリとファイルが作成されている
        Assert.True(Directory.Exists(nestedDirectory));
        Assert.True(File.Exists(nestedConfigPath));

        // Cleanup
        Directory.Delete(nestedDirectory, true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testConfigDirectory))
        {
            try
            {
                Directory.Delete(_testConfigDirectory, true);
            }
            catch
            {
                // ベストエフォート
            }
        }
    }
}
