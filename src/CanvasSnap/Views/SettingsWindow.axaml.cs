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
    /// 参照ボタンのクリックハンドラ (Requirement 8.7, 8.8, 8.9, 6.7, 6.8)
    /// </summary>
    private async void OnBrowseDirectory(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var selectedPath = await ShowFolderPickerAsync(viewModel.SaveDirectory);
        if (selectedPath == null)
        {
            return;
        }

        // 書き込み権限とディスク容量をチェック (Requirement 8.8, 6.7, 6.8)
        var validationResult = await ValidateDirectoryAsync(selectedPath);
        if (!validationResult.IsValid)
        {
            // エラーダイアログを表示 (Requirement 8.9, 6.7, 6.8)
            await ShowErrorDialogAsync(validationResult.ErrorMessage);

            // 権限エラーの場合は、ユーザーに別のフォルダを選択させる (Requirement 8.9)
            // (何もしない = ダイアログを閉じて元の画面に戻る)
            return;
        }

        // 検証成功: 保存先を更新
        viewModel.SaveDirectory = selectedPath;
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

    /// <summary>
    /// ディレクトリの書き込み権限とディスク容量を検証 (Requirement 8.8, 6.7, 6.8)
    /// </summary>
    private async Task<DirectoryValidationResult> ValidateDirectoryAsync(string path)
    {
        try
        {
            // ディレクトリが存在しない場合は作成を試みる (書き込み権限のチェック)
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            // テストファイルを作成して書き込み権限を確認
            var testFile = System.IO.Path.Combine(path, $".test_{Guid.NewGuid()}.tmp");
            try
            {
                await System.IO.File.WriteAllTextAsync(testFile, "test");
                System.IO.File.Delete(testFile);
            }
            catch (System.IO.IOException ex)
            {
                // ディスク容量不足の可能性 (Requirement 6.8)
                if (ex.HResult == -2147024784) // ERROR_DISK_FULL (0x80070070)
                {
                    return DirectoryValidationResult.Failure(
                        DirectoryValidationErrorType.DiskFull,
                        "ディスク容量が不足しています。\n不要なファイルを削除して空き容量を確保してください。"
                    );
                }

                // その他のI/Oエラー
                return DirectoryValidationResult.Failure(
                    DirectoryValidationErrorType.IOError,
                    $"保存先フォルダへのアクセス中にエラーが発生しました:\n{ex.Message}\n\n別のフォルダを選択してください。"
                );
            }
            catch (UnauthorizedAccessException)
            {
                // 書き込み権限がない (Requirement 8.9, 6.7)
                return DirectoryValidationResult.Failure(
                    DirectoryValidationErrorType.PermissionDenied,
                    "選択したフォルダには書き込み権限がありません。\n別のフォルダを選択してください。"
                );
            }

            return DirectoryValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            // ディレクトリ作成時の権限エラー (Requirement 8.9, 6.7)
            return DirectoryValidationResult.Failure(
                DirectoryValidationErrorType.PermissionDenied,
                "選択したフォルダには書き込み権限がありません。\n別のフォルダを選択してください。"
            );
        }
        catch (Exception ex)
        {
            // その他の予期しないエラー
            return DirectoryValidationResult.Failure(
                DirectoryValidationErrorType.Unknown,
                $"予期しないエラーが発生しました:\n{ex.Message}\n\n別のフォルダを選択してください。"
            );
        }
    }

    /// <summary>
    /// エラーダイアログを表示 (Requirement 6.7, 6.8, 8.9)
    /// </summary>
    private async Task ShowErrorDialogAsync(string message)
    {
        var dialog = new Window
        {
            Title = "エラー",
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Avalonia.Thickness(20, 8)
        };

        button.Click += (s, e) => dialog.Close();
        panel.Children.Add(button);

        dialog.Content = panel;
        await dialog.ShowDialog(this);
    }
}

/// <summary>
/// ディレクトリ検証結果
/// </summary>
internal record DirectoryValidationResult
{
    public bool IsValid { get; init; }
    public DirectoryValidationErrorType ErrorType { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;

    public static DirectoryValidationResult Success() => new() { IsValid = true };

    public static DirectoryValidationResult Failure(DirectoryValidationErrorType errorType, string message) =>
        new() { IsValid = false, ErrorType = errorType, ErrorMessage = message };
}

/// <summary>
/// ディレクトリ検証エラーの種類
/// </summary>
internal enum DirectoryValidationErrorType
{
    None,
    PermissionDenied,
    DiskFull,
    IOError,
    Unknown
}
