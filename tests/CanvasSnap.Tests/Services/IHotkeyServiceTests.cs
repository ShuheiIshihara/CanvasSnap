using System;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// IHotkeyServiceインターフェースの契約テスト
/// Requirements: 5.1 (ホットキー押下で即座にキャプチャ処理開始)
/// Requirements: 5.4 (OS予約ショートカットと競合時は登録拒否)
/// Requirements: 5.6 (ホットキー登録失敗時のエラーメッセージ)
/// </summary>
public class IHotkeyServiceTests
{
    /// <summary>
    /// モックIHotkeyService実装（テスト用）
    /// </summary>
    private class MockHotkeyService : IHotkeyService
    {
        private HotkeyConfig? _registeredHotkey;
        private bool _shouldThrowConflict;
        private bool _shouldThrowRegistration;

        public event EventHandler? HotkeyPressed;

        public MockHotkeyService(bool throwConflict = false, bool throwRegistration = false)
        {
            _shouldThrowConflict = throwConflict;
            _shouldThrowRegistration = throwRegistration;
        }

        public Task RegisterHotkeyAsync(HotkeyConfig config)
        {
            if (_shouldThrowConflict)
            {
                throw new HotkeyConflictException(
                    "Hotkey conflicts with system shortcut",
                    config);
            }

            if (_shouldThrowRegistration)
            {
                throw new HotkeyRegistrationException(
                    "Failed to register hotkey");
            }

            _registeredHotkey = config;
            return Task.CompletedTask;
        }

        public Task UnregisterHotkeyAsync()
        {
            _registeredHotkey = null;
            return Task.CompletedTask;
        }

        public void TriggerHotkeyPressed()
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    [Fact]
    public async Task RegisterHotkeyAsync_ShouldAcceptValidHotkeyConfig()
    {
        // Arrange
        var service = new MockHotkeyService();
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53); // Cmd+Shift+S

        // Act
        var exception = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task RegisterHotkeyAsync_WhenConflict_ShouldThrowHotkeyConflictException()
    {
        // Arrange
        var service = new MockHotkeyService(throwConflict: true);
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HotkeyConflictException>(
            () => service.RegisterHotkeyAsync(config));

        Assert.NotNull(exception.ConflictingHotkey);
        Assert.Equal(config, exception.ConflictingHotkey);
    }

    [Fact]
    public async Task RegisterHotkeyAsync_WhenRegistrationFails_ShouldThrowHotkeyRegistrationException()
    {
        // Arrange
        var service = new MockHotkeyService(throwRegistration: true);
        var config = new HotkeyConfig(HotkeyModifiers.Control, 0x41); // Cmd+A

        // Act & Assert
        await Assert.ThrowsAsync<HotkeyRegistrationException>(
            () => service.RegisterHotkeyAsync(config));
    }

    [Fact]
    public async Task UnregisterHotkeyAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new MockHotkeyService();
        var config = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53);
        await service.RegisterHotkeyAsync(config);

        // Act
        var exception = await Record.ExceptionAsync(() => service.UnregisterHotkeyAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void HotkeyPressedEvent_ShouldBeTriggered()
    {
        // Arrange
        var service = new MockHotkeyService();
        bool eventFired = false;
        service.HotkeyPressed += (sender, e) => { eventFired = true; };

        // Act
        service.TriggerHotkeyPressed();

        // Assert
        Assert.True(eventFired, "HotkeyPressed event should be triggered");
    }

    [Fact]
    public async Task RegisterHotkeyAsync_WithDifferentModifiers_ShouldAcceptAll()
    {
        // Arrange
        var service = new MockHotkeyService();
        var configs = new[]
        {
            new HotkeyConfig(HotkeyModifiers.Control, 0x53),
            new HotkeyConfig(HotkeyModifiers.Shift, 0x53),
            new HotkeyConfig(HotkeyModifiers.Alt, 0x53),
            new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53),
            new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x53),
        };

        // Act & Assert
        foreach (var config in configs)
        {
            var exception = await Record.ExceptionAsync(() => service.RegisterHotkeyAsync(config));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void HotkeyPressedEvent_ShouldProvideEventArgs()
    {
        // Arrange
        var service = new MockHotkeyService();
        EventArgs? capturedArgs = null;
        service.HotkeyPressed += (sender, e) => { capturedArgs = e; };

        // Act
        service.TriggerHotkeyPressed();

        // Assert
        Assert.NotNull(capturedArgs);
    }

    [Fact]
    public async Task UnregisterHotkeyAsync_WithoutRegistration_ShouldNotThrow()
    {
        // Arrange
        var service = new MockHotkeyService();

        // Act
        var exception = await Record.ExceptionAsync(() => service.UnregisterHotkeyAsync());

        // Assert
        Assert.Null(exception);
    }
}
