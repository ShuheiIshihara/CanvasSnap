using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CanvasSnap.Models;
using CanvasSnap.Services;
using CanvasSnap.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.ViewModels;

/// <summary>
/// SettingsViewModelのテスト
/// Task 11.2: SettingsViewModelの実装
/// </summary>
public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IDisplayService> _mockDisplayService;
    private readonly Mock<IHotkeyService> _mockHotkeyService;
    private readonly Mock<ICaptureOrchestrator> _mockOrchestrator;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<SettingsViewModel>> _mockLogger;

    public SettingsViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockDisplayService = new Mock<IDisplayService>();
        _mockHotkeyService = new Mock<IHotkeyService>();
        _mockOrchestrator = new Mock<ICaptureOrchestrator>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<SettingsViewModel>>();

        _mockNotificationService.Setup(n => n.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<NotificationType>()))
            .Returns(Task.CompletedTask);
        _mockNotificationService.Setup(n => n.ShowCriticalErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        _mockHotkeyService.Setup(h => h.RegisterHotkeyAsync(It.IsAny<HotkeyConfig>()))
            .Returns(Task.CompletedTask);
        _mockSettingsService.Setup(s => s.SaveSettingsAsync(It.IsAny<CaptureSettings>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Arrange & Act
        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(viewModel.SelectRegionCommand);
        Assert.NotNull(viewModel.SelectMaskCommand);
        Assert.NotNull(viewModel.TestCaptureCommand);
        Assert.NotNull(viewModel.SaveCommand);
        Assert.NotNull(viewModel.BrowseDirectoryCommand);
    }

    [Fact]
    public async Task Constructor_ShouldLoadSettings()
    {
        // Arrange
        var settings = new CaptureSettings
        {
            Region = new CaptureRegion(100, 200, 300, 400),
            MaskRegions = [new MaskRegion(10, 20, 30, 40)],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/tmp/screenshots",
            IsMaskEnabled = true
        };

        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(settings);

        // Act
        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        // Assert
        Assert.Equal("(100, 200, 300, 400)", viewModel.RegionText);
        Assert.Equal("/tmp/screenshots", viewModel.SaveDirectory);
        Assert.True(viewModel.IsMaskEnabled);
    }

    [Fact]
    public void HotkeyText_ShouldReflectHotkeyConfig()
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

        // Act
        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        // Assert
        Assert.Contains("Ctrl", viewModel.HotkeyText);
        Assert.Contains("Shift", viewModel.HotkeyText);
    }

    [Fact]
    public async Task SaveCommand_ShouldSaveSettings()
    {
        // Arrange
        var initialSettings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 100, 100),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/tmp",
            IsMaskEnabled = false
        };

        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(initialSettings);

        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        // Act
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        _mockSettingsService.Verify(s => s.SaveSettingsAsync(It.IsAny<CaptureSettings>()), Times.Once);
    }

    [Fact]
    public async Task TestCaptureCommand_ShouldExecuteCapture()
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
            .ReturnsAsync(Result<string, CaptureError>.Success("/tmp/test.png"));

        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        // Act
        viewModel.TestCaptureCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        _mockOrchestrator.Verify(o => o.ExecuteCaptureAsync(It.IsAny<CaptureSettings>()), Times.Once);
    }

    [Fact]
    public async Task IsMaskEnabled_PropertyChanged_ShouldNotify()
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

        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        var propertyChangedFired = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsMaskEnabled))
                propertyChangedFired = true;
        };

        // Act
        viewModel.IsMaskEnabled = true;

        // Assert
        Assert.True(propertyChangedFired);
        Assert.True(viewModel.IsMaskEnabled);
    }

    [Fact]
    public async Task SaveDirectory_PropertyChanged_ShouldNotify()
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

        var viewModel = new SettingsViewModel(
            _mockSettingsService.Object,
            _mockDisplayService.Object,
            _mockHotkeyService.Object,
            _mockOrchestrator.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        await viewModel.InitializeAsync();

        var propertyChangedFired = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SaveDirectory))
                propertyChangedFired = true;
        };

        // Act
        viewModel.SaveDirectory = "/new/path";

        // Assert
        Assert.True(propertyChangedFired);
        Assert.Equal("/new/path", viewModel.SaveDirectory);
    }
}
