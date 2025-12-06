using System.Text.Json;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// ISettingsServiceの単体テスト
/// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        // テスト用の一時ディレクトリを作成
        _testDirectory = Path.Combine(Path.GetTempPath(), "CanvasSnapTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockNotificationService = new Mock<INotificationService>();
        _service = new SettingsService(_testDirectory, _mockNotificationService.Object);
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

    /// <summary>
    /// 設定ファイルが破損している場合、ユーザーに通知を発行する
    /// Requirements: 7.7 (設定ファイル破損により復元した場合、ユーザーに通知し設定の再構成を促す)
    /// </summary>
    [Fact]
    public async Task LoadSettingsAsync_WhenJsonIsCorrupted_NotifiesUser()
    {
        // Arrange
        var configPath = _service.GetConfigFilePath();
        var configDir = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir!);
        }

        // 不正なJSONを書き込む（破損ファイルをシミュレート）
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        // Act
        await _service.LoadSettingsAsync();

        // Assert - 通知が発行されること
        _mockNotificationService.Verify(
            n => n.ShowNotificationAsync(
                It.Is<string>(msg => msg.Contains("設定ファイル") || msg.Contains("デフォルト設定")),
                NotificationType.Warning),
            Times.Once);
    }

    /// <summary>
    /// 設定ファイルが正常な場合、通知は発行されない
    /// </summary>
    [Fact]
    public async Task LoadSettingsAsync_WhenJsonIsValid_DoesNotNotify()
    {
        // Arrange
        var validSettings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 1920, 1080),
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 1),
            SaveDirectory = "/tmp/screenshots",
            IsMaskEnabled = false,
            MaskRegions = Array.Empty<MaskRegion>()
        };
        await _service.SaveSettingsAsync(validSettings);

        // Act
        await _service.LoadSettingsAsync();

        // Assert - 通知は発行されないこと
        _mockNotificationService.Verify(
            n => n.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<NotificationType>()),
            Times.Never);
    }

    /// <summary>
    /// 通知サービスがnullの場合でも、破損検出時に例外をスローしない
    /// </summary>
    [Fact]
    public async Task LoadSettingsAsync_WhenNotificationServiceIsNull_DoesNotThrow()
    {
        // Arrange
        var serviceWithoutNotification = new SettingsService(_testDirectory);
        var configPath = serviceWithoutNotification.GetConfigFilePath();
        var configDir = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir!);
        }

        // 不正なJSONを書き込む
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        // Act & Assert - 例外がスローされないこと
        var exception = await Record.ExceptionAsync(() => serviceWithoutNotification.LoadSettingsAsync());
        Assert.Null(exception);

        // デフォルト設定が返されること
        var settings = await serviceWithoutNotification.LoadSettingsAsync();
        Assert.NotNull(settings);
    }
}
