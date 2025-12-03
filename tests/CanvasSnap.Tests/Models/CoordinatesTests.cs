using CanvasSnap.Models;
using Xunit;

namespace CanvasSnap.Tests.Models;

/// <summary>
/// PhysicalCoordinatesとLogicalCoordinatesのテスト
/// Requirements: 3.4 (座標変換用型の定義)
/// </summary>
public class CoordinatesTests
{
    [Fact]
    public void PhysicalCoordinates_ShouldBeCreated_WithValidValues()
    {
        // Arrange & Act
        var physical = new PhysicalCoordinates(100, 200);

        // Assert
        Assert.Equal(100, physical.X);
        Assert.Equal(200, physical.Y);
    }

    [Fact]
    public void LogicalCoordinates_ShouldBeCreated_WithValidValues()
    {
        // Arrange & Act
        var logical = new LogicalCoordinates(50, 100);

        // Assert
        Assert.Equal(50, logical.X);
        Assert.Equal(100, logical.Y);
    }

    [Fact]
    public void PhysicalCoordinates_ShouldSupportEqualityComparison()
    {
        // Arrange
        var coord1 = new PhysicalCoordinates(100, 200);
        var coord2 = new PhysicalCoordinates(100, 200);
        var coord3 = new PhysicalCoordinates(150, 200);

        // Act & Assert
        Assert.Equal(coord1, coord2);
        Assert.NotEqual(coord1, coord3);
    }

    [Fact]
    public void LogicalCoordinates_ShouldSupportEqualityComparison()
    {
        // Arrange
        var coord1 = new LogicalCoordinates(50, 100);
        var coord2 = new LogicalCoordinates(50, 100);
        var coord3 = new LogicalCoordinates(75, 100);

        // Act & Assert
        Assert.Equal(coord1, coord2);
        Assert.NotEqual(coord1, coord3);
    }

    [Fact]
    public void PhysicalCoordinates_ShouldSupportNegativeValues()
    {
        // Arrange & Act - セカンダリディスプレイが左側にある場合、負の座標値を持つ可能性がある
        var physical = new PhysicalCoordinates(-100, -50);

        // Assert
        Assert.Equal(-100, physical.X);
        Assert.Equal(-50, physical.Y);
    }

    [Fact]
    public void LogicalCoordinates_ShouldSupportNegativeValues()
    {
        // Arrange & Act - UI表示で負の座標が必要な場合がある
        var logical = new LogicalCoordinates(-50, -25);

        // Assert
        Assert.Equal(-50, logical.X);
        Assert.Equal(-25, logical.Y);
    }
}
