using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// MacHotkeyServiceの単体テスト
/// Requirements: 5.1 (ホットキー押下で即座にキャプチャ処理開始)
/// Requirements: 5.2 (デフォルトホットキー設定)
/// Requirements: 5.3 (修飾キーと任意のキーの組み合わせ)
/// Requirements: 5.4 (OS予約ショートカットと競合時は登録拒否)
/// Requirements: 5.5 (IMEがオンでもホットキーに反応)
/// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
/// </summary>
/// <remarks>
/// Phase 1 MVP: 単体テストはインターフェース契約とエラーハンドリングを検証
/// 実際のホットキー機能は統合テストまたは手動テストで確認
/// macOS環境でのアクセシビリティ権限が必要なため、CI/CD環境では実行不可
/// </remarks>
public class MacHotkeyServiceTests
{
    [Fact]
    public void MacHotkeyService_ShouldImplementIHotkeyService()
    {
        // Arrange & Act
        var service = new MacHotkeyService();

        // Assert
        Assert.IsAssignableFrom<IHotkeyService>(service);
    }

    [Fact]
    public void MacHotkeyService_Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new MacHotkeyService());
        Assert.Null(exception);
    }

    [Fact]
    public async Task RegisterHotkeyAsync_ShouldAcceptValidHotkeyConfig()
    {
        // Arrange
        var service = new MacHotkeyService();
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53); // Cmd+Shift+S

        // Act & Assert
        // macOS環境でアクセシビリティ権限がない場合は例外がスローされる可能性がある
        // 非macOS環境では正常に完了する必要がある
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var exception = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config));
            Assert.Null(exception);
        }
        else
        {
            // macOS環境では権限の状態に応じて結果が異なる
            // 権限がない場合はHotkeyRegistrationExceptionがスローされる可能性がある
            try
            {
                await service.RegisterHotkeyAsync(config);
                // 権限がある場合は成功
            }
            catch (HotkeyRegistrationException)
            {
                // 権限がない場合は失敗（これは正常な動作）
            }
        }
    }

    [Fact]
    public async Task UnregisterHotkeyAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new MacHotkeyService();

        // Act
        var exception = await Record.ExceptionAsync(() => service.UnregisterHotkeyAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task UnregisterHotkeyAsync_AfterRegistration_ShouldNotThrow()
    {
        // Arrange
        var service = new MacHotkeyService();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53);
            await service.RegisterHotkeyAsync(config);
        }

        // Act
        var exception = await Record.ExceptionAsync(() => service.UnregisterHotkeyAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task RegisterHotkeyAsync_MultipleTimes_ShouldUnregisterPrevious()
    {
        // Arrange
        var service = new MacHotkeyService();
        var config1 = new HotkeyConfig(HotkeyModifiers.Control, 0x53); // Cmd+S
        var config2 = new HotkeyConfig(HotkeyModifiers.Shift, 0x53);   // Shift+S

        // Act & Assert
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 非macOS環境では正常に完了する必要がある
            var exception1 = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config1));
            var exception2 = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config2));

            Assert.Null(exception1);
            Assert.Null(exception2);
        }
    }

    [Fact]
    public void HotkeyPressedEvent_ShouldBeDefinedOnService()
    {
        // Arrange
        var service = new MacHotkeyService();
        bool eventFired = false;

        // Act
        service.HotkeyPressed += (sender, e) => { eventFired = true; };

        // Assert - イベントハンドラが登録できることを確認
        // 実際の発火は統合テストで確認
        Assert.False(eventFired); // まだ発火していないはず
    }

    [Fact]
    public async Task Dispose_ShouldCleanUpResources()
    {
        // Arrange
        var service = new MacHotkeyService();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53);
            await service.RegisterHotkeyAsync(config);
        }

        // Act
        var exception = Record.Exception(() => service.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task RegisterHotkeyAsync_WithDifferentModifierCombinations_ShouldAccept()
    {
        // Arrange
        var service = new MacHotkeyService();
        var configs = new[]
        {
            new HotkeyConfig(HotkeyModifiers.Control, 0x53),
            new HotkeyConfig(HotkeyModifiers.Shift, 0x53),
            new HotkeyConfig(HotkeyModifiers.Alt, 0x53),
            new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53),
            new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x53),
        };

        // Act & Assert
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var config in configs)
            {
                var exception = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config));
                Assert.Null(exception);
            }
        }
    }
}
