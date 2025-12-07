using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// MacNotificationServiceの単体テスト
/// Requirements: 10.1 (キャプチャ成功時のOS標準通知)
/// Requirements: 10.2 (軽微なエラーのOS標準通知)
/// Requirements: 10.3 (クリティカルエラーのダイアログ表示)
/// Requirements: 10.4 (権限エラー時のシステム設定誘導ボタン)
/// </summary>
/// <remarks>
/// Phase 1 MVP: 単体テストはインターフェース契約とエラーハンドリングを検証
/// 実際の通知表示は統合テストまたは手動テストで確認
/// macOS環境でのUNUserNotificationCenter権限が必要なため、CI/CD環境では実行不可
/// </remarks>
public class MacNotificationServiceTests
{
    private sealed class StubAppleScriptExecutor : MacNotificationService.IAppleScriptExecutor
    {
        public List<string> Scripts { get; } = new();
        public bool ThrowOnExecute { get; set; }

        public Task ExecuteAsync(string script)
        {
            if (ThrowOnExecute)
            {
                throw new InvalidOperationException("osascript failed");
            }

            Scripts.Add(script);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void MacNotificationService_ShouldImplementINotificationService()
    {
        // Arrange & Act
        var service = new MacNotificationService();

        // Assert
        Assert.IsAssignableFrom<INotificationService>(service);
    }

    [Fact]
    public void MacNotificationService_Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new MacNotificationService());
        Assert.Null(exception);
    }

    [Fact]
    public async Task ShowNotificationAsync_ShouldAcceptMessage()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);
        var message = "スクリーンショットを保存しました";

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.ShowNotificationAsync(message));

        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Equal(
            MacNotificationService.BuildNotificationScript(message, NotificationType.Info),
            executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowNotificationAsync_WithNotificationType_ShouldAccept()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);

        // Act & Assert
        var exceptionSuccess = await Record.ExceptionAsync(() =>
            service.ShowNotificationAsync("成功", NotificationType.Success));
        var exceptionWarning = await Record.ExceptionAsync(() =>
            service.ShowNotificationAsync("警告", NotificationType.Warning));
        var exceptionError = await Record.ExceptionAsync(() =>
            service.ShowNotificationAsync("エラー", NotificationType.Error));

        Assert.Null(exceptionSuccess);
        Assert.Null(exceptionWarning);
        Assert.Null(exceptionError);
        Assert.Equal(3, executor.Scripts.Count);
        Assert.Contains("subtitle \"成功\"", executor.Scripts[0]);
        Assert.Contains("subtitle \"警告\"", executor.Scripts[1]);
        Assert.Contains("subtitle \"エラー\"", executor.Scripts[2]);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_ShouldAcceptTitleAndMessage()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);
        var title = "権限エラー";
        var message = "必要な権限が付与されていません";

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            service.ShowCriticalErrorAsync(title, message));

        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Equal(
            MacNotificationService.BuildDialogScript(title, message, null),
            executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_WithActionButtons_ShouldAccept()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);
        var title = "権限エラー";
        var message = "必要な権限が付与されていません";
        var actionButtons = new[] { "システム設定を開く", "閉じる" };

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            service.ShowCriticalErrorAsync(title, message, actionButtons));

        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Contains("buttons {\"システム設定を開く\", \"閉じる\"}", executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_WithoutActionButtons_ShouldAccept()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            service.ShowCriticalErrorAsync("エラー", "メッセージ"));

        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Contains("buttons {\"閉じる\"}", executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowCriticalErrorAsync_EmptyButtons_ShouldFallbackToClose()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.ShowCriticalErrorAsync("タイトル", "本文", Array.Empty<string>()));

        // Assert
        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Contains("buttons {\"閉じる\"}", executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowNotificationAsync_DefaultType_ShouldBeInfo()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: true, executor);
        var message = "テストメッセージ";

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.ShowNotificationAsync(message));

        Assert.Null(exception);
        Assert.Single(executor.Scripts);
        Assert.Contains("subtitle \"情報\"", executor.Scripts[0]);
    }

    [Fact]
    public async Task ShowNotificationAsync_ShouldSwallowExecutorErrors()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor { ThrowOnExecute = true };
        var service = new MacNotificationService(isMacOS: true, executor);

        // Act
        var exception = await Record.ExceptionAsync(() => service.ShowNotificationAsync("失敗する通知"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ShowNotificationAsync_ShouldSkipWhenNotMacOs()
    {
        // Arrange
        var executor = new StubAppleScriptExecutor();
        var service = new MacNotificationService(isMacOS: false, executor);

        // Act
        var exception = await Record.ExceptionAsync(() => service.ShowNotificationAsync("非macOS"));

        // Assert
        Assert.Null(exception);
        Assert.Empty(executor.Scripts);
    }

    [Theory]
    [InlineData("line1\nline2", "line1\\nline2")]
    [InlineData("escape!shell", "escape\\!shell")]
    public void EscapeShellArgument_ShouldEscapeNewlinesAndExclamation(string input, string expectedFragment)
    {
        // Act
        var escaped = MacNotificationService.EscapeShellArgument(input);

        // Assert
        Assert.Contains(expectedFragment, escaped);
    }
}
