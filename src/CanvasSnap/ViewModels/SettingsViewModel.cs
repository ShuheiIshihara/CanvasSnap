using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CanvasSnap.Models;
using CanvasSnap.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CanvasSnap.ViewModels;

/// <summary>
/// 設定画面のViewModel
/// Task 11.2: 設定画面ロジック、領域選択トリガー、設定保存
/// Requirements: 8.1-8.10 (設定画面UI)
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDisplayService _displayService;
    private readonly IHotkeyService _hotkeyService;
    private readonly ICaptureOrchestrator _orchestrator;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SettingsViewModel> _logger;
    private CaptureSettings _currentSettings;

    /// <summary>
    /// DisplayService（Viewから領域選択時に使用）
    /// </summary>
    public IDisplayService DisplayService => _displayService;

    public SettingsViewModel(
        ISettingsService settingsService,
        IDisplayService displayService,
        IHotkeyService hotkeyService,
        ICaptureOrchestrator orchestrator,
        INotificationService notificationService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
        _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentSettings = new CaptureSettings
        {
            Region = new CaptureRegion(0, 0, 100, 100),
            MaskRegions = [],
            HotkeyConfig = new HotkeyConfig(HotkeyModifiers.Control | HotkeyModifiers.Shift, 83),
            SaveDirectory = "/tmp",
            IsMaskEnabled = false
        };

        // コマンドの初期化
        SelectRegionCommand = ReactiveCommand.CreateFromTask(SelectRegionAsync, outputScheduler: RxApp.MainThreadScheduler);
        SelectMaskCommand = ReactiveCommand.CreateFromTask(SelectMaskAsync, outputScheduler: RxApp.MainThreadScheduler);
        TestCaptureCommand = ReactiveCommand.CreateFromTask(TestCaptureAsync, outputScheduler: RxApp.MainThreadScheduler);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync, outputScheduler: RxApp.MainThreadScheduler);
        BrowseDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseDirectoryAsync, outputScheduler: RxApp.MainThreadScheduler);
    }

    /// <summary>
    /// ホットキー表示テキスト
    /// </summary>
    [Reactive]
    public string HotkeyText { get; set; } = string.Empty;

    /// <summary>
    /// キャプチャ領域表示テキスト
    /// </summary>
    [Reactive]
    public string RegionText { get; set; } = string.Empty;

    /// <summary>
    /// マスク領域表示テキスト
    /// </summary>
    [Reactive]
    public string MaskText { get; set; } = string.Empty;

    /// <summary>
    /// マスク機能有効フラグ
    /// </summary>
    [Reactive]
    public bool IsMaskEnabled { get; set; }

    /// <summary>
    /// 保存先ディレクトリ
    /// </summary>
    [Reactive]
    public string SaveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 領域選択コマンド
    /// </summary>
    public ICommand SelectRegionCommand { get; }

    /// <summary>
    /// マスク領域選択コマンド
    /// </summary>
    public ICommand SelectMaskCommand { get; }

    /// <summary>
    /// テストキャプチャコマンド
    /// </summary>
    public ICommand TestCaptureCommand { get; }

    /// <summary>
    /// 設定保存コマンド
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 保存先フォルダ選択コマンド
    /// </summary>
    public ICommand BrowseDirectoryCommand { get; }

    /// <summary>
    /// 設定を読み込む
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        _currentSettings = await _settingsService.LoadSettingsAsync();
        UpdateDisplayFromSettings();
    }

    /// <summary>
    /// 非同期初期化。呼び出し側でawaitしてエラー伝播を保証する。
    /// </summary>
    public Task InitializeAsync() => LoadSettingsAsync();

    /// <summary>
    /// 設定から表示を更新
    /// </summary>
    private void UpdateDisplayFromSettings()
    {
        RegionText = $"({_currentSettings.Region.X}, {_currentSettings.Region.Y}, {_currentSettings.Region.Width}, {_currentSettings.Region.Height})";
        SaveDirectory = _currentSettings.SaveDirectory;
        IsMaskEnabled = _currentSettings.IsMaskEnabled;

        // ホットキーテキストの生成
        var modifiers = _currentSettings.HotkeyConfig.Modifiers;
        var parts = new System.Collections.Generic.List<string>();

        if (modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");

        parts.Add($"Key{_currentSettings.HotkeyConfig.KeyCode}");
        HotkeyText = string.Join("+", parts);

        // マスクテキストの生成
        if (_currentSettings.MaskRegions.Count() > 0)
        {
            var mask = _currentSettings.MaskRegions.First();
            MaskText = $"({mask.X}, {mask.Y}, {mask.Width}, {mask.Height})";
        }
        else
        {
            MaskText = "未設定";
        }
    }

    /// <summary>
    /// 領域選択を開始（実装はView側で行う）
    /// </summary>
    private async Task SelectRegionAsync()
    {
        // Note: 実際の領域選択UIはSettingsWindow.axaml.csのOnSelectRegionで処理
        await Task.CompletedTask;
    }

    /// <summary>
    /// マスク領域選択を開始（実装はView側で行う）
    /// </summary>
    private async Task SelectMaskAsync()
    {
        // Note: 実際のマスク選択UIはSettingsWindow.axaml.csのOnSelectMaskで処理
        await Task.CompletedTask;
    }

    /// <summary>
    /// キャプチャ領域を更新（Viewから呼び出される）
    /// </summary>
    public void UpdateRegion(CaptureRegion region)
    {
        _currentSettings = _currentSettings with { Region = region };
        UpdateDisplayFromSettings();
    }

    /// <summary>
    /// マスク領域を追加（Viewから呼び出される）
    /// </summary>
    public void AddMaskRegion(MaskRegion maskRegion)
    {
        _currentSettings = _currentSettings with { MaskRegions = [maskRegion] };
        UpdateDisplayFromSettings();
    }

    /// <summary>
    /// テストキャプチャを実行
    /// </summary>
    private async Task TestCaptureAsync()
    {
        var testSettings = CreateSettingsFromUI();
        var result = await _orchestrator.ExecuteCaptureAsync(testSettings);

        // UIスレッドで結果処理を実行
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync($"テストキャプチャを保存しました: {result.Value}", NotificationType.Success);
            }
            else
            {
                var message = result.Error switch
                {
                    CaptureError.PermissionDenied => "必要な権限がありません",
                    CaptureError.DisplayUnavailable => "ディスプレイが利用できません",
                    CaptureError.CaptureFailed => "キャプチャに失敗しました",
                    CaptureError.SaveFailed => "保存に失敗しました",
                    _ => "不明なエラーが発生しました"
                };

                await _notificationService.ShowNotificationAsync($"テストキャプチャ失敗: {message}", NotificationType.Error);
            }
        });
    }

    /// <summary>
    /// 設定を保存
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        var settings = CreateSettingsFromUI();

        try
        {
            await _settingsService.SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await _notificationService.ShowNotificationAsync("設定の保存に失敗しました", NotificationType.Error);
            });
            return;
        }

        try
        {
            // ホットキーを再登録
            await _hotkeyService.RegisterHotkeyAsync(settings.HotkeyConfig);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await _notificationService.ShowNotificationAsync("設定を保存しました", NotificationType.Success);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register hotkey after saving settings");
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await _notificationService.ShowNotificationAsync("設定は保存しましたが、ホットキーの登録に失敗しました", NotificationType.Warning);
            });
        }
    }

    /// <summary>
    /// 保存先フォルダを選択（実装はView側で行う）
    /// </summary>
    private async Task BrowseDirectoryAsync()
    {
        // Note: 実際のフォルダ選択はSettingsWindow.axaml.csのOnBrowseDirectoryで処理
        await Task.CompletedTask;
    }

    /// <summary>
    /// UI状態から設定オブジェクトを作成
    /// </summary>
    private CaptureSettings CreateSettingsFromUI()
    {
        return new CaptureSettings
        {
            Region = _currentSettings.Region,
            MaskRegions = _currentSettings.MaskRegions,
            HotkeyConfig = _currentSettings.HotkeyConfig,
            SaveDirectory = SaveDirectory,
            IsMaskEnabled = IsMaskEnabled
        };
    }
}
