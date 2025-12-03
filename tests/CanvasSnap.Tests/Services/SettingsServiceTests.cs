using System.Text.Json;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// ISettingsServiceの単体テスト
/// Requirements: 7.1, 7.2, 7.3, 7.4
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        // テスト用の一時ディレクトリを作成
        _testDirectory = Path.Combine(Path.GetTempPath(), "CanvasSnapTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _service = new SettingsService(_testDirectory);
    }

    public void Dispose()
    {
        // テスト後にクリーンアップ
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void GetDefaultSettings_ReturnsValidSettings()
    {
        // Act
        var settings = _service.GetDefaultSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.Region);
        Assert.NotNull(settings.HotkeyConfig);
        Assert.NotNull(settings.SaveDirectory);
        Assert.False(string.IsNullOrEmpty(settings.SaveDirectory));

        // デフォルトホットキーはCmd+Shift+S (macOS) / Ctrl+Shift+S (Windows)
        Assert.True(settings.HotkeyConfig.Modifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(settings.HotkeyConfig.Modifiers.HasFlag(HotkeyModifiers.Shift));

        // マスク機能はデフォルトで無効
        Assert.False(settings.IsMaskEnabled);
    }

    [Fact]
    public void GetConfigFilePath_ReturnsPlatformSpecificPath()
    {
        // デフォルトコンストラクタでプラットフォーム固有のパスをテスト
        var defaultService = new SettingsService();

        // Act
        var path = defaultService.GetConfigFilePath();

        // Assert
        Assert.NotNull(path);
        Assert.False(string.IsNullOrEmpty(path));

        if (OperatingSystem.IsMacOS())
        {
            // macOS: ~/Library/Application Support/CanvasSnap/config.json
            Assert.Contains("Library/Application Support/CanvasSnap", path);
        }
        else if (OperatingSystem.IsWindows())
        {
            // Windows: %AppData%\CanvasSnap\config.json
            Assert.Contains("CanvasSnap", path);
        }

        Assert.EndsWith("config.json", path);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(_service.GetDefaultSettings().HotkeyConfig, settings.HotkeyConfig);
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesConfigFile()
    {
        // Arrange
        var testSettings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 1920, 1080),
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 1), // Cmd+Shift+S
            SaveDirectory = "/tmp/screenshots",
            IsMaskEnabled = false,
            MaskRegions = Array.Empty<MaskRegion>()
        };

        // Act
        await _service.SaveSettingsAsync(testSettings);

        // Assert
        var configPath = _service.GetConfigFilePath();
        Assert.True(File.Exists(configPath));
    }

    [Fact]
    public async Task SaveAndLoadSettings_PreservesAllData()
    {
        // Arrange
        var originalSettings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 1920, 1080),
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 1),
            SaveDirectory = "/tmp/screenshots",
            IsMaskEnabled = true,
            MaskRegions = new[]
            {
                new MaskRegion(50, 50, 100, 100),
                new MaskRegion(200, 200, 150, 150)
            }
        };

        // Act
        await _service.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal(originalSettings.Region, loadedSettings.Region);
        Assert.Equal(originalSettings.HotkeyConfig, loadedSettings.HotkeyConfig);
        Assert.Equal(originalSettings.SaveDirectory, loadedSettings.SaveDirectory);
        Assert.Equal(originalSettings.IsMaskEnabled, loadedSettings.IsMaskEnabled);
        Assert.Equal(originalSettings.MaskRegions.Length, loadedSettings.MaskRegions.Length);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenJsonIsInvalid_ReturnsDefaultSettings()
    {
        // Arrange
        var configPath = _service.GetConfigFilePath();
        var configDir = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir!);
        }

        // 不正なJSONを書き込む
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert - デフォルト設定が返されること
        Assert.NotNull(settings);

        // 破損ファイルが.backupとして保存されていること
        Assert.True(File.Exists(configPath + ".backup"));
    }
}
