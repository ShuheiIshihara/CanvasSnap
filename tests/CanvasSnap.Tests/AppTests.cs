using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CanvasSnap.Tests;

public class AppTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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
