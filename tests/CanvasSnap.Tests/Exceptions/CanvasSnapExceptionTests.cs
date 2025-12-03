using System;
using CanvasSnap.Exceptions;
using Xunit;

namespace CanvasSnap.Tests.Exceptions;

/// <summary>
/// CanvasSnapException基底クラスのテスト
/// Requirements: 12.1, 12.2 (権限管理とエラーハンドリング)
/// </summary>
public class CanvasSnapExceptionTests
{
    [Fact]
    public void CanvasSnapException_ShouldBeCreated_WithMessage()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new CanvasSnapException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void CanvasSnapException_ShouldBeCreated_WithMessageAndInnerException()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new CanvasSnapException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ScreenCaptureException_ShouldInheritFromCanvasSnapException()
    {
        // Arrange & Act
        var exception = new ScreenCaptureException("Screen capture failed");

        // Assert
        Assert.IsAssignableFrom<CanvasSnapException>(exception);
        Assert.Equal("Screen capture failed", exception.Message);
    }

    [Fact]
    public void ImageProcessingException_ShouldInheritFromCanvasSnapException()
    {
        // Arrange & Act
        var exception = new ImageProcessingException("Image processing failed");

        // Assert
        Assert.IsAssignableFrom<CanvasSnapException>(exception);
        Assert.Equal("Image processing failed", exception.Message);
    }

    [Fact]
    public void SettingsException_ShouldInheritFromCanvasSnapException()
    {
        // Arrange & Act
        var exception = new SettingsException("Settings load failed");

        // Assert
        Assert.IsAssignableFrom<CanvasSnapException>(exception);
        Assert.Equal("Settings load failed", exception.Message);
    }

    [Fact]
    public void PermissionDeniedException_ShouldInheritFromCanvasSnapException()
    {
        // Arrange & Act
        var exception = new PermissionDeniedException("Permission denied", PermissionType.ScreenRecording);

        // Assert
        Assert.IsAssignableFrom<CanvasSnapException>(exception);
        Assert.Equal("Permission denied", exception.Message);
    }

    [Fact]
    public void PermissionDeniedException_ShouldContainPermissionType()
    {
        // Arrange
        var permissionType = PermissionType.ScreenRecording;

        // Act
        var exception = new PermissionDeniedException("Screen recording permission required", permissionType);

        // Assert
        Assert.Equal(permissionType, exception.PermissionType);
    }

    [Theory]
    [InlineData(PermissionType.ScreenRecording)]
    [InlineData(PermissionType.Accessibility)]
    public void PermissionDeniedException_ShouldSupportAllPermissionTypes(PermissionType permissionType)
    {
        // Arrange & Act
        var exception = new PermissionDeniedException($"Permission required: {permissionType}", permissionType);

        // Assert
        Assert.Equal(permissionType, exception.PermissionType);
    }

    [Fact]
    public void PermissionDeniedException_ShouldSupportInnerException()
    {
        // Arrange
        var innerException = new UnauthorizedAccessException("Underlying permission error");
        var permissionType = PermissionType.ScreenRecording;

        // Act
        var exception = new PermissionDeniedException("Permission denied", permissionType, innerException);

        // Assert
        Assert.Equal(permissionType, exception.PermissionType);
        Assert.Equal(innerException, exception.InnerException);
    }
}
