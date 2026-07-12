using System;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CanvasSnap.Models;
using CanvasSnap.Services;
using CanvasSnap.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.ViewModels;

/// <summary>
/// MainWindowViewModelのテスト
/// Task 11.1: MainWindowViewModelの実装
/// </summary>
public class MainWindowViewModelTests
{
    private readonly Mock<ICaptureOrchestrator> _mockOrchestrator;
    private readonly Mock<IHotkeyService> _mockHotkeyService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;

    public MainWindowViewModelTests()
    {
        // モックの初期化
        _mockOrchestrator = new Mock<ICaptureOrchestrator>();
        _mockHotkeyService = new Mock<IHotkeyService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(viewModel.ShowSettingsCommand);
        Assert.NotNull(viewModel.ExitCommand);
    }

    [Fact]
    public void Constructor_ShouldSubscribeToHotkeyPressedEvent()
    {
        // Arrange
        var hotkeyPressedInvoked = false;
        _mockHotkeyService.SetupAdd(s => s.HotkeyPressed += It.IsAny<EventHandler>())
            .Callback<EventHandler>(handler => hotkeyPressedInvoked = true);

        // Act
        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Assert
        Assert.True(hotkeyPressedInvoked);
    }

    // OnHotkeyPressedはDispatcher.UIThread.InvokeAsyncを使用するため、
    // ヘッドレスAvalonia環境のUIスレッド上で実行する必要がある
    [AvaloniaFact]
    public async Task OnHotkeyPressed_ShouldInvokeExecuteCaptureAsync()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 100, 100),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/tmp",
            IsMaskEnabled = false
        };

        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(settings);

        _mockOrchestrator.Setup(o => o.ExecuteCaptureAsync(It.IsAny<CaptureSettings>()))
            .ReturnsAsync(Result<string, CaptureError>.Success("/tmp/screenshot.png"));

        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Act - ホットキーイベントを発火
        _mockHotkeyService.Raise(s => s.HotkeyPressed += null, EventArgs.Empty);

        // Dispatcherにキューイングされた処理を実行
        Dispatcher.UIThread.RunJobs();
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        // Assert
        _mockSettingsService.Verify(s => s.LoadSettingsAsync(), Times.Once);
        _mockOrchestrator.Verify(o => o.ExecuteCaptureAsync(settings), Times.Once);
    }

    [Fact]
    public void ShowSettingsCommand_CanExecute_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Act
        var canExecute = viewModel.ShowSettingsCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ExitCommand_CanExecute_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Act
        var canExecute = viewModel.ExitCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    // OnHotkeyPressedはDispatcher.UIThread.InvokeAsyncを使用するため、
    // ヘッドレスAvalonia環境のUIスレッド上で実行する必要がある
    [AvaloniaFact]
    public async Task OnHotkeyPressed_WhenCaptureErrors_ShouldHandleGracefully()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 100, 100),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/tmp",
            IsMaskEnabled = false
        };

        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(settings);

        _mockOrchestrator.Setup(o => o.ExecuteCaptureAsync(It.IsAny<CaptureSettings>()))
            .ReturnsAsync(Result<string, CaptureError>.Failure(CaptureError.PermissionDenied));

        var viewModel = new MainWindowViewModel(
            _mockOrchestrator.Object,
            _mockHotkeyService.Object,
            _mockSettingsService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );

        // Act - ホットキーイベントを発火
        _mockHotkeyService.Raise(s => s.HotkeyPressed += null, EventArgs.Empty);

        // Dispatcherにキューイングされた処理を実行
        Dispatcher.UIThread.RunJobs();
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        // Assert - エラーでも例外が発生しないことを確認
        _mockOrchestrator.Verify(o => o.ExecuteCaptureAsync(settings), Times.Once);
    }
}
