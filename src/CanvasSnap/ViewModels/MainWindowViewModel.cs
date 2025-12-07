using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CanvasSnap.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CanvasSnap.ViewModels;

/// <summary>
/// MainWindowのViewModel
/// Task 11.1: トレイアイコン制御、ホットキーイベント受信、設定ウィンドウ起動
/// Requirements: 9.1-9.5 (システムトレイ統合)
/// </summary>
public class MainWindowViewModel : ViewModelBase
    , IDisposable
{
    private readonly ICaptureOrchestrator _orchestrator;
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private bool _disposed;

    public MainWindowViewModel(
        ICaptureOrchestrator orchestrator,
        IHotkeyService hotkeyService,
        ISettingsService settingsService,
        ILogger<MainWindowViewModel> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // コマンドの初期化
        ShowSettingsCommand = ReactiveCommand.Create(ShowSettings);
        ExitCommand = ReactiveCommand.Create(Exit);

        // ホットキーイベントを購読
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
    }

    /// <summary>
    /// 設定ウィンドウ表示コマンド
    /// </summary>
    public ICommand ShowSettingsCommand { get; }

    /// <summary>
    /// アプリケーション終了コマンド
    /// </summary>
    public ICommand ExitCommand { get; }

    /// <summary>
    /// ホットキー押下イベントハンドラ
    /// </summary>
    /// <remarks>
    /// ⚠️ 重要: HotkeyPressedイベントはバックグラウンドスレッド（CFRunLoopスレッド）で発火
    /// UI操作や通知表示を行う前に、必ずUIスレッドにマーシャリングする
    /// </remarks>
    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // UIスレッドにマーシャリング
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    var settings = await _settingsService.LoadSettingsAsync();
                    var result = await _orchestrator.ExecuteCaptureAsync(settings);

                    // resultの判定（UI更新が必要な場合はここで実行）
                    if (result.IsError)
                    {
                        // エラー通知はOrchestratorが既に実施済み（設計方針による）
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute capture on hotkey");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch hotkey handler to UI thread");
        }
    }

    /// <summary>
    /// 設定ウィンドウを表示
    /// </summary>
    private void ShowSettings()
    {
        // TODO: Task 11.2でSettingsViewModelを実装後、ここで設定ウィンドウを表示
        // var settingsWindow = new SettingsWindow
        // {
        //     DataContext = new SettingsViewModel(_settingsService, ...)
        // };
        // settingsWindow.Show();
    }

    /// <summary>
    /// アプリケーションを終了
    /// </summary>
    private void Exit()
    {
        Dispose();
        Environment.Exit(0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
