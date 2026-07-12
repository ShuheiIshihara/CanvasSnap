using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CanvasSnap.Tests;

/// <summary>
/// App.axamlのTrayIcon定義がIAssetLoader等のプラットフォームサービスを要求するため、
/// [AvaloniaFact]でヘッドレスAvalonia環境上で実行する
/// </summary>
public class AppTests
{
    [AvaloniaFact]
    public void App_Should_InitializeServiceProvider()
    {
        // Arrange
        var app = new App();
        app.Initialize();

        // Act
        var serviceProvider = app.Services;

        // Assert
        Assert.NotNull(serviceProvider);
    }

    [AvaloniaFact]
    public void App_Should_RegisterServicesBasedOnPlatform()
    {
        // Arrange
        var app = new App();
        app.Initialize();

        // Act
        var services = app.Services;

        // Assert
        Assert.NotNull(services);
        // macOS環境の場合、macOS固有のサービスが登録されていることを確認
        // 現時点ではサービスがないため、ServiceProviderが存在することのみ確認
    }

    [AvaloniaFact]
    public void ServiceProvider_Should_BeAccessibleThroughProperty()
    {
        // Arrange
        var app = new App();
        app.Initialize();

        // Act
        var services = app.Services;

        // Assert
        Assert.IsAssignableFrom<IServiceProvider>(services);
    }
}
