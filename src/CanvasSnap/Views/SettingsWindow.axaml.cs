using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CanvasSnap.ViewModels;

namespace CanvasSnap.Views;

/// <summary>
/// 設定ウィンドウ
/// Task 12.2: ホットキー、キャプチャ領域、マスク領域、保存先の表示UIを実装
/// Requirements: 8.1-8.10 (設定画面UI)
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// キャンセルボタンのクリックハンドラ
    /// </summary>
    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 参照ボタンのクリックハンドラ (Requirement 8.7)
    /// </summary>
    private async void OnBrowseDirectory(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var selectedPath = await ShowFolderPickerAsync(viewModel.SaveDirectory);
        if (selectedPath != null)
        {
            viewModel.SaveDirectory = selectedPath;
        }
    }

    /// <summary>
    /// フォルダ選択ダイアログを表示
    /// </summary>
    private async Task<string?> ShowFolderPickerAsync(string? startPath)
    {
        var storageProvider = StorageProvider;
        if (storageProvider == null)
        {
            return null;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "保存先フォルダを選択",
            AllowMultiple = false
        };

        // 開始パスが指定されている場合は設定
        if (!string.IsNullOrEmpty(startPath) && System.IO.Directory.Exists(startPath))
        {
            try
            {
                var folder = await storageProvider.TryGetFolderFromPathAsync(new Uri(startPath, UriKind.Absolute));
                if (folder != null)
                {
                    options.SuggestedStartLocation = folder;
                }
            }
            catch (Exception ex)
            {
                // 無効なパスの場合はログに記録して無視
                Debug.WriteLine($"Failed to set suggested start location: {startPath}, Error: {ex.Message}");
            }
        }

        try
        {
            var result = await storageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                return result[0].Path.LocalPath;
            }
        }
        catch (OperationCanceledException)
        {
            // ユーザーがキャンセルした場合は何もしない
            Debug.WriteLine("Folder picker was cancelled by user");
        }
        catch (Exception ex)
        {
            // エラーが発生した場合はログに記録
            Debug.WriteLine($"Failed to open folder picker: {ex.Message}");
        }

        return null;
    }
}
