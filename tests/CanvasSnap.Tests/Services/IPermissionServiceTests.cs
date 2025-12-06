using CanvasSnap.Exceptions;
using CanvasSnap.Services;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// IPermissionServiceインターフェースの契約テスト
/// Requirements: 12.1 (権限チェック), 12.2 (権限設定画面)
/// </summary>
public class IPermissionServiceTests
{
    [Fact]
    public async Task CheckPermissionsAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckPermissionsAsync())
            .ReturnsAsync(true);

        // Act
        var result = await mockService.Object.CheckPermissionsAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task CheckScreenRecordingPermissionAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckScreenRecordingPermissionAsync())
            .ReturnsAsync(true);

        // Act
        var result = await mockService.Object.CheckScreenRecordingPermissionAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task CheckAccessibilityPermissionAsync_ShouldReturnBooleanResult()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckAccessibilityPermissionAsync())
            .ReturnsAsync(true);

        // Act
        var result = await mockService.Object.CheckAccessibilityPermissionAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task OpenPermissionSettingsAsync_WithScreenRecording_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.OpenPermissionSettingsAsync(PermissionType.ScreenRecording))
            .Returns(Task.CompletedTask);

        // Act & Assert - 例外なく完了すべき
        await mockService.Object.OpenPermissionSettingsAsync(PermissionType.ScreenRecording);

        mockService.Verify(s => s.OpenPermissionSettingsAsync(PermissionType.ScreenRecording), Times.Once);
    }

    [Fact]
    public async Task OpenPermissionSettingsAsync_WithAccessibility_ShouldExecuteSuccessfully()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.OpenPermissionSettingsAsync(PermissionType.Accessibility))
            .Returns(Task.CompletedTask);

        // Act & Assert - 例外なく完了すべき
        await mockService.Object.OpenPermissionSettingsAsync(PermissionType.Accessibility);

        mockService.Verify(s => s.OpenPermissionSettingsAsync(PermissionType.Accessibility), Times.Once);
    }

    [Fact]
    public async Task CheckPermissionsAsync_WhenAllPermissionsGranted_ShouldReturnTrue()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckScreenRecordingPermissionAsync()).ReturnsAsync(true);
        mockService.Setup(s => s.CheckAccessibilityPermissionAsync()).ReturnsAsync(true);
        mockService.Setup(s => s.CheckPermissionsAsync()).ReturnsAsync(true);

        // Act
        var result = await mockService.Object.CheckPermissionsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckPermissionsAsync_WhenScreenRecordingDenied_ShouldReturnFalse()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckScreenRecordingPermissionAsync()).ReturnsAsync(false);
        mockService.Setup(s => s.CheckAccessibilityPermissionAsync()).ReturnsAsync(true);
        mockService.Setup(s => s.CheckPermissionsAsync()).ReturnsAsync(false);

        // Act
        var result = await mockService.Object.CheckPermissionsAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckPermissionsAsync_WhenAccessibilityDenied_ShouldReturnFalse()
    {
        // Arrange
        var mockService = new Mock<IPermissionService>();
        mockService.Setup(s => s.CheckScreenRecordingPermissionAsync()).ReturnsAsync(true);
        mockService.Setup(s => s.CheckAccessibilityPermissionAsync()).ReturnsAsync(false);
        mockService.Setup(s => s.CheckPermissionsAsync()).ReturnsAsync(false);

        // Act
        var result = await mockService.Object.CheckPermissionsAsync();

        // Assert
        Assert.False(result);
    }
}

/// <summary>
/// 権限タイプ列挙型のテスト
/// </summary>
public class PermissionTypeTests
{
    [Fact]
    public void PermissionType_ShouldHaveScreenRecordingValue()
    {
        // Act
        var value = PermissionType.ScreenRecording;

        // Assert
        Assert.Equal(PermissionType.ScreenRecording, value);
    }

    [Fact]
    public void PermissionType_ShouldHaveAccessibilityValue()
    {
        // Act
        var value = PermissionType.Accessibility;

        // Assert
        Assert.Equal(PermissionType.Accessibility, value);
    }
}
