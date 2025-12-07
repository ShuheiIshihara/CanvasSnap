using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CanvasSnap.Exceptions;
using CanvasSnap.Models;
using Microsoft.Extensions.Logging;

namespace CanvasSnap.Services;

/// <summary>
/// キャプチャフロー全体を調整するオーケストレータ
/// Requirements: 1.1 (安全なスクリーンキャプチャ)
/// Requirements: 6.1-6.8 (ファイル保存)
/// Requirements: 10.1-10.4 (通知システム)
/// Requirements: 11.1 (パフォーマンス)
/// </summary>
/// <remarks>
/// 実装ノート:
/// - キャプチャトリガー受信からファイル保存までのエンドツーエンド処理
/// - エラーハンドリングと適切な通知方法の選択（クリティカル/軽微）
/// - リソースクリーンアップ（中間画像データのDispose）
/// - トランザクション境界: 1回のキャプチャ操作
/// - すべての例外をtry-catchでキャッチし、Result<string, CaptureError>に変換
/// - 例外は外部に漏らさない
/// </remarks>
public class CaptureOrchestrator
{
    private readonly IPermissionService _permissionService;
    private readonly IDisplayService _displayService;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CaptureOrchestrator> _logger;

    public CaptureOrchestrator(
        IPermissionService permissionService,
        IDisplayService displayService,
        IScreenCaptureService screenCaptureService,
        IImageProcessingService imageProcessingService,
        INotificationService notificationService,
        ILogger<CaptureOrchestrator> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// キャプチャフローを実行（権限確認、キャプチャ、マスク、保存、通知）
    /// </summary>
    /// <param name="settings">キャプチャ設定（領域、マスク、保存先）</param>
    /// <returns>成功時はファイルパス、失敗時はエラー情報</returns>
    public async Task<Result<string, CaptureError>> ExecuteCaptureAsync(CaptureSettings settings)
    {
        byte[]? imageData = null;

        try
        {
            // 1. 権限チェック
            if (!await _permissionService.CheckPermissionsAsync())
            {
                _logger.LogError("Permission denied: Required permissions not granted");
                await _notificationService.ShowCriticalErrorAsync("権限エラー", "必要な権限が付与されていません");
                return Result<string, CaptureError>.Failure(CaptureError.PermissionDenied);
            }

            // 2. ディスプレイ可用性確認
            var displayInfo = await _displayService.GetDisplayInfoAsync(settings.Region);
            if (displayInfo is null)
            {
                await _notificationService.ShowNotificationAsync("キャプチャ中止: ディスプレイが利用できません", NotificationType.Error);
                return Result<string, CaptureError>.Failure(CaptureError.DisplayUnavailable);
            }

            // 3. キャプチャ実行
            imageData = await _screenCaptureService.CaptureRegionAsync(settings.Region);

            // 4. マスク適用（有効時）
            if (settings.IsMaskEnabled && settings.MaskRegions.Any())
            {
                imageData = await _imageProcessingService.ApplyMaskAsync(imageData, settings.MaskRegions.ToArray());
            }

            // 5. ファイル保存
            var filePath = GenerateFilePath(settings.SaveDirectory);

            // ディレクトリが存在しない場合は作成
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, imageData);

            // 6. 通知
            await _notificationService.ShowNotificationAsync("スクリーンショットを保存しました", NotificationType.Info);

            _logger.LogInformation("Screenshot saved successfully to {FilePath}", filePath);
            return Result<string, CaptureError>.Success(filePath);
        }
        catch (PermissionDeniedException ex)
        {
            _logger.LogError(ex, "Permission denied during capture operation");
            await _notificationService.ShowCriticalErrorAsync("権限エラー", "必要な権限が付与されていません");
            return Result<string, CaptureError>.Failure(CaptureError.PermissionDenied);
        }
        catch (ScreenCaptureException ex)
        {
            _logger.LogWarning(ex, "Screen capture failed");
            await _notificationService.ShowNotificationAsync("キャプチャに失敗しました", NotificationType.Error);
            return Result<string, CaptureError>.Failure(CaptureError.CaptureFailed);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File save failed during capture operation");
            await _notificationService.ShowCriticalErrorAsync("保存エラー", "ファイルの保存に失敗しました");
            return Result<string, CaptureError>.Failure(CaptureError.SaveFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during capture operation");
            await _notificationService.ShowCriticalErrorAsync("エラー", "予期しないエラーが発生しました");
            return Result<string, CaptureError>.Failure(CaptureError.Unknown);
        }
        finally
        {
            // リソースクリーンアップ（必要に応じて）
            imageData = null;
        }
    }

    /// <summary>
    /// ファイルパスを生成（screenshot_yyyyMMdd_HHmmssfff.png形式）
    /// </summary>
    /// <param name="saveDirectory">保存先ディレクトリ</param>
    /// <param name="timestamp">ファイル名に使用する時刻（省略時は現在時刻）</param>
    /// <returns>生成されたファイルパス</returns>
    internal static string GenerateFilePath(string saveDirectory, DateTime? timestamp = null)
    {
        var timestampValue = timestamp ?? DateTime.Now;
        var timestampText = timestampValue.ToString("yyyyMMdd_HHmmssfff");
        var baseName = $"screenshot_{timestampText}";
        var extension = ".png";

        var candidate = Path.Combine(saveDirectory, $"{baseName}{extension}");
        var counter = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(saveDirectory, $"{baseName}_{counter:D3}{extension}");
            counter++;
        }

        return candidate;
    }
}
