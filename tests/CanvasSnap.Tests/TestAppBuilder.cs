using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(CanvasSnap.Tests.TestAppBuilder))]

namespace CanvasSnap.Tests;

/// <summary>
/// Avalonia.Headlessテストセッションのセットアップ
/// [AvaloniaFact]属性のテストは、このビルダーで初期化されたヘッドレス環境のUIスレッド上で実行される
/// （Dispatcher.UIThread、IAssetLoader等のプラットフォームサービスが利用可能になる）
/// </summary>
/// <remarks>
/// CanvasSnap.Appではなく素のApplicationを使用する。
/// Appを使うとOnFrameworkInitializationCompletedで実サービス（MacHotkeyService等）が起動してしまうため。
/// </remarks>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<Application>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
