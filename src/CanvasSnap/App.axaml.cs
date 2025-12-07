using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CanvasSnap.ViewModels;
using CanvasSnap.Views;
using CanvasSnap.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CanvasSnap;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;
    private MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // ロギング設定
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // プラットフォーム検出とサービス登録
        if (OperatingSystem.IsMacOS())
        {
            // macOS固有のサービス登録
            services.AddSingleton<IScreenCaptureService, MacScreenCaptureService>();
            services.AddSingleton<IHotkeyService, MacHotkeyService>();
            services.AddSingleton<IDisplayService, MacDisplayService>();
            services.AddSingleton<IPermissionService, MacPermissionService>();
            services.AddSingleton<INotificationService, MacNotificationService>();
        }
        else if (OperatingSystem.IsWindows())
        {
            // Windows固有のサービス登録（Phase 2）
            throw new PlatformNotSupportedException("Windows support is planned for Phase 2");
        }

        // 共通サービスの登録
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICaptureOrchestrator, CaptureOrchestrator>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();

        Services = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // MainWindowViewModelを取得して保持
            _mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();

            // バックグラウンド常駐設定 (Requirement 9.1, 9.2)
            // ShowInTaskbar=false: タスクバーに表示しない
            // WindowState=Minimized: 最小化状態で起動
            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel,
                ShowInTaskbar = false,
                WindowState = WindowState.Minimized,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    /// <summary>
    /// トレイアイコンの「設定」メニュークリックハンドラ (Requirement 9.3, 9.4)
    /// </summary>
    private void OnShowSettings(object? sender, EventArgs e)
    {
        _mainWindowViewModel?.ShowSettingsCommand.Execute(null);
    }

    /// <summary>
    /// トレイアイコンの「終了」メニュークリックハンドラ (Requirement 9.5)
    /// </summary>
    private void OnExit(object? sender, EventArgs e)
    {
        _mainWindowViewModel?.ExitCommand.Execute(null);
    }
}