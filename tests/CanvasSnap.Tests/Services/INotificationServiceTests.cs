using System;
using System.Threading.Tasks;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// INotificationServiceインターフェースの契約テスト
/// Requirements: 10.1 (キャプチャ成功時のOS標準通知)
/// Requirements: 10.2 (軽微なエラーのOS標準通知)
/// Requirements: 10.3 (クリティカルエラーのダイアログ表示)
/// Requirements: 10.4 (権限エラー時のシステム設定誘導ボタン)
/// </summary>
public class INotificationServiceTests
{
    /// <summary>
    /// モックINotificationService実装（テスト用）
    /// </summary>
    private class MockNotificationService : INotificationService
    {
        public string? LastNotificationMessage { get; private set; }
        public NotificationType? LastNotificationType { get; private set; }
        public string? LastCriticalErrorTitle { get; private set; }
        public string? LastCriticalErrorMessage { get; private set; }
        public string[]? LastActionButtons { get; private set; }

        public Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info)
        {
            LastNotificationMessage = message;
            LastNotificationType = type;
            return Task.CompletedTask;
        }

        public Task ShowCriticalErrorAsync(string title, string message, string[]? actionButtons = null)
        {
            LastCriticalErrorTitle = title;
            LastCriticalErrorMessage = message;
            LastActionButtons = actionButtons;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ShowNotificationAsync_ShouldAcceptMessage()
    {
        // Arrange
        var service = new MockNotificationService();
        var message = "スクリーンショットを保存しました";

        // Act
        await service.ShowNotificationAsync(message);

        // Assert
        Assert.Equal(message, service.LastNotificationMessage);
    }

    [Fact]
    public async Task ShowNotificationAsync_DefaultType_ShouldBeInfo()
    {
        // Arrange
        var service = new MockNotificationService();

        // Act
        await service.ShowNotificationAsync("テストメッセージ");

        // Assert
        Assert.Equal(NotificationType.Info, service.LastNotificationType);
    }

    [Fact]
    public async Task ShowNotificationAsync_WithSuccessType_ShouldAccept()
    {
        // Arrange
        var service = new MockNotificationService();

        // Act
        await service.ShowNotificationAsync("成功", NotificationType.Success);

        // Assert
        Assert.Equal(NotificationType.Success, service.LastNotificationType);
    }

    [Fact]
    public async Task ShowNotificationAsync_WithWarningType_ShouldAccept()
    {
        // Arrange
        var service = new MockNotificationService();

        // Act
        await service.ShowNotificationAsync("警告", NotificationType.Warning);

        // Assert
        Assert.Equal(NotificationType.Warning, service.LastNotificationType);
    }

    [Fact]
    public async Task ShowNotificationAsync_WithErrorType_ShouldAccept()
    {
        // Arrange
        var service = new MockNotificationService();

        // Act
        await service.ShowNotificationAsync("エラー", NotificationType.Error);

        // Assert
        Assert.Equal(NotificationType.Error, service.LastNotificationType);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_ShouldAcceptTitleAndMessage()
    {
        // Arrange
        var service = new MockNotificationService();
        var title = "権限エラー";
        var message = "必要な権限が付与されていません";

        // Act
        await service.ShowCriticalErrorAsync(title, message);

        // Assert
        Assert.Equal(title, service.LastCriticalErrorTitle);
        Assert.Equal(message, service.LastCriticalErrorMessage);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_WithActionButtons_ShouldAccept()
    {
        // Arrange
        var service = new MockNotificationService();
        var actionButtons = new[] { "システム設定を開く", "閉じる" };

        // Act
        await service.ShowCriticalErrorAsync("エラー", "メッセージ", actionButtons);

        // Assert
        Assert.NotNull(service.LastActionButtons);
        Assert.Equal(2, service.LastActionButtons.Length);
        Assert.Equal("システム設定を開く", service.LastActionButtons[0]);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_WithoutActionButtons_ShouldAcceptNull()
    {
        // Arrange
        var service = new MockNotificationService();

        // Act
        await service.ShowCriticalErrorAsync("エラー", "メッセージ");

        // Assert
        // actionButtonsはnullでも受け入れる
        Assert.Null(service.LastActionButtons);
    }

    [Fact]
    public void NotificationType_ShouldHaveFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<NotificationType>();

        // Assert
        Assert.Equal(4, values.Length);
        Assert.Contains(NotificationType.Info, values);
        Assert.Contains(NotificationType.Success, values);
        Assert.Contains(NotificationType.Warning, values);
        Assert.Contains(NotificationType.Error, values);
    }
}
