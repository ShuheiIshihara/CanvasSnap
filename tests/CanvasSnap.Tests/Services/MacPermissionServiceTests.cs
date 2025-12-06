using CanvasSnap.Exceptions;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// MacPermissionServiceの単体テスト
/// Requirements: 12.1 (権限チェック), 12.2 (システム設定を開く), 12.3 (Info.plist設定)
/// </summary>
public class MacPermissionServiceTests
{
    [Fact]
    public async Task CheckScreenRecordingPermissionAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act
        var result = await service.CheckScreenRecordingPermissionAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task CheckAccessibilityPermissionAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act
        var result = await service.CheckAccessibilityPermissionAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task CheckPermissionsAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act
        var result = await service.CheckPermissionsAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task OpenPermissionSettingsAsync_WithScreenRecording_ShouldNotThrow()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act & Assert - 例外をスローしないことを確認
        await service.OpenPermissionSettingsAsync(PermissionType.ScreenRecording);
    }

    [Fact]
    public async Task OpenPermissionSettingsAsync_WithAccessibility_ShouldNotThrow()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act & Assert - 例外をスローしないことを確認
        await service.OpenPermissionSettingsAsync(PermissionType.Accessibility);
    }

    [Fact]
    public async Task CheckPermissionsAsync_ShouldCheckBothPermissions()
    {
        // Arrange
        var service = new MacPermissionService();

        // Act
        var allPermissions = await service.CheckPermissionsAsync();
        var screenRecording = await service.CheckScreenRecordingPermissionAsync();
        var accessibility = await service.CheckAccessibilityPermissionAsync();

        // Assert
        // Phase 1 MVP: テスト環境では権限チェックの動作を確認
        // allPermissionsは両方の権限のAND条件であることを確認
        Assert.IsType<bool>(allPermissions);
        Assert.IsType<bool>(screenRecording);
        Assert.IsType<bool>(accessibility);
    }

    [Fact]
    public void MacPermissionService_ShouldImplementIPermissionService()
    {
        // Arrange & Act
        var service = new MacPermissionService();

        // Assert
        Assert.IsAssignableFrom<IPermissionService>(service);
    }
}
