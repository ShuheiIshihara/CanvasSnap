using CanvasSnap.Models;
using System.Text.Json;
using Xunit;

namespace CanvasSnap.Tests.Models;

/// <summary>
/// CaptureSettings集約ルートのテスト
/// Requirements: 7.2 (設定をJSON形式で保存)
/// </summary>
public class CaptureSettingsTests
{
    [Fact]
    public void CaptureSettings_ShouldBeCreated_WithAllRequiredProperties()
    {
        // Arrange
        var region = new CaptureRegion(100, 200, 1920, 1080);
        var maskRegions = new[] { new MaskRegion(50, 100, 200, 50) };
        var hotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);
        var saveDirectory = "/Users/test/Pictures";

        // Act
        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = maskRegions,
            HotkeyConfig = hotkeyConfig,
            SaveDirectory = saveDirectory,
            IsMaskEnabled = true
        };

        // Assert
        Assert.Equal(region, settings.Region);
        Assert.Equal(maskRegions, settings.MaskRegions);
        Assert.Equal(hotkeyConfig, settings.HotkeyConfig);
        Assert.Equal(saveDirectory, settings.SaveDirectory);
        Assert.True(settings.IsMaskEnabled);
    }

    [Fact]
    public void CaptureSettings_MaskRegions_ShouldDefaultToEmptyArray()
    {
        // Arrange
        var region = new CaptureRegion(100, 200, 1920, 1080);
        var hotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);
        var saveDirectory = "/Users/test/Pictures";

        // Act - MaskRegionsを指定しない
        var settings = new CaptureSettings
        {
            Region = region,
            HotkeyConfig = hotkeyConfig,
            SaveDirectory = saveDirectory,
            IsMaskEnabled = false
        };

        // Assert
        Assert.NotNull(settings.MaskRegions);
        Assert.Empty(settings.MaskRegions);
    }

    [Fact]
    public void CaptureSettings_ShouldSerializeToJson_Successfully()
    {
        // Arrange
        var region = new CaptureRegion(100, 200, 1920, 1080);
        var maskRegions = new[] { new MaskRegion(50, 100, 200, 50) };
        var hotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);
        var saveDirectory = "/Users/test/Pictures";

        var settings = new CaptureSettings
        {
            Region = region,
            MaskRegions = maskRegions,
            HotkeyConfig = hotkeyConfig,
            SaveDirectory = saveDirectory,
            IsMaskEnabled = true
        };

        // Act
        var json = JsonSerializer.Serialize(settings);

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("\"Region\"", json);
        Assert.Contains("\"MaskRegions\"", json);
        Assert.Contains("\"HotkeyConfig\"", json);
        Assert.Contains("\"SaveDirectory\"", json);
        Assert.Contains("\"IsMaskEnabled\"", json);
    }

    [Fact]
    public void CaptureSettings_ShouldDeserializeFromJson_Successfully()
    {
        // Arrange
        var json = """
        {
            "Region": {
                "X": 100,
                "Y": 200,
                "Width": 1920,
                "Height": 1080
            },
            "MaskRegions": [
                {
                    "X": 50,
                    "Y": 100,
                    "Width": 200,
                    "Height": 50
                }
            ],
            "HotkeyConfig": {
                "Modifiers": 3,
                "KeyCode": 83
            },
            "SaveDirectory": "/Users/test/Pictures",
            "IsMaskEnabled": true
        }
        """;

        // Act
        var settings = JsonSerializer.Deserialize<CaptureSettings>(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(100, settings.Region.X);
        Assert.Equal(200, settings.Region.Y);
        Assert.Single(settings.MaskRegions);
        Assert.Equal(50, settings.MaskRegions[0].X);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyConfig.Modifiers);
        Assert.Equal("/Users/test/Pictures", settings.SaveDirectory);
        Assert.True(settings.IsMaskEnabled);
    }

    [Fact]
    public void CaptureSettings_ShouldSupportRoundTripSerialization()
    {
        // Arrange
        var original = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 1920, 1080),
            MaskRegions = new[]
            {
                new MaskRegion(50, 100, 200, 50),
                new MaskRegion(300, 150, 100, 30)
            },
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = true
        };

        // Act - シリアライズ → デシリアライズ
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CaptureSettings>(json);

        // Assert - 元の値と一致する
        Assert.NotNull(deserialized);
        Assert.Equal(original.Region, deserialized.Region);
        Assert.Equal(original.MaskRegions.Length, deserialized.MaskRegions.Length);
        Assert.Equal(original.MaskRegions[0], deserialized.MaskRegions[0]);
        Assert.Equal(original.HotkeyConfig, deserialized.HotkeyConfig);
        Assert.Equal(original.SaveDirectory, deserialized.SaveDirectory);
        Assert.Equal(original.IsMaskEnabled, deserialized.IsMaskEnabled);
    }

    [Fact]
    public void CaptureSettings_ShouldBeImmutable()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 1920, 1080),
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = true
        };

        // Act - withを使って新しいインスタンスを作成
        var modifiedSettings = settings with { IsMaskEnabled = false };

        // Assert - 元のインスタンスは変更されない
        Assert.True(settings.IsMaskEnabled);
        Assert.False(modifiedSettings.IsMaskEnabled);
    }

    [Fact]
    public void CaptureSettings_ShouldSupportMultipleMaskRegions()
    {
        // Arrange & Act - 複数のマスク領域を含む設定
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 1920, 1080),
            MaskRegions = new[]
            {
                new MaskRegion(50, 100, 200, 50),   // ユーザー名
                new MaskRegion(300, 150, 100, 30),  // スコア
                new MaskRegion(500, 200, 150, 40)   // ランク
            },
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/Users/test/Pictures",
            IsMaskEnabled = true
        };

        // Assert
        Assert.Equal(3, settings.MaskRegions.Length);
        Assert.All(settings.MaskRegions, mask =>
        {
            Assert.True(mask.Width > 0);
            Assert.True(mask.Height > 0);
        });
    }
}
