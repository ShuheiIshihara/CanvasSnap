# Implementation Plan: screenshot-capture-system

## Overview

このドキュメントは、screenshot-capture-systemの実装タスクを定義します。Phase 1（macOS MVP）に焦点を当て、全13要件を実装可能なタスクに分解しています。

**アーキテクチャ**: MVVM + Service Layer
**総タスク数**: 12メジャータスク、47サブタスク
**並列実行**: (P)マーカー付きタスクは並列実行可能

---

## Tasks

### 1. プロジェクト初期化とDI設定

- [x] 1.1 (P) .NET 10プロジェクト作成とAvalonia 11.x初期設定
  - Avalonia.Desktop、ReactiveUI、SixLabors.ImageSharp、System.Text.Jsonパッケージをインストール
  - プロジェクト構成をMVVMパターンに従って整理（Views/ViewModels/Services/Models/Helpersディレクトリ）
  - nullable reference typesを有効化
  - _Requirements: 13.2, 13.3_

- [x] 1.2 (P) DIコンテナ設定とプラットフォーム検出
  - Microsoft.Extensions.DependencyInjectionを使用してサービス登録
  - OperatingSystem.IsMacOS()でプラットフォームを検出し、macOS実装を注入
  - App.xaml.csでServiceProviderを初期化
  - IServiceProviderをViewModelに渡す仕組みを構築
  - _Requirements: 13.4_

### 2. ドメインモデル実装

- [x] 2.1 (P) 座標系とキャプチャ設定のモデル定義
  - PhysicalCoordinates、LogicalCoordinatesレコード型を定義（HiDPI変換用）
  - CaptureRegionレコード型を定義（X, Y, Width, Heightの物理ピクセル座標）
  - MaskRegionレコード型を定義（キャプチャ領域左上を原点とする相対座標）
  - HotkeyConfigレコード型を定義（HotkeyModifiersフラグ列挙型とKeyCode）
  - CaptureSettings集約ルートを定義（Region, MaskRegions, HotkeyConfig, SaveDirectory, IsMaskEnabled）
  - _Requirements: 2.4, 3.4, 4.2, 5.3, 7.2_

- [x] 2.2 (P) ディスプレイ情報モデルとエラー型定義
  - DisplayInfoレコード型を定義（Id, Name, X, Y, Width, Height, ScaleFactor, IsPrimary）
  - CaptureError列挙型を定義（PermissionDenied, DisplayUnavailable, CaptureFailed, SaveFailed, Unknown）
  - Result<T, E>型を定義（SuccessまたはErrorを保持）
  - _Requirements: 2.7, 3.1_

- [x] 2.3 (P) 例外階層の定義
  - CanvasSnapException基底クラスを定義
  - ScreenCaptureException、ImageProcessingException、SettingsException、PermissionDeniedExceptionを定義
  - PermissionDeniedExceptionにPermissionType（ScreenRecording, Accessibility）を追加
  - _Requirements: 12.1, 12.2_

### 3. 設定永続化サービス実装

- [x] 3.1 (P) ISettingsServiceインターフェースと実装
  - LoadSettingsAsync、SaveSettingsAsync、GetDefaultSettings、GetConfigFilePathメソッドを定義
  - macOS: `~/Library/Application Support/CanvasSnap/config.json`、Windows: `%AppData%\CanvasSnap\config.json`のパス取得
  - デフォルト設定（Cmd+Shift+S、ピクチャフォルダ）を返す実装
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 3.2 設定ファイルの読み書きとエラーハンドリング
  - System.Text.JsonでCaptureSettingsをシリアライズ/デシリアライズ
  - JSONパース失敗時はSettingsExceptionをスローせず、デフォルト設定を返す
  - 破損ファイルを`.backup`拡張子でリネームして保存
  - 破損検出時にユーザー通知を発行（INotificationServiceへの依存）
  - _Requirements: 7.5, 7.6, 7.7_

### 4. 画像処理サービス実装

