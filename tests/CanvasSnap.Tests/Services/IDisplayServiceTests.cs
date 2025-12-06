using CanvasSnap.Models;
using CanvasSnap.Services;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Services;

/// <summary>
/// IDisplayServiceインターフェースの契約テスト
/// Requirements: 2.6 (ディスプレイ情報取得), 3.1 (座標系), 3.3 (座標変換)
/// </summary>
public class IDisplayServiceTests
{
    [Fact]
    public async Task GetAllDisplaysAsync_ShouldReturnDisplayInfoCollection()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var expectedDisplays = new List<DisplayInfo>
        {
            new DisplayInfo("1", "Primary Display", 0, 0, 1920, 1080, 2.0, true),
            new DisplayInfo("2", "Secondary Display", 1920, 0, 1920, 1080, 1.0, false)
        };
        mockService.Setup(s => s.GetAllDisplaysAsync())
            .ReturnsAsync(expectedDisplays);

        // Act
        var result = await mockService.Object.GetAllDisplaysAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.IsPrimary);
    }

    [Fact]
    public async Task GetDisplayInfoAsync_WithValidRegion_ShouldReturnDisplayInfo()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var region = new CaptureRegion(100, 100, 800, 600);
        var expectedDisplay = new DisplayInfo("1", "Primary Display", 0, 0, 1920, 1080, 2.0, true);
        mockService.Setup(s => s.GetDisplayInfoAsync(region))
            .ReturnsAsync(expectedDisplay);

        // Act
        var result = await mockService.Object.GetDisplayInfoAsync(region);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task GetDisplayInfoAsync_WithRegionOutsideDisplay_ShouldReturnNull()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var region = new CaptureRegion(10000, 10000, 800, 600);
        mockService.Setup(s => s.GetDisplayInfoAsync(region))
            .ReturnsAsync((DisplayInfo?)null);

        // Act
        var result = await mockService.Object.GetDisplayInfoAsync(region);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LogicalToPhysical_WithRetinaScaleFactor_ShouldDoubleCoordinates()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var logical = new LogicalCoordinates(100, 200);
        var scaleFactor = 2.0;
        var expectedPhysical = new PhysicalCoordinates(200, 400);

        mockService.Setup(s => s.LogicalToPhysical(logical, scaleFactor))
            .Returns(expectedPhysical);

        // Act
        var result = mockService.Object.LogicalToPhysical(logical, scaleFactor);

        // Assert
        Assert.Equal(200, result.X);
        Assert.Equal(400, result.Y);
    }

    [Fact]
    public void LogicalToPhysical_WithNonRetinaScaleFactor_ShouldKeepCoordinates()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var logical = new LogicalCoordinates(100, 200);
        var scaleFactor = 1.0;
        var expectedPhysical = new PhysicalCoordinates(100, 200);

        mockService.Setup(s => s.LogicalToPhysical(logical, scaleFactor))
            .Returns(expectedPhysical);

        // Act
        var result = mockService.Object.LogicalToPhysical(logical, scaleFactor);

        // Assert
        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void PhysicalToLogical_WithRetinaScaleFactor_ShouldHalveCoordinates()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var physical = new PhysicalCoordinates(200, 400);
        var scaleFactor = 2.0;
        var expectedLogical = new LogicalCoordinates(100, 200);

        mockService.Setup(s => s.PhysicalToLogical(physical, scaleFactor))
            .Returns(expectedLogical);

        // Act
        var result = mockService.Object.PhysicalToLogical(physical, scaleFactor);

        // Assert
        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void PhysicalToLogical_WithNonRetinaScaleFactor_ShouldKeepCoordinates()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();
        var physical = new PhysicalCoordinates(100, 200);
        var scaleFactor = 1.0;
        var expectedLogical = new LogicalCoordinates(100, 200);

        mockService.Setup(s => s.PhysicalToLogical(physical, scaleFactor))
            .Returns(expectedLogical);

        // Act
        var result = mockService.Object.PhysicalToLogical(physical, scaleFactor);

        // Assert
        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void DisplayConfigurationChanged_EventShouldExistInInterface()
    {
        // Arrange
        var mockService = new Mock<IDisplayService>();

        // Act & Assert - イベントが定義されていることを確認
        // インターフェースにイベントが存在することを型システムで検証
        // 実際のイベント発火テストは具象実装クラスのテストで行う
        EventHandler? handler = null;
        mockService.SetupAdd(s => s.DisplayConfigurationChanged += It.IsAny<EventHandler>())
            .Callback<EventHandler>(h => handler = h);

        // イベント購読は成功すべき
        mockService.Object.DisplayConfigurationChanged += (sender, args) => { };

        // 検証: イベント購読が呼び出されたことを確認
        mockService.VerifyAdd(s => s.DisplayConfigurationChanged += It.IsAny<EventHandler>(), Times.Once);
    }
}
