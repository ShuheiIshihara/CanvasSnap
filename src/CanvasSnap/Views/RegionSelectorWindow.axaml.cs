using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CanvasSnap.ViewModels;

namespace CanvasSnap.Views;

/// <summary>
/// 領域選択ウィンドウ
/// Task 12.3: RegionSelectorWindow.axamlの実装
/// Requirements: 2.1-2.5 (全画面透明オーバーレイ、矩形選択、座標表示、ESCキャンセル)
/// </summary>
public partial class RegionSelectorWindow : Window
{
    private RegionSelectorViewModel? ViewModel => DataContext as RegionSelectorViewModel;

    public RegionSelectorWindow()
    {
        InitializeComponent();

        // ESCキーでキャンセル (Requirement 2.5)
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// マウスボタン押下時の処理 (Requirement 2.2)
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null)
            return;

        var point = e.GetPosition(this);
        ViewModel.OnMouseDown((int)point.X, (int)point.Y);
    }

    /// <summary>
    /// マウス移動時の処理 (Requirement 2.2, 2.3)
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel is null)
            return;

        var point = e.GetPosition(this);
        ViewModel.OnMouseMove((int)point.X, (int)point.Y);
    }

    /// <summary>
    /// マウスボタン解放時の処理 (Requirement 2.4)
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel is null)
            return;

        var point = e.GetPosition(this);
        ViewModel.OnMouseUp((int)point.X, (int)point.Y);

        // 選択完了後にウィンドウを閉じる
        Close();
    }

    /// <summary>
    /// ESCキーでキャンセル (Requirement 2.5)
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // ViewModelの状態をリセット
            ViewModel?.OnCancel();

            // ウィンドウを閉じる（選択をキャンセル）
            Close();
        }
    }
}