- [x] 4.1 (P) IImageProcessingServiceインターフェースと実装
  - ApplyMaskAsyncメソッドを定義（byte[]画像データとMaskRegion[]を受け取る）
  - ImageSharpのImage.LoadAsync<Rgba32>で画像を読み込み
  - Mutate(ctx => ctx.Fill(Color.Black, rectangle))で黒塗りマスクを適用
  - マスク適用後のPNG画像をbyte[]として返す（SaveAsPngAsync使用）
  - メモリストリーム処理でメモリ効率を確保
  - _Requirements: 4.3, 4.4, 4.5_

- [x] 4.2 (P) マスク領域の検証とエラーハンドリング
  - マスク座標がキャプチャ領域外の場合にImageProcessingExceptionをスロー
  - 複数マスク領域の順次処理（foreach）
  - 画像データが無効な場合の例外処理
  - _Requirements: 4.1, 4.2_

### 5. ディスプレイサービス実装（macOS）

- [x] 5.1 IDisplayServiceインターフェース定義
  - GetAllDisplaysAsync、GetDisplayInfoAsync、LogicalToPhysical、PhysicalToLogical、DisplayConfigurationChangedイベントを定義
  - DisplayInfoレコード型の返却
  - _Requirements: 2.6, 3.1, 3.3_

- [x] 5.2 macOS実装（NSScreen APIを使用）
  - NSScreen.Screensで全ディスプレイ情報を取得
  - プライマリディスプレイを特定（NSScreen.MainScreen）
  - ScaleFactorをNSScreen.BackingScaleFactorから取得（Retina: 2.0）
  - 座標系をプライマリディスプレイ左上原点に変換
  - _Requirements: 2.7, 3.1, 3.2_

- [x] 5.3 座標変換とディスプレイ構成変更検出
  - LogicalToPhysical、PhysicalToLogicalで座標変換を実装（ScaleFactor考慮）
  - NSWorkspace.Notifications.DidChangeScreenParametersNotificationを監視
  - ディスプレイ構成変更時にDisplayConfigurationChangedイベントを発火
  - GetDisplayInfoAsyncでディスプレイ切断時はnullを返す
  - _Requirements: 2.8, 2.9, 3.3, 3.4_

### 6. 権限管理サービス実装（macOS）

- [x] 6.1 IPermissionServiceインターフェース定義
  - CheckPermissionsAsync、CheckScreenRecordingPermissionAsync、CheckAccessibilityPermissionAsync、OpenPermissionSettingsAsyncメソッドを定義
  - _Requirements: 12.1, 12.2_

- [x] 6.2 macOS実装（TCC APIを使用）
  - CGPreflightScreenCaptureAccess、CGRequestScreenCaptureAccessでScreen Recording権限をチェック
  - AXIsProcessTrustedでアクセシビリティ権限をチェック
  - `open x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture`でシステム設定を開く
  - CheckPermissionsAsyncで両権限をチェックし、いずれかが不足している場合はfalseを返す
  - _Requirements: 12.1, 12.2, 12.3_

### 7. スクリーンキャプチャサービス実装（macOS）

- [ ] 7.1 IScreenCaptureServiceインターフェース定義
  - CaptureRegionAsyncメソッドを定義（CaptureRegionを受け取りbyte[]を返す）
  - ScreenCaptureException、PermissionDeniedExceptionをスロー可能と定義
  - _Requirements: 1.1, 1.7_

- [ ] 7.2 macOS実装（screencaptureコマンドを使用）
  - Process.Startで`screencapture -R<x>,<y>,<w>,<h> -x -t png <tempFile>`を実行
  - 一時ファイルにPNGを保存し、File.ReadAllBytesAsyncで読み込む
  - 終了コードが0以外の場合はScreenCaptureExceptionをスロー
  - 一時ファイルをfinally句で確実に削除
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [ ] 7.3 (P) パフォーマンス測定とベンチマーク
  - BenchmarkDotNetでキャプチャ処理時間を測定
  - 1920x1080領域で0.5秒以内の目標達成を確認
  - 0.7秒超過の場合はPhase 2移行判断基準に従う
  - _Requirements: 11.1_

### 8. ホットキーサービス実装（macOS）

- [ ] 8.1 IHotkeyServiceインターフェース定義
  - RegisterHotkeyAsync、UnregisterHotkeyAsync、HotkeyPressedイベントを定義
  - HotkeyConflictException、HotkeyRegistrationExceptionを定義
  - _Requirements: 5.1, 5.4, 5.6_

