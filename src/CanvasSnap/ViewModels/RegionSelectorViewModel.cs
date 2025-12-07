using System;
using CanvasSnap.Models;
using CanvasSnap.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CanvasSnap.ViewModels;

/// <summary>
/// 領域選択オーバーレイのViewModel
/// Task 11.3: 矩形選択UI、座標変換
/// Requirements: 2.1-2.9, 3.1-3.4 (キャプチャ領域管理、HiDPI対応)
/// </summary>
public class RegionSelectorViewModel : ViewModelBase
{
    private readonly IDisplayService _displayService;
    private readonly DisplayInfo _currentDisplay;
    private LogicalCoordinates? _startPoint;

    public RegionSelectorViewModel(
        IDisplayService displayService,
        DisplayInfo currentDisplay)
    {
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
        _currentDisplay = currentDisplay ?? throw new ArgumentNullException(nameof(currentDisplay));

        // 初期値
        CoordinatesText = string.Empty;
    }

    /// <summary>
    /// 矩形のX座標（論理ピクセル）
    /// </summary>
    [Reactive]
    public int RectangleX { get; set; }

    /// <summary>
    /// 矩形のY座標（論理ピクセル）
    /// </summary>
    [Reactive]
    public int RectangleY { get; set; }

    /// <summary>
    /// 矩形の幅（論理ピクセル）
    /// </summary>
    [Reactive]
    public int RectangleWidth { get; set; }

    /// <summary>
    /// 矩形の高さ（論理ピクセル）
    /// </summary>
    [Reactive]
    public int RectangleHeight { get; set; }

    /// <summary>
    /// 座標とサイズの表示テキスト
    /// </summary>
    [Reactive]
    public string CoordinatesText { get; set; }

    /// <summary>
    /// 領域選択完了イベント
    /// </summary>
    public event EventHandler<CaptureRegion>? RegionSelected;

    /// <summary>
    /// マウスボタン押下時の処理
    /// </summary>
    /// <param name="logicalX">論理X座標</param>
    /// <param name="logicalY">論理Y座標</param>
    public void OnMouseDown(int logicalX, int logicalY)
    {
        _startPoint = new LogicalCoordinates(logicalX, logicalY);
    }

    /// <summary>
    /// マウス移動時の処理
    /// </summary>
    /// <param name="logicalX">論理X座標</param>
    /// <param name="logicalY">論理Y座標</param>
    public void OnMouseMove(int logicalX, int logicalY)
    {
        if (_startPoint is null)
            return;

        var current = new LogicalCoordinates(logicalX, logicalY);

        // 矩形の左上と幅・高さを計算（ドラッグ方向に関係なく正の値）
        RectangleX = Math.Min(_startPoint.X, current.X);
        RectangleY = Math.Min(_startPoint.Y, current.Y);
        RectangleWidth = Math.Abs(current.X - _startPoint.X);
        RectangleHeight = Math.Abs(current.Y - _startPoint.Y);

        // 座標表示を更新
        CoordinatesText = $"({RectangleX}, {RectangleY}, {RectangleWidth}, {RectangleHeight})";
    }

    /// <summary>
    /// マウスボタン解放時の処理
    /// </summary>
    /// <param name="logicalX">論理X座標</param>
    /// <param name="logicalY">論理Y座標</param>
    public void OnMouseUp(int logicalX, int logicalY)
    {
        if (_startPoint is null)
            return;

        // 最終的な矩形を計算
        var current = new LogicalCoordinates(logicalX, logicalY);
        var rectX = Math.Min(_startPoint.X, current.X);
        var rectY = Math.Min(_startPoint.Y, current.Y);
        var rectWidth = Math.Abs(current.X - _startPoint.X);
        var rectHeight = Math.Abs(current.Y - _startPoint.Y);

        // 論理座標を物理座標に変換
        var physicalStart = _displayService.LogicalToPhysical(
            new LogicalCoordinates(rectX, rectY),
            _currentDisplay.ScaleFactor
        );

        var physicalEnd = _displayService.LogicalToPhysical(
            new LogicalCoordinates(rectX + rectWidth, rectY + rectHeight),
            _currentDisplay.ScaleFactor
        );

        var physicalWidth = physicalEnd.X - physicalStart.X;
        var physicalHeight = physicalEnd.Y - physicalStart.Y;

        // キャプチャ領域を作成
        var region = new CaptureRegion(
            physicalStart.X,
            physicalStart.Y,
            physicalWidth,
            physicalHeight
        );

        // イベントを発火
        RegionSelected?.Invoke(this, region);

        // 状態をリセット
        _startPoint = null;
    }
}
