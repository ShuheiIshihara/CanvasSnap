using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CanvasSnap.Models;
using CanvasSnap.Services;

namespace CanvasSnap.Tests.Benchmarks;

/// <summary>
/// スクリーンキャプチャのパフォーマンスベンチマーク
/// Requirements: 11.1 (0.5秒以内のキャプチャ処理時間)
/// </summary>
/// <remarks>
/// 実行方法（コンソールから）:
/// 1. ベンチマーククラスを使用して実行:
///    using BenchmarkDotNet.Running;
///    var summary = BenchmarkRunner.Run&lt;ScreenCaptureBenchmarks&gt;();
///
/// または、BenchmarkSwitcherを使用:
///    var summary = BenchmarkRunner.Run(typeof(ScreenCaptureBenchmarks).Assembly, args);
///
/// 注意事項:
/// - macOS環境でのScreen Recording権限が必要
/// - 実際のscreencaptureコマンドを実行するため、CI/CD環境では実行不可
/// - ベンチマーク結果は BenchmarkDotNet.Artifacts ディレクトリに保存されます
///
/// Phase 1 MVP目標:
/// - 1920x1080領域: 0.5秒以内
/// - 0.7秒を超える場合: Phase 2でScreenCaptureKit APIへの移行を検討
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ScreenCaptureBenchmarks
{
    private MacScreenCaptureService? _service;
    private CaptureRegion _smallRegion = null!;
    private CaptureRegion _mediumRegion = null!;
    private CaptureRegion _largeRegion = null!;
    private CaptureRegion _fullHDRegion = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new MacScreenCaptureService();

        // 各種サイズのキャプチャ領域を定義
        _smallRegion = new CaptureRegion(0, 0, 640, 480);      // VGA
        _mediumRegion = new CaptureRegion(0, 0, 1280, 720);    // 720p
        _largeRegion = new CaptureRegion(0, 0, 1600, 900);     // 900p
        _fullHDRegion = new CaptureRegion(0, 0, 1920, 1080);   // 1080p (要件目標)
    }

    /// <summary>
    /// 640x480（VGA）領域のキャプチャベンチマーク
    /// </summary>
    [Benchmark]
    public async Task<byte[]> CaptureSmallRegion_640x480()
    {
        return await _service!.CaptureRegionAsync(_smallRegion);
    }

    /// <summary>
    /// 1280x720（720p）領域のキャプチャベンチマーク
    /// </summary>
    [Benchmark]
    public async Task<byte[]> CaptureMediumRegion_1280x720()
    {
        return await _service!.CaptureRegionAsync(_mediumRegion);
    }

    /// <summary>
    /// 1600x900（900p）領域のキャプチャベンチマーク
    /// </summary>
    [Benchmark]
    public async Task<byte[]> CaptureLargeRegion_1600x900()
    {
        return await _service!.CaptureRegionAsync(_largeRegion);
    }

    /// <summary>
    /// 1920x1080（Full HD）領域のキャプチャベンチマーク
    /// Requirements: 11.1 - 0.5秒以内の目標達成を確認
    /// </summary>
    [Benchmark(Description = "Full HD (1920x1080) - 要件目標: 0.5秒以内")]
    public async Task<byte[]> CaptureFullHDRegion_1920x1080()
    {
        return await _service!.CaptureRegionAsync(_fullHDRegion);
    }
}