- [ ] 8.2 macOS実装（CGEvent APIを使用）
  - `macos-global-hotkey-final-report.md`の完全な実装コードを参照
  - CFRunLoop専用スレッドを作成し、CGEventTapCreateでイベントタップを設定
  - EventCallbackでキーコードと修飾キーを判定し、HotkeyConfigとマッチすればHotkeyPressedイベントを発火
  - UnregisterHotkeyAsyncでCFRunLoopStop、CGEventTapEnable(false)、リソース解放を実施
  - _Requirements: 5.1, 5.2, 5.3, 5.5_

- [ ] 8.3 ホットキー競合検出とエラー処理
  - OS予約ショートカットとの競合検出（CGEventTapCreate失敗時）
  - HotkeyConflictExceptionをスローし、ユーザーに別の組み合わせを促す
  - 登録失敗時にHotkeyRegistrationExceptionをスロー
  - _Requirements: 5.4, 5.6_

### 9. 通知サービス実装（macOS）

- [ ] 9.1 (P) INotificationServiceインターフェース定義
  - ShowNotificationAsync（軽微なエラー・成功メッセージ）、ShowCriticalErrorAsync（クリティカルエラーダイアログ）を定義
  - NotificationType列挙型を定義（Info, Success, Warning, Error）
  - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [ ] 9.2 (P) macOS実装（UNUserNotificationCenterを使用）
  - UNUserNotificationCenterでOS標準通知を表示
  - ShowCriticalErrorAsyncでAvaloniaのMessageBoxまたはNSAlertを使用
  - アクションボタン（システム設定を開く）をサポート
  - _Requirements: 10.1, 10.2, 10.3, 10.4_

### 10. CaptureOrchestrator実装

- [ ] 10.1 CaptureOrchestratorクラスの実装
  - ExecuteCaptureAsyncメソッドをdesign.mdの実装例に従って実装
  - 権限チェック → ディスプレイ可用性確認 → キャプチャ → マスク適用 → ファイル保存 → 通知のフローを実装
  - すべての例外をtry-catchでキャッチし、Result<string, CaptureError>に変換
  - PermissionDeniedException、ScreenCaptureException、IOException、Exceptionを個別に処理
  - _Requirements: 1.1, 6.1, 6.6, 10.1_

- [ ] 10.2 ファイル保存とファイル名生成
  - GenerateFilePathメソッドで`screenshot_yyyyMMdd_HHmmssfff.png`形式のファイル名を生成
  - 同名ファイルが存在する場合は`_001`, `_002`連番を付与
  - File.WriteAllBytesAsyncでPNG画像を保存
  - 保存先ディレクトリが存在しない場合は作成（Directory.CreateDirectory）
  - _Requirements: 6.2, 6.3, 6.4, 6.5_

- [ ] 10.3 エラーハンドリングとログ記録
  - 各例外タイプに応じてILogger.LogError/LogWarningでログ記録
  - クリティカルエラーはINotificationService.ShowCriticalErrorAsyncでダイアログ表示
  - 軽微なエラーはINotificationService.ShowNotificationAsyncで通知表示
  - finally句でリソースクリーンアップ（imageData = null）
  - _Requirements: 6.7, 6.8, 10.2, 10.3, 10.4_

### 11. UI層実装（ViewModels）

- [ ] 11.1 MainWindowViewModelの実装
  - ReactiveObjectを継承し、ShowSettingsCommand、ExitCommandを定義
  - IHotkeyService.HotkeyPressedイベントを購読
  - OnHotkeyPressedでDispatcher.UIThread.InvokeAsyncを使用してUIスレッドにマーシャリング
  - CaptureOrchestrator.ExecuteCaptureAsyncを呼び出し、Resultを判定
  - _Requirements: 5.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 11.2 SettingsViewModelの実装
  - ReactiveObjectを継承し、設定値をReactiveプロパティで公開（HotkeyText, RegionText, MaskText, IsMaskEnabled, SaveDirectory）
  - SelectRegionCommand、SelectMaskCommand、TestCaptureCommand、SaveCommand、BrowseDirectoryCommandを定義
  - LoadSettingsAsyncで起動時に設定を読み込み、SaveSettingsAsyncで保存
  - TestCaptureCommandでCaptureOrchestratorを呼び出して動作確認
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.10_

