using CanvasSnap.Models;
using Xunit;

namespace CanvasSnap.Tests.Models;

/// <summary>
/// HotkeyConfigとHotkeyModifiersのテスト
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせを受け付ける)
/// </summary>
public class HotkeyConfigTests
{
    [Fact]
    public void HotkeyConfig_ShouldBeCreated_WithValidValues()
    {
        // Arrange & Act
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83); // Ctrl+Shift+S

        // Assert
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, config.Modifiers);
        Assert.Equal(83, config.KeyCode);
    }

    [Fact]
    public void HotkeyModifiers_ShouldSupportFlagCombinations()
    {
        // Arrange & Act
        var controlShift = HotkeyModifiers.Control | HotkeyModifiers.Shift;
        var controlAlt = HotkeyModifiers.Control | HotkeyModifiers.Alt;
        var allModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Alt;

        // Assert
        Assert.True(controlShift.HasFlag(HotkeyModifiers.Control));
        Assert.True(controlShift.HasFlag(HotkeyModifiers.Shift));
        Assert.False(controlShift.HasFlag(HotkeyModifiers.Alt));

        Assert.True(controlAlt.HasFlag(HotkeyModifiers.Control));
        Assert.True(controlAlt.HasFlag(HotkeyModifiers.Alt));

        Assert.True(allModifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(allModifiers.HasFlag(HotkeyModifiers.Shift));
        Assert.True(allModifiers.HasFlag(HotkeyModifiers.Alt));
    }

    [Theory]
    [InlineData(HotkeyModifiers.Control, 83)] // Ctrl+S
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83)] // Ctrl+Shift+S
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt, 83)] // Ctrl+Alt+S
    public void HotkeyConfig_ShouldAcceptVariousModifierCombinations(HotkeyModifiers modifiers, int keyCode)
    {
        // Arrange & Act
        var config = new HotkeyConfig(modifiers, keyCode);

        // Assert
        Assert.Equal(modifiers, config.Modifiers);
        Assert.Equal(keyCode, config.KeyCode);
    }

    [Fact]
    public void HotkeyConfig_ShouldSupportEqualityComparison()
    {
        // Arrange
        var config1 = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);
        var config2 = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);
        var config3 = new HotkeyConfig(HotkeyModifiers.Control, 83);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.NotEqual(config1, config3);
    }

    [Fact]
    public void HotkeyConfig_ShouldBeImmutable()
    {
        // Arrange
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);

        // Act - recordは不変なので、withを使って新しいインスタンスを作成
        var modifiedConfig = config with { KeyCode = 84 };

        // Assert - 元のインスタンスは変更されない
        Assert.Equal(83, config.KeyCode);
        Assert.Equal(84, modifiedConfig.KeyCode);
    }

    [Fact]
    public void HotkeyModifiers_None_ShouldRepresentNoModifiers()
    {
        // Arrange & Act
        var none = HotkeyModifiers.None;

        // Assert
        Assert.Equal(0, (int)none);
        Assert.False(none.HasFlag(HotkeyModifiers.Control));
        Assert.False(none.HasFlag(HotkeyModifiers.Shift));
        Assert.False(none.HasFlag(HotkeyModifiers.Alt));
    }

    [Fact]
    public void HotkeyConfig_DefaultMacOSHotkey_ShouldBeCommandShiftS()
    {
        // Arrange & Act - macOSのデフォルトホットキー: Cmd+Shift+S (Req 5.2)
        // Cmdキーは内部的にControlとして扱う（プラットフォーム別実装で変換）
        var defaultMacOS = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);

        // Assert
        Assert.True(defaultMacOS.Modifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(defaultMacOS.Modifiers.HasFlag(HotkeyModifiers.Shift));
        Assert.Equal(83, defaultMacOS.KeyCode); // 'S'
    }

    [Fact]
    public void HotkeyConfig_DefaultWindowsHotkey_ShouldBeCtrlShiftS()
    {
        // Arrange & Act - Windowsのデフォルトホットキー: Ctrl+Shift+S (Req 5.2)
        var defaultWindows = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83);

        // Assert
        Assert.True(defaultWindows.Modifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(defaultWindows.Modifiers.HasFlag(HotkeyModifiers.Shift));
        Assert.Equal(83, defaultWindows.KeyCode); // 'S'
    }
}
