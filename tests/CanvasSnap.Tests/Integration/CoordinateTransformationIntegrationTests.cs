using CanvasSnap.Models;
using CanvasSnap.Services;
using CanvasSnap.ViewModels;
using Moq;
using Xunit;

namespace CanvasSnap.Tests.Integration;

/// <summary>
/// 座標変換フロー統合テスト
/// Task 13.2: RegionSelectorViewModel + IDisplayService で論理座標→物理座標変換
/// Requirements: 11.1 (パフォーマンス)
/// </summary>
public class CoordinateTransformationIntegrationTests
{
    private readonly Mock<IDisplayService> _mockDisplayService;
    private readonly DisplayInfo _retinaDisplay;
    private readonly DisplayInfo _nonRetinaDisplay;

    public CoordinateTransformationIntegrationTests()
    {
        _mockDisplayService = new Mock<IDisplayService>();

        _retinaDisplay = new DisplayInfo(
            "1",
            "Primary Display (Retina)",
            0,
            0,
            2880,
            1800,
            2.0,
            true
        );

        _nonRetinaDisplay = new DisplayInfo(
            "2",
            "Secondary Display (Non-Retina)",
            2880,
            0,
            1920,
            1080,
            1.0,
            false
        );
    }

    /// <summary>
    /// Retinaディスプレイでの座標変換フロー: 論理座標 → 物理座標 → CaptureRegion
    /// </summary>
    [Fact]
    public void CoordinateTransformation_RetinaDisplay_ShouldDoubleCoordinates()
    {
        // Arrange
        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 2.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                (int)(logical.X * scale),
                (int)(logical.Y * scale)
            ));

        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        CaptureRegion? capturedRegion = null;
        viewModel.RegionSelected += (sender, region) => capturedRegion = region;

        // Act: マウスダウン → マウスムーブ → マウスアップ（論理座標で操作）
        viewModel.OnMouseDown(100, 100);    // 論理座標 (100, 100)
        viewModel.OnMouseMove(500, 400);    // 論理座標 (500, 400)
        viewModel.OnMouseUp(500, 400);      // 論理座標 (500, 400)

        // Assert: 物理座標に変換されてCaptureRegionが作成される
        Assert.NotNull(capturedRegion);
        Assert.Equal(200, capturedRegion.X);      // 100 * 2.0
        Assert.Equal(200, capturedRegion.Y);      // 100 * 2.0
        Assert.Equal(800, capturedRegion.Width);  // (500 - 100) * 2.0
        Assert.Equal(600, capturedRegion.Height); // (400 - 100) * 2.0

        // Verify: LogicalToPhysicalが2回呼ばれている（開始点と終了点）
        _mockDisplayService.Verify(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 2.0), Times.AtLeast(2));
    }

    /// <summary>
    /// 非Retinaディスプレイでの座標変換フロー: 論理座標 = 物理座標
    /// </summary>
    [Fact]
    public void CoordinateTransformation_NonRetinaDisplay_ShouldKeepCoordinates()
    {
        // Arrange
        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 1.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                logical.X,
                logical.Y
            ));

        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _nonRetinaDisplay);

        CaptureRegion? capturedRegion = null;
        viewModel.RegionSelected += (sender, region) => capturedRegion = region;

        // Act
        viewModel.OnMouseDown(100, 100);
        viewModel.OnMouseMove(500, 400);
        viewModel.OnMouseUp(500, 400);

        // Assert: 論理座標と物理座標が同じ
        Assert.NotNull(capturedRegion);
        Assert.Equal(100, capturedRegion.X);
        Assert.Equal(100, capturedRegion.Y);
        Assert.Equal(400, capturedRegion.Width);  // 500 - 100
        Assert.Equal(300, capturedRegion.Height); // 400 - 100
    }

    /// <summary>
    /// 逆方向ドラッグ（右下から左上）での座標変換フロー
    /// </summary>
    [Fact]
    public void CoordinateTransformation_ReverseDrag_ShouldNormalizeCoordinates()
    {
        // Arrange
        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 2.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                (int)(logical.X * scale),
                (int)(logical.Y * scale)
            ));

        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        CaptureRegion? capturedRegion = null;
        viewModel.RegionSelected += (sender, region) => capturedRegion = region;

        // Act: 右下から左上にドラッグ
        viewModel.OnMouseDown(500, 400);    // 開始点（右下）
        viewModel.OnMouseMove(100, 100);    // 現在地（左上）
        viewModel.OnMouseUp(100, 100);      // 終了点（左上）

        // Assert: 座標が正規化されている（左上が原点）
        Assert.NotNull(capturedRegion);
        Assert.Equal(200, capturedRegion.X);      // min(500, 100) * 2.0
        Assert.Equal(200, capturedRegion.Y);      // min(400, 100) * 2.0
        Assert.Equal(800, capturedRegion.Width);  // abs(100 - 500) * 2.0
        Assert.Equal(600, capturedRegion.Height); // abs(100 - 400) * 2.0
    }

    /// <summary>
    /// リアルタイム座標表示の更新フロー
    /// </summary>
    [Fact]
    public void CoordinateTransformation_MouseMove_ShouldUpdateCoordinatesText()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        // Act
        viewModel.OnMouseDown(100, 100);
        viewModel.OnMouseMove(200, 150);

        // Assert: CoordinatesTextが更新される（論理座標で表示）
        Assert.Equal("(100, 100, 100, 50)", viewModel.CoordinatesText);

        // Act: さらにマウスを動かす
        viewModel.OnMouseMove(300, 250);

        // Assert: 再更新される
        Assert.Equal("(100, 100, 200, 150)", viewModel.CoordinatesText);
    }

    /// <summary>
    /// キャンセル操作（ESCキー）での座標リセットフロー
    /// </summary>
    [Fact]
    public void CoordinateTransformation_Cancel_ShouldResetCoordinates()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        // Act: 選択を開始
        viewModel.OnMouseDown(100, 100);
        viewModel.OnMouseMove(500, 400);

        Assert.Equal("(100, 100, 400, 300)", viewModel.CoordinatesText);

        // Act: キャンセル
        viewModel.OnCancel();

        // Assert: 座標がリセットされる
        Assert.Equal(0, viewModel.RectangleX);
        Assert.Equal(0, viewModel.RectangleY);
        Assert.Equal(0, viewModel.RectangleWidth);
        Assert.Equal(0, viewModel.RectangleHeight);
        Assert.Equal(string.Empty, viewModel.CoordinatesText);
    }

    /// <summary>
    /// マルチディスプレイ環境での座標変換フロー（DisplayInfoの切り替え）
    /// </summary>
    [Fact]
    public void CoordinateTransformation_SwitchDisplay_ShouldUseCorrectScaleFactor()
    {
        // Arrange: 最初はRetinaディスプレイ
        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 2.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                (int)(logical.X * scale),
                (int)(logical.Y * scale)
            ));

        var viewModel1 = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        CaptureRegion? region1 = null;
        viewModel1.RegionSelected += (sender, region) => region1 = region;

        viewModel1.OnMouseDown(100, 100);
        viewModel1.OnMouseUp(200, 200);

        // Assert: Retinaディスプレイの座標（2倍）
        Assert.NotNull(region1);
        Assert.Equal(200, region1.X);

        // Arrange: 次は非Retinaディスプレイ
        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 1.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                logical.X,
                logical.Y
            ));

        var viewModel2 = new RegionSelectorViewModel(_mockDisplayService.Object, _nonRetinaDisplay);

        CaptureRegion? region2 = null;
        viewModel2.RegionSelected += (sender, region) => region2 = region;

        viewModel2.OnMouseDown(100, 100);
        viewModel2.OnMouseUp(200, 200);

        // Assert: 非Retinaディスプレイの座標（等倍）
        Assert.NotNull(region2);
        Assert.Equal(100, region2.X);
    }

    /// <summary>
    /// 座標表示位置の切り替えフロー（上部 ↔ 下部）
    /// </summary>
    [Fact]
    public void CoordinateTransformation_CoordinatesPositioning_ShouldToggleBasedOnYPosition()
    {
        // Arrange
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, _retinaDisplay);

        // Act: 上部に近い位置（Y < 40）
        viewModel.OnMouseDown(100, 10);
        viewModel.OnMouseMove(200, 30);

        // Assert: 座標表示が下に配置される
        Assert.False(viewModel.IsCoordinatesAbove);

        // Act: 中央以降の位置（Y >= 40）
        viewModel.OnMouseDown(100, 100);
        viewModel.OnMouseMove(200, 200);

        // Assert: 座標表示が上に配置される
        Assert.True(viewModel.IsCoordinatesAbove);
    }

    /// <summary>
    /// IDisplayServiceとの統合: GetAllDisplaysAsync → 座標変換 → CaptureRegion
    /// </summary>
    [Fact]
    public async Task CoordinateTransformation_Integration_WithDisplayService_ShouldWork()
    {
        // Arrange
        _mockDisplayService.Setup(d => d.GetAllDisplaysAsync())
            .ReturnsAsync(new[] { _retinaDisplay, _nonRetinaDisplay });

        _mockDisplayService.Setup(d => d.LogicalToPhysical(It.IsAny<LogicalCoordinates>(), 2.0))
            .Returns((LogicalCoordinates logical, double scale) => new PhysicalCoordinates(
                (int)(logical.X * scale),
                (int)(logical.Y * scale)
            ));

        // Act: ディスプレイ情報を取得
        var displays = await _mockDisplayService.Object.GetAllDisplaysAsync();
        var primaryDisplay = displays.First(d => d.IsPrimary);

        // Act: ViewModelを作成して座標選択
        var viewModel = new RegionSelectorViewModel(_mockDisplayService.Object, primaryDisplay);

        CaptureRegion? capturedRegion = null;
        viewModel.RegionSelected += (sender, region) => capturedRegion = region;

        viewModel.OnMouseDown(100, 100);
        viewModel.OnMouseUp(500, 400);

        // Assert
        Assert.NotNull(capturedRegion);
        Assert.Equal(200, capturedRegion.X);
        Assert.Equal(200, capturedRegion.Y);
        Assert.Equal(800, capturedRegion.Width);
        Assert.Equal(600, capturedRegion.Height);

        _mockDisplayService.Verify(d => d.GetAllDisplaysAsync(), Times.Once);
    }
}