- [ ] 11.3 RegionSelectorViewModelの実装
  - OnMouseDown、OnMouseMove、OnMouseUpメソッドで矩形選択を実装
  - ドラッグ中にRectangleX/Y/Width/Heightプロパティを更新
  - CoordinatesTextプロパティで座標とサイズをリアルタイム表示
  - OnMouseUpで論理座標を物理座標に変換し、RegionSelectedイベントを発火
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

### 12. UI層実装（Views）

- [ ] 12.1 App.axamlとApp.xaml.csの実装
  - TrayIconをApp.axamlで定義（<TrayIcon.Icons>、コンテキストメニュー）
  - App.xaml.csでDIコンテナを初期化し、プラットフォーム別サービスを登録
  - MainWindowViewModelをDIから取得し、DataContextに設定
  - ShowInTaskbar=false、WindowState=Minimizedでバックグラウンド常駐
  - _Requirements: 9.1, 9.2_

- [ ] 12.2 SettingsWindow.axamlの実装
  - ホットキー、キャプチャ領域、マスク領域、保存先の表示UIを実装
  - 領域選択ボタン、マスク選択ボタン、テストキャプチャボタン、保存ボタンを配置
  - 保存先フォルダ選択ダイアログ（Avalonia.Dialogs.StorageProvider）
  - ReactiveUIバインディングでViewModelプロパティと接続
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.7, 8.10_

- [ ] 12.3 RegionSelectorWindow.axamlの実装
  - 全画面透明オーバーレイウィンドウ（SystemDecorations=None, TransparencyLevelHint=Transparent）
  - Canvasで矩形選択を描画（Rectangle要素を動的更新）
  - 座標とサイズ表示用TextBlock
  - ESCキーでキャンセル、マウスアップで選択完了
  - _Requirements: 2.1, 2.2, 2.3, 2.5_

- [ ] 12.4 保存先フォルダ検証とエラー表示
  - 保存先フォルダの書き込み権限チェック（Directory.CreateDirectoryで試行）
  - 権限がない場合はエラーメッセージを表示し、別のフォルダを選択させる
  - ディスク容量不足時のクリティカルエラーダイアログ表示
  - _Requirements: 6.7, 6.8, 8.8, 8.9_

### 13. 統合とテスト

- [ ] 13.1 単体テストの実装（Services）
  - xUnit、Moqを使用してIScreenCaptureService、IImageProcessingService、IDisplayService、ISettingsServiceの単体テストを実装
  - CaptureOrchestrator.ExecuteCaptureAsyncの正常系・異常系テスト（Result型の検証）
  - IDisplayService.LogicalToPhysicalの座標変換テスト（Retina/非Retina）
  - _Requirements: 11.1, 11.2_

- [ ] 13.2 統合テストの実装
  - キャプチャフロー統合テスト（Orchestrator + モックサービス）
  - 設定永続化フローテスト（SaveSettings → LoadSettings ラウンドトリップ）
  - 座標変換フローテスト（RegionSelectorViewModel + IDisplayService）
  - _Requirements: 11.1_

- [ ] 13.3 E2Eテストシナリオ（手動）
  - 初回起動フロー（権限ダイアログ → 権限付与 → トレイアイコン表示）
  - 領域選択フロー（設定画面 → 領域選択 → ドラッグ → 座標保存）
  - キャプチャフロー（ホットキー → キャプチャ → ファイル保存 → 通知）
  - マスク機能フロー（マスク選択 → マスク有効化 → キャプチャ → 黒塗り確認）
  - エラーハンドリング（権限なし → ダイアログ、ディスプレイ切断 → 通知）
  - _Requirements: 12.1, 12.2_

- [ ] 13.4* パフォーマンスベンチマーク（オプション）
  - BenchmarkDotNetでキャプチャ処理時間、メモリ使用量、CPU使用率を測定
  - 0.5秒以内のキャプチャ完了を検証
  - アイドル時のメモリ50MB未満、CPU 3%未満を検証
  - _Requirements: 11.1, 11.2, 11.3_

