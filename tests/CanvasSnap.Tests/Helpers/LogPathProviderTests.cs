using System;
using System.IO;
using CanvasSnap.Helpers;
using Xunit;

namespace CanvasSnap.Tests.Helpers;

/// <summary>
/// LogPathProviderの単体テスト
/// Task 14.3: ログ出力先の解決
/// Requirements: 11.3
/// </summary>
public class LogPathProviderTests
{
    [Fact]
    public void GetLogDirectory_ShouldReturnPlatformSpecificPath()
    {
        // Act
        var dir = LogPathProvider.GetLogDirectory();

        // Assert
        Assert.False(string.IsNullOrEmpty(dir));

        if (OperatingSystem.IsMacOS())
        {
            // macOS: ~/Library/Logs/CanvasSnap
            Assert.EndsWith(Path.Combine("Library", "Logs", "CanvasSnap"), dir);
        }
        else
        {
            // Windows/その他: %LocalAppData%\CanvasSnap\Logs
            Assert.EndsWith(Path.Combine("CanvasSnap", "Logs"), dir);
        }
    }

    [Fact]
    public void GetLogFilePath_ShouldEndWithAppLog()
    {
        // Act
        var path = LogPathProvider.GetLogFilePath();

        // Assert
        Assert.EndsWith("app.log", path);
        Assert.Equal(LogPathProvider.GetLogDirectory(), Path.GetDirectoryName(path));
    }
}
