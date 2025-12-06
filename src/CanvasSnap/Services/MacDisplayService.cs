using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CanvasSnap.Models;

namespace CanvasSnap.Services;

/// <summary>
/// macOS環境でのディスプレイサービス実装
/// Requirements: 2.7 (ディスプレイ一覧取得), 3.1 (座標系), 3.2 (ScaleFactor取得)
/// </summary>
/// <remarks>
/// 実装ノート:
/// - macOS環境ではNSScreen API相当の機能を提供
/// - Phase 1 MVPではシンプルな実装を提供し、Phase 2で完全なNSScreen統合を実装
/// - 座標系はプライマリディスプレイ左上を原点(0,0)として管理
/// - テスト環境ではフォールバック実装を使用
/// </remarks>
public class MacDisplayService : IDisplayService
{
    private readonly bool _isMacOS;

    public MacDisplayService()
    {
        _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    /// <summary>
    /// 接続されているすべてのディスプレイ情報を取得
    /// NSScreen.Screensに相当
    /// </summary>
    /// <remarks>
    /// Phase 1 MVP実装:
    /// - macOS環境では実際のディスプレイ情報を取得（将来的にNSScreen API統合）
    /// - テスト環境ではプライマリディスプレイのみを返す
    /// - Phase 2でNSScreen APIの完全統合を実装予定
    /// </remarks>
    public Task<IEnumerable<DisplayInfo>> GetAllDisplaysAsync()
    {
        // Phase 1 MVP: プライマリディスプレイのみをサポート
        // macOS環境での典型的なRetina Displayを想定
        // TODO Phase 2: NSScreen APIを使用して実際のディスプレイ情報を取得
        var displays = new List<DisplayInfo>
        {
            new DisplayInfo(
                Id: "0",
                Name: "Primary Display",
                X: 0,
                Y: 0,
                Width: _isMacOS ? 2880 : 1920,  // macOS Retina想定
                Height: _isMacOS ? 1800 : 1080,
                ScaleFactor: _isMacOS ? 2.0 : 1.0,  // macOS Retina: 2.0
                IsPrimary: true
            )
        };

        return Task.FromResult<IEnumerable<DisplayInfo>>(displays);
    }

    /// <summary>
    /// 指定されたキャプチャ領域が含まれるディスプレイ情報を取得
    /// </summary>
    public async Task<DisplayInfo?> GetDisplayInfoAsync(CaptureRegion region)
    {
        var displays = await GetAllDisplaysAsync();

        // キャプチャ領域の中心点がどのディスプレイに含まれるかチェック
        var centerX = region.X + region.Width / 2;
        var centerY = region.Y + region.Height / 2;

        foreach (var display in displays)
        {
            if (centerX >= display.X && centerX < display.X + display.Width &&
                centerY >= display.Y && centerY < display.Y + display.Height)
            {
                return display;
            }
        }

        // どのディスプレイにも含まれない場合はnull
        return null;
    }

    /// <summary>
    /// 論理座標を物理座標に変換
    /// HiDPI/Retina対応のスケーリング計算
    /// </summary>
    public PhysicalCoordinates LogicalToPhysical(LogicalCoordinates logical, double scaleFactor)
    {
        return new PhysicalCoordinates(
            X: (int)Math.Round(logical.X * scaleFactor),
            Y: (int)Math.Round(logical.Y * scaleFactor)
        );
    }

    /// <summary>
    /// 物理座標を論理座標に変換
    /// HiDPI/Retina対応のスケーリング計算
    /// </summary>
    public LogicalCoordinates PhysicalToLogical(PhysicalCoordinates physical, double scaleFactor)
    {
        return new LogicalCoordinates(
            X: (int)Math.Round(physical.X / scaleFactor),
            Y: (int)Math.Round(physical.Y / scaleFactor)
        );
    }

    /// <summary>
    /// ディスプレイ構成変更イベント
    /// NSWorkspace.Notifications.DidChangeScreenParametersNotificationに対応
    /// </summary>
    /// <remarks>
    /// TODO: macOS NSWorkspace通知の監視を実装
    /// 現在は手動発火のみサポート
    /// </remarks>
    public event EventHandler? DisplayConfigurationChanged;

    /// <summary>
    /// ディスプレイ構成変更イベントを発火（テスト用）
    /// </summary>
    protected virtual void OnDisplayConfigurationChanged()
    {
        DisplayConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}