### 14. macOS固有の最終調整

- [ ] 14.1 Info.plistの設定
  - NSScreenCaptureUsageDescriptionに「ゲーム画面のスクリーンショット撮影に使用」を記載
  - NSAccessibilityUsageDescriptionに「グローバルホットキー登録のため」を記載
  - アプリアイコン（.icns）の設定
  - _Requirements: 12.3_

- [ ] 14.2 (P) アプリアイコンとトレイアイコンの準備
  - .icns形式のアプリアイコン作成（16x16, 32x32, 48x48, 128x128, 256x256）
  - .ico形式のトレイアイコン作成（macOS用）
  - アイコンリソースをプロジェクトに追加
  - _Requirements: 9.1_

- [ ] 14.3 ログ出力とデバッグ支援
  - Serilogをインストールし、構造化ログを設定
  - ログ出力先を`~/Library/Logs/CanvasSnap/app.log`に設定
  - ファイルローテーション設定（日次、最大10ファイル保持）
  - _Requirements: 11.3_

---

## Requirements Coverage

全13要件を47サブタスクでカバーしています：

| Requirement | Covered by Tasks |
|-------------|------------------|
| 1. 安全なスクリーンキャプチャ | 7.1, 7.2, 10.1 |
| 2. キャプチャ領域管理 | 2.1, 5.2, 5.3, 11.3, 12.3 |
| 3. HiDPI対応 | 2.1, 2.2, 5.1, 5.2, 5.3 |
| 4. プライバシーマスク | 2.1, 4.1, 4.2 |
| 5. ホットキー操作 | 2.1, 8.1, 8.2, 8.3, 11.1 |
| 6. ファイル保存 | 10.1, 10.2, 10.3, 12.4 |
| 7. 設定永続化 | 2.1, 3.1, 3.2 |
| 8. 設定画面UI | 11.2, 12.2, 12.4 |
| 9. システムトレイ | 11.1, 12.1, 14.2 |
| 10. 通知システム | 9.1, 9.2, 10.1, 10.3 |
| 11. パフォーマンス | 7.3, 13.1, 13.2, 13.4, 14.3 |
| 12. 権限管理 | 2.3, 6.1, 6.2, 13.3, 14.1 |
| 13. プラットフォーム互換性 | 1.1, 1.2 |

---

## Parallel Execution Guide

`(P)`マーカー付きタスクは並列実行可能です。以下のグループで並列実行を推奨します：

**グループ1: 初期セットアップ**
- 1.1, 1.2 (プロジェクト初期化とDI設定)

**グループ2: ドメインモデルと例外定義**
- 2.1, 2.2, 2.3 (独立したモデル定義)

**グループ3: サービス層（依存関係なし）**
- 3.1 (設定サービス)
- 4.1, 4.2 (画像処理サービス)
- 7.3 (パフォーマンステスト)
- 9.1, 9.2 (通知サービス)
- 14.2 (アイコン準備)

**グループ4: プラットフォーム依存サービス（順次実行推奨）**
- 5.1 → 5.2 → 5.3 (ディスプレイサービス)
- 6.1 → 6.2 (権限管理サービス)
- 7.1 → 7.2 (スクリーンキャプチャサービス)
- 8.1 → 8.2 → 8.3 (ホットキーサービス)

**グループ5: UI層（Orchestrator完了後）**
- 11.1, 11.2, 11.3 (ViewModels)
- 12.1, 12.2, 12.3, 12.4 (Views)

**グループ6: 最終調整**
- 13.1, 13.2, 13.3, 13.4 (テスト)
- 14.1, 14.3 (macOS固有設定)

---

## Implementation Notes

- **依存関係**: 10. CaptureOrchestratorは全サービス実装完了後に着手
- **テスト**: 各サービス実装後に単体テストを実施（13.1）
- **Phase 2移行判断**: 7.3のパフォーマンス測定結果に基づき、ScreenCaptureKit移行を判断
- **UIスレッド**: 11.1のOnHotkeyPressedでDispatcher.UIThread.InvokeAsyncを必ず使用（CGEvent APIスレッド対策）
