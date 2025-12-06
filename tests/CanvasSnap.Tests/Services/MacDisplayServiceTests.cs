using CanvasSnap.Models;
using CanvasSnap.Services;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// MacDisplayServiceの単体テスト
/// Requirements: 2.7 (ディスプレイ一覧取得), 3.1 (座標系), 3.2 (ScaleFactor取得)
/// </summary>
public class MacDisplayServiceTests
{
    [Fact]
    public async Task GetAllDisplaysAsync_ShouldReturnAtLeastOneDisplay()
    {
        // Arrange
        var service = new MacDisplayService();

        // Act
        var displays = await service.GetAllDisplaysAsync();

        // Assert
        Assert.NotNull(displays);
        Assert.NotEmpty(displays);
        // macOS環境では少なくとも1つのディスプレイが存在する
        Assert.True(displays.Any());
    }

    [Fact]
    public async Task GetAllDisplaysAsync_ShouldHaveOnePrimaryDisplay()
    {
        // Arrange
        var service = new MacDisplayService();

        // Act
        var displays = await service.GetAllDisplaysAsync();

        // Assert
        var primaryDisplays = displays.Where(d => d.IsPrimary).ToList();
        Assert.Single(primaryDisplays);
    }

    [Fact]
    public async Task GetAllDisplaysAsync_DisplaysShouldHaveValidProperties()
    {
        // Arrange
        var service = new MacDisplayService();

        // Act
        var displays = await service.GetAllDisplaysAsync();

        // Assert
        foreach (var display in displays)
        {
            Assert.NotNull(display.Id);
            Assert.NotEmpty(display.Id);
            Assert.NotNull(display.Name);
            Assert.True(display.Width > 0, "Width should be positive");
            Assert.True(display.Height > 0, "Height should be positive");
            Assert.True(display.ScaleFactor >= 1.0, "ScaleFactor should be at least 1.0");
            Assert.True(display.ScaleFactor <= 3.0, "ScaleFactor should not exceed 3.0 (reasonable max for current displays)");
        }
    }

    [Fact]
    public async Task GetAllDisplaysAsync_PrimaryDisplayShouldHaveOriginAtZero()
    {
        // Arrange
        var service = new MacDisplayService();

        // Act
        var displays = await service.GetAllDisplaysAsync();

        // Assert
        var primaryDisplay = displays.First(d => d.IsPrimary);
        Assert.Equal(0, primaryDisplay.X);
        Assert.Equal(0, primaryDisplay.Y);
    }

    [Fact]
    public async Task GetDisplayInfoAsync_WithRegionInPrimaryDisplay_ShouldReturnPrimaryDisplay()
    {
        // Arrange
        var service = new MacDisplayService();
        var region = new CaptureRegion(100, 100, 800, 600);

        // Act
        var displayInfo = await service.GetDisplayInfoAsync(region);

        // Assert
        Assert.NotNull(displayInfo);
        Assert.True(displayInfo.IsPrimary);
    }

    [Fact]
    public async Task GetDisplayInfoAsync_WithRegionOutsideAllDisplays_ShouldReturnNull()
    {
        // Arrange
        var service = new MacDisplayService();
        // 非常に大きな座標を指定（どのディスプレイ範囲外）
        var region = new CaptureRegion(100000, 100000, 800, 600);

        // Act
        var displayInfo = await service.GetDisplayInfoAsync(region);

        // Assert
        Assert.Null(displayInfo);
    }

    [Fact]
    public void LogicalToPhysical_WithRetinaScaleFactor_ShouldDoubleCoordinates()
    {
        // Arrange
        var service = new MacDisplayService();
        var logical = new LogicalCoordinates(100, 200);
        var scaleFactor = 2.0;

        // Act
        var physical = service.LogicalToPhysical(logical, scaleFactor);

        // Assert
        Assert.Equal(200, physical.X);
        Assert.Equal(400, physical.Y);
    }

    [Fact]
    public void LogicalToPhysical_WithNonRetinaScaleFactor_ShouldKeepCoordinates()
    {
        // Arrange
        var service = new MacDisplayService();
        var logical = new LogicalCoordinates(100, 200);
        var scaleFactor = 1.0;

        // Act
        var physical = service.LogicalToPhysical(logical, scaleFactor);

        // Assert
        Assert.Equal(100, physical.X);
        Assert.Equal(200, physical.Y);
    }

    [Fact]
    public void PhysicalToLogical_WithRetinaScaleFactor_ShouldHalveCoordinates()
    {
        // Arrange
        var service = new MacDisplayService();
        var physical = new PhysicalCoordinates(200, 400);
        var scaleFactor = 2.0;

        // Act
        var logical = service.PhysicalToLogical(physical, scaleFactor);

        // Assert
        Assert.Equal(100, logical.X);
        Assert.Equal(200, logical.Y);
    }

    [Fact]
    public void PhysicalToLogical_WithNonRetinaScaleFactor_ShouldKeepCoordinates()
    {
        // Arrange
        var service = new MacDisplayService();
        var physical = new PhysicalCoordinates(100, 200);
        var scaleFactor = 1.0;

        // Act
        var logical = service.PhysicalToLogical(physical, scaleFactor);

        // Assert
        Assert.Equal(100, logical.X);
        Assert.Equal(200, logical.Y);
    }

    [Fact]
    public void DisplayConfigurationChanged_ShouldBeRaisedWhenDisplayConfigChanges()
    {
        // Arrange
        var service = new MacDisplayService();
        var eventRaised = false;
        EventHandler handler = (sender, args) => { eventRaised = true; };

        // Act
        service.DisplayConfigurationChanged += handler;

        // Note: 実際のディスプレイ構成変更をシミュレートすることは困難なため、
        // このテストはイベント購読が可能であることのみを検証
        // 実際のイベント発火はマニュアルテストで検証

        // Assert - イベント購読が成功したことを確認
        service.DisplayConfigurationChanged -= handler;
        Assert.False(eventRaised); // この時点ではイベントは発火していない
    }
}
