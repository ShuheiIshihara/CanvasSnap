using System;
using System.Linq;
using CanvasSnap.Models;
using CanvasSnap.Services;
using CanvasSnap.ViewModels;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.ViewModels;

/// <summary>
/// RegionSelectorViewModelのテスト
/// Task 11.3: RegionSelectorViewModelの実装
/// </summary>
public class RegionSelectorViewModelTests
{
    private readonly Mock<IDisplayService> _mockDisplayService;
    private readonly DisplayInfo _testDisplay;

    public RegionSelectorViewModelTests()
    {
        _mockDisplayService = new Mock<IDisplayService>();
        _testDisplay = new DisplayInfo(
            Id: "0",
            Name: "Main Display",
            X: 0,
            Y: 0,
            Width: 1920,
            Height: 1080,
            ScaleFactor: 2.0,
            IsPrimary: true
        );
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);

        // Assert
        Assert.NotNull(viewModel.CoordinatesText);
        Assert.Equal(0, viewModel.RectangleX);
        Assert.Equal(0, viewModel.RectangleY);
        Assert.Equal(0, viewModel.RectangleWidth);
        Assert.Equal(0, viewModel.RectangleHeight);
    }

    [Fact]
    public void OnMouseDown_ShouldSetStartPoint()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);

        // Act
        viewModel.OnMouseDown(100, 200);

        // Assert - 内部状態は次のOnMouseMoveで確認
        viewModel.OnMouseMove(150, 250);
        Assert.Equal(100, viewModel.RectangleX);
        Assert.Equal(200, viewModel.RectangleY);
        Assert.Equal(50, viewModel.RectangleWidth);
        Assert.Equal(50, viewModel.RectangleHeight);
    }

    [Fact]
    public void OnMouseMove_ShouldUpdateRectangleProperties()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        viewModel.OnMouseDown(100, 150);

        // Act
        viewModel.OnMouseMove(300, 400);

        // Assert
        Assert.Equal(100, viewModel.RectangleX);
        Assert.Equal(150, viewModel.RectangleY);
        Assert.Equal(200, viewModel.RectangleWidth);
        Assert.Equal(250, viewModel.RectangleHeight);
    }

    [Fact]
    public void OnMouseMove_WithNegativeDrag_ShouldHandleCorrectly()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        viewModel.OnMouseDown(300, 400);

        // Act - 左上にドラッグ
        viewModel.OnMouseMove(100, 150);

        // Assert - 矩形は常に正の幅・高さを持つ
        Assert.Equal(100, viewModel.RectangleX);
        Assert.Equal(150, viewModel.RectangleY);
        Assert.Equal(200, viewModel.RectangleWidth);
        Assert.Equal(250, viewModel.RectangleHeight);
    }

    [Fact]
    public void OnMouseMove_ShouldUpdateCoordinatesText()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        viewModel.OnMouseDown(50, 75);

        // Act
        viewModel.OnMouseMove(200, 300);

        // Assert
        Assert.Contains("50", viewModel.CoordinatesText);
        Assert.Contains("75", viewModel.CoordinatesText);
        Assert.Contains("150", viewModel.CoordinatesText);
        Assert.Contains("225", viewModel.CoordinatesText);
    }

    [Fact]
    public void OnMouseUp_ShouldConvertToPhysicalCoordinates()
    {
        // Arrange
        _mockDisplayService.Setup(s => s.LogicalToPhysical(
                It.IsAny<LogicalCoordinates>(),
                It.IsAny<double>()))
            .Returns<LogicalCoordinates, double>((logical, scale) =>
                new PhysicalCoordinates((int)(logical.X * scale), (int)(logical.Y * scale)));

        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        viewModel.OnMouseDown(100, 150);
        viewModel.OnMouseMove(300, 400);

        CaptureRegion? capturedRegion = null;
        viewModel.RegionSelected += (sender, region) => capturedRegion = region;

        // Act
        viewModel.OnMouseUp(300, 400);

        // Assert
        Assert.NotNull(capturedRegion);
        Assert.Equal(200, capturedRegion.X); // 100 * 2.0
        Assert.Equal(300, capturedRegion.Y); // 150 * 2.0
        Assert.Equal(400, capturedRegion.Width); // 200 * 2.0
        Assert.Equal(500, capturedRegion.Height); // 250 * 2.0
    }

    [Fact]
    public void OnMouseUp_ShouldFireRegionSelectedEvent()
    {
        // Arrange
        _mockDisplayService.Setup(s => s.LogicalToPhysical(
                It.IsAny<LogicalCoordinates>(),
                It.IsAny<double>()))
            .Returns<LogicalCoordinates, double>((logical, scale) =>
                new PhysicalCoordinates((int)(logical.X * scale), (int)(logical.Y * scale)));

        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        viewModel.OnMouseDown(10, 20);
        viewModel.OnMouseMove(110, 120);

        var eventFired = false;
        viewModel.RegionSelected += (sender, region) => eventFired = true;

        // Act
        viewModel.OnMouseUp(110, 120);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void RectangleProperties_PropertyChanged_ShouldNotify()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _testDisplay);
        var propertyNames = new System.Collections.Generic.List<string>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                propertyNames.Add(e.PropertyName);
        };

        viewModel.OnMouseDown(50, 75);

        // Act
        viewModel.OnMouseMove(100, 100);

        // Assert
        Assert.Contains(nameof(viewModel.RectangleX), propertyNames);
        Assert.Contains(nameof(viewModel.RectangleY), propertyNames);
        Assert.Contains(nameof(viewModel.RectangleWidth), propertyNames);
        Assert.Contains(nameof(viewModel.RectangleHeight), propertyNames);
        Assert.Contains(nameof(viewModel.CoordinatesText), propertyNames);
    }
}
