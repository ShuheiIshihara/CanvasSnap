# Research & Design Decisions

---
**目的**: 技術設計を裏付ける調査結果、アーキテクチャ評価、設計決定の根拠を記録する。

**使用法**:
- ディスカバリーフェーズの調査活動と成果を記録
- `design.md` に含めるには詳細すぎる設計判断のトレードオフを文書化
- 将来の監査や再利用のための参照と証拠を提供
---

## Summary
- **Feature**: `screenshot-capture-system`
- **Discovery Scope**: Full (greenfield - 新規プロジェクト)
- **Key Findings**:
  - CGEvent API（Quartz Event Services）によるグローバルホットキー実装が最適（Carbon Framework非推奨のため）
  - Avalonia 11.xのTrayIcon/NativeMenuはXAMLまたはプログラマティック定義が可能
  - macOS screencaptureコマンドがPhase 1 MVPの高速実装手段として有効
  - ImageSharpのRectangle操作はミューターブル設計でパフォーマンス最適化済み
  - MVVM + Service Layerアーキテクチャによるプラットフォーム抽象化がmacOS/Windows対応に最適

## Research Log

### Avalonia TrayIcon/NativeMenu実装パターン
- **Context**: システムトレイアイコンとコンテキストメニューの実装方法調査（Req 9対応）
- **Sources Consulted**:
  - [Avalonia UI Official Docs - TrayIcon](https://docs.avaloniaui.net/docs/reference/controls/tray-icon)
  - [GitHub Discussion - TrayIcon Best Practices](https://github.com/AvaloniaUI/Avalonia/discussions/8234)
- **Findings**:
  - **XAMLパターン**: `App.axaml`で`<TrayIcon.Icons>`コレクションを定義
  - **プログラマティックパターン**: `App.xaml.cs`で`TrayIcons.Add(new TrayIcon())`を使用
  - **NativeMenu**: `<TrayIcon.Menu>`でメニュー項目を宣言、クリックイベントをViewModelにバインド
  - **アイコン形式**: .ico形式（複数解像度含む）が必須、16x16/32x32/48x48を推奨
  - **WindowState連携**: `ShowInTaskbar=False`と組み合わせてバックグラウンド動作を実現
- **Implications**:
  - MainWindowViewModelがTrayIconコマンド（設定表示、終了）を公開
  - アイコンリソースはプラットフォーム別に準備（.ico for Windows, .icns for macOS）

### Avalonia全画面透明オーバーレイウィンドウ
- **Context**: 領域選択UI（Req 2対応）のための透明オーバーレイ実装調査
- **Sources Consulted**:
  - [GitHub Discussion - Transparent Click-through Window](https://github.com/AvaloniaUI/Avalonia/discussions/11911)
  - [Avalonia Docs - Window Transparency](https://docs.avaloniaui.net/docs/guides/window-transparency)
- **Findings**:
  - **透明化設定**: `SystemDecorations="None"` + `TransparencyLevelHint="Transparent"` + `Background="Transparent"`
  - **クリックスルー制限**: Avaloniaネイティブにはクリックスルー機能なし、プラットフォーム固有API必要
    - Windows: `WS_EX_TRANSPARENT`フラグ (Win32 API)
    - macOS: `setIgnoresMouseEvents(_:)` (AppKit)
  - **全画面表示**: `WindowState="Maximized"` または `Bounds`を手動設定
  - **マルチディスプレイ対応**: 各ディスプレイごとにWindowインスタンス作成が必要
- **Implications**:
  - RegionSelectorViewModelは選択中のみマウスイベントを処理、選択完了後はウィンドウを閉じる
  - クリックスルーは不要（選択完了まで操作をブロックする仕様）
  - マルチディスプレイ選択UIはPhase 2対応とし、Phase 1ではプライマリディスプレイのみ

### ImageSharpマスク処理パフォーマンス
- **Context**: プライバシーマスク適用（Req 4対応）の性能検証
- **Sources Consulted**:
  - [SixLabors ImageSharp Official Site](https://sixlabors.com/products/imagesharp/)
  - [ImageSharp GitHub - Performance](https://github.com/SixLabors/ImageSharp?tab=readme-ov-file#performance)
- **Findings**:
  - **Rectangleミューターブル設計**: パフォーマンス最適化のためstructがミューターブル
  - **PNG完全サポート**: アルファチャンネル合成、透過処理が標準機能
  - **SIMD最適化**: .NET Vector APIによる高速画像処理
  - **System.Drawing比較**: 多くのベンチマークでSystem.Drawingより高速
  - **クロスプラットフォーム**: Windows/macOS/Linuxで一貫した動作
- **Implications**:
  - IImageProcessingServiceはImageSharpの`Image<Rgba32>`を使用
  - マスク適用は`Mutate(ctx => ctx.Fill(Color.Black, rectangle))`パターン
  - メモリ効率のためストリーム処理を優先（`Image.Load(stream)`）

### macOS screencaptureコマンド仕様
- **Context**: Phase 1 MVPのキャプチャ実装方法調査（Req 1対応）
- **Sources Consulted**:
  - [SS64.com - macOS screencapture manual](https://ss64.com/mac/screencapture.html)
  - [Apple Developer Forums - screencapture performance](https://developer.apple.com/forums/)
- **Findings**:
  - **基本構文**: `screencapture -R<x>,<y>,<w>,<h> -x -t png <filepath>`
    - `-R`: 座標指定（カンマ区切り、スペース不可）
    - `-x`: サウンド無効化
    - `-t png`: PNG形式指定
  - **座標系**: プライマリディスプレイ左上が原点 (0,0)、左側セカンダリは負のX座標
  - **終了コード**: 公式ドキュメントに記載なし（要実装時検証）
  - **パフォーマンス**: 録画開始時のわずかな遅延報告あり、スクリーンショットは高速
  - **権限**: Screen Recording権限が必須（macOS 10.15 Catalina以降）
- **Implications**:
  - IScreenCaptureServiceのmacOS実装は`Process.Start("screencapture", args)`
  - 0.5秒パフォーマンス目標の達成可否は実装後に計測
  - Phase 2でScreenCaptureKit移行オプションを確保（リスク軽減策）

### macOSグローバルホットキー実装
- **Context**: ホットキー登録方法の技術選定（Req 5対応）
- **Sources Consulted**:
  - ユーザー提供レポート: `macos-global-hotkey-final-report.md`
  - Carbon Framework vs CGEvent API比較分析
- **Findings**:
  - **Carbon Framework（却下）**:
    - 非推奨API、32bit時代の遺産
    - 学習価値が低い、将来性なし
  - **CGEvent API（採用）**:
    - Core Graphics Quartz Event Services
    - Apple公式サポート、64bit/Apple Silicon完全対応
    - 現代的API、将来性が高い
  - **実装パターン**:
    ```csharp
    private void RunLoopThreadProc()
    {
        _threadRunLoop = CFRunLoopGetCurrent();
        CFRunLoopAddSource(_threadRunLoop, _runLoopSource, kCFRunLoopCommonModes);
        CGEventTapEnable(_eventTap, true);
        CFRunLoopRun(); // ブロッキング呼び出し
    }

    private IntPtr EventCallback(IntPtr proxy, uint type, IntPtr eventRef, IntPtr userInfo)
    {
        if (type == kCGEventKeyDown)
        {
            var keyCode = (int)CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
            var flags = CGEventGetFlags(eventRef);
            HotKeyPressed?.Invoke(keyCode, flags);
        }
        return eventRef;
    }
    ```
  - **権限要件**: アクセシビリティ権限が必須（Screen Recording権限に追加）
  - **リソース管理**: CFRunLoop専用スレッド、適切なクリーンアップ必須
- **Implications**:
  - IHotkeyServiceのmacOS実装はCGEvent APIベース
  - IPermissionServiceはアクセシビリティ権限チェックを追加
  - 完全な実装コードがレポートに含まれる（実装時参照）

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| **MVVM + Service Layer** | Views (AXAML) → ViewModels (ReactiveUI) → Services (Interfaces) → Models (POCOs) | - 明確なレイヤー境界<br>- テスト可能なコア<br>- プラットフォーム抽象化が容易<br>- ReactiveUIによる宣言的UI | - サービスレイヤーの構築コスト<br>- 小規模機能でのオーバーヘッド | **採用**: Avaloniaベストプラクティス、macOS/Windows対応に最適 |
| MVC | Controllers → Models, Views observe Models | シンプル、学習コストが低い | - プラットフォーム抽象化が困難<br>- ViewとModelの結合 | Avalonia非推奨パターン |
| Clean Architecture | Entities → Use Cases → Interface Adapters → Frameworks | ドメインロジックの独立性が高い | - 小規模プロジェクトでは過剰<br>- レイヤー数が多い | 将来の大規模化で検討 |
| Event-driven | イベントバス中心のコンポーネント連携 | 疎結合、拡張性が高い | - デバッグが困難<br>- イベントフロー追跡の複雑化 | 現状の要件では不要 |

**選定理由**: MVVM + Service Layerは以下の点で最適
- Avalonia公式推奨、ReactiveUIとの親和性
- インターフェースベースのサービス層でmacOS/Windows実装を切り替え可能
- 各ドメイン（Capture, Configuration, UI, Platform）の明確な境界
- 単体テスト・統合テストの記述が容易

## Design Decisions

### Decision: macOSホットキー実装にCGEvent APIを採用

- **Context**: グローバルホットキー登録（Req 5）のためのmacOS API選定
- **Alternatives Considered**:
  1. **Carbon Framework** — 古典的なRegisterEventHotKey API
  2. **CGEvent API (Quartz Event Services)** — Core Graphicsのイベント監視機構
  3. **NSEvent.addGlobalMonitorForEvents** — Cocoa API（制約あり）
- **Selected Approach**: CGEvent API（CGEventTapCreate）を使用
  - CFRunLoop専用スレッドでイベントタップを作成
  - CGEventTapCreateでキーイベントをグローバル監視
  - コールバック関数でキーコードと修飾キーを判定
  - 完全な実装コードがユーザー提供レポートに含まれる
- **Rationale**:
  - Apple公式サポート、64bit/Apple Silicon完全対応
  - 将来性が高い現代的API
  - Carbon Frameworkは非推奨で学習価値が低い
  - NSEventのglobalMonitorは一部キーイベントを取得できない制約がある
- **Trade-offs**:
  - **メリット**: 安定性、将来性、クロスバージョン互換性
  - **デメリット**: アクセシビリティ権限が必須（Screen Recording権限に追加）、CFRunLoop管理の複雑性
- **Follow-up**: 実装時にリソースクリーンアップ（CGEventTapEnable無効化、CFRunLoopStop）を確実に実施

### Decision: Phase 1キャプチャ手法としてscreencaptureコマンドを採用

- **Context**: スクリーンキャプチャ実行方法（Req 1）の技術選定
- **Alternatives Considered**:
  1. **screencaptureコマンド** — `Process.Start("screencapture -R...")`
  2. **ScreenCaptureKit** — macOS 12.3+の新API、AVFoundation活用
  3. **CGWindowListCreateImage** — Core Graphics低レベルAPI
- **Selected Approach**: screencaptureコマンドをPhase 1 MVPで使用
  - `screencapture -R<x>,<y>,<w>,<h> -x -t png <filepath>`
  - Process.Startでコマンド実行、終了待機
  - 終了コードでエラー判定（要実装時検証）
- **Rationale**:
  - 高速実装、macOSネイティブツール
  - 追加のフレームワーク学習不要
  - macOS全バージョン対応（10.x〜最新）
- **Trade-offs**:
  - **メリット**: 実装速度、シンプル、枯れた技術
  - **デメリット**: 0.5秒パフォーマンス目標が未検証
- **Follow-up**:
  - Phase 1実装後にパフォーマンス計測（Req 11-1達成確認）
  - 0.5秒未達の場合、Phase 2でScreenCaptureKit移行オプション検討

### Decision: 座標系はプライマリディスプレイ原点を採用

- **Context**: マルチディスプレイ環境での座標管理（Req 2, 3）
- **Alternatives Considered**:
  1. **プライマリディスプレイ左上原点** — macOS screencapture仕様準拠
  2. **各ディスプレイ個別原点** — ディスプレイ相対座標
  3. **仮想スクリーン絶対座標** — Windows仕様準拠
- **Selected Approach**: プライマリディスプレイ左上を (0,0) とする絶対座標系
  - 左側セカンダリディスプレイは負のX座標
  - PhysicalCoordinates（物理ピクセル）で保存
  - LogicalCoordinates（論理ピクセル）からHiDPI変換
- **Rationale**:
  - macOS screencaptureコマンドの仕様に完全準拠
  - Phase 1（macOS優先）の実装を簡素化
- **Trade-offs**:
  - **メリット**: macOSネイティブ座標系、screencaptureと直接連携
  - **デメリット**: Windows移植時に座標変換ロジック追加が必要
- **Follow-up**:
  - Req 2-9準拠、ディスプレイ切断時のキャプチャ中止処理実装
  - IDisplayServiceでディスプレイ情報の可用性チェック

### Decision: マスク保存は相対座標を採用

- **Context**: プライバシーマスク領域の永続化方法（Req 4, 7）
- **Alternatives Considered**:
  1. **絶対座標保存** — 画面上の固定位置
  2. **相対座標保存** — キャプチャ領域左上を基準
  3. **パーセンテージ保存** — キャプチャ領域のサイズ比
- **Selected Approach**: キャプチャ領域左上を基準とした相対座標で保存
  - 例: キャプチャ領域 (100, 200, 400, 300)、マスク絶対座標 (150, 250, 50, 50)
  - 保存値: MaskRegion { X=50, Y=50, Width=50, Height=50 }
  - 適用時: 絶対座標 = キャプチャ領域.Origin + マスク相対座標
- **Rationale**:
  - キャプチャ領域変更時にマスク位置を再計算不要
  - ユーザーの意図（「ゲーム画面内のこの部分を隠す」）に合致
- **Trade-offs**:
  - **メリット**: 設定の可搬性、直感的な振る舞い
  - **デメリット**: キャプチャ領域外にマスクを配置不可（仕様上問題なし）
- **Follow-up**: MaskRegionモデルに座標系のドキュメントコメント追加

## Risks & Mitigations

### リスク1: screencaptureコマンドのパフォーマンス
- **リスク**: 0.5秒以内のキャプチャ完了（Req 11-1）が未検証
- **影響度**: 高（ユーザー体験の中核要件）
- **軽減策**:
  - Phase 1実装後、BenchmarkDotNetで実測
  - 未達の場合、Phase 2でScreenCaptureKit移行
  - キャプチャ中のプログレス表示でユーザー体験を改善

### リスク2: macOS/Windows互換性
- **リスク**: サービスインターフェース設計が不十分でWindows移植時に大幅修正
- **影響度**: 中（Phase 2対応遅延）
- **軽減策**:
  - インターフェース設計時にWindows APIを事前調査
  - IScreenCaptureService、IHotkeyServiceの抽象度を十分に高める
  - Phase 1実装時にWindows実装の疑似コード作成（設計検証）

### リスク3: 権限取得の失敗
- **リスク**: Screen Recording権限・アクセシビリティ権限の拒否でアプリ機能不全
- **影響度**: 高（アプリ起動不可）
- **軽減策**:
  - Req 12-1〜12-3準拠、権限チェック＋誘導UI実装
  - IPermissionServiceで両権限を個別チェック
  - クリティカルエラーとして明確なダイアログ表示

### リスク4: マルチディスプレイ環境の複雑性
- **リスク**: ディスプレイ切断、HiDPI混在、座標変換のエッジケース
- **影響度**: 中（特定環境でのバグ）
- **軽減策**:
  - Req 2-9準拠、ディスプレイ切断時のキャプチャ中止
  - IDisplayServiceで現在のディスプレイ構成を常時監視
  - Phase 1では手動テスト、Phase 2で自動UIテスト検討

### リスク5: ImageSharpのメモリ使用量
- **リスク**: 大画面キャプチャ＋マスク処理で50MB制約（Req 11-2）超過
- **影響度**: 低（4K解像度でも理論値30MB程度）
- **軽減策**:
  - ストリーム処理優先、中間バッファ最小化
  - 実装後にメモリプロファイリング（dotMemory等）
  - 必要に応じてマスク適用後に即座にDispose

## References

### 公式ドキュメント
- [Avalonia UI Documentation](https://docs.avaloniaui.net/) — UI フレームワーク全般
- [Avalonia TrayIcon Control](https://docs.avaloniaui.net/docs/reference/controls/tray-icon) — システムトレイ実装
- [SixLabors ImageSharp](https://sixlabors.com/products/imagesharp/) — 画像処理ライブラリ
- [macOS screencapture Manual](https://ss64.com/mac/screencapture.html) — screencaptureコマンド仕様

### コミュニティリソース
- [GitHub - Avalonia Transparency Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/11911) — 透明オーバーレイ実装パターン
- [GitHub - ImageSharp Performance](https://github.com/SixLabors/ImageSharp?tab=readme-ov-file#performance) — パフォーマンス最適化情報

### 内部ドキュメント
- `macos-global-hotkey-final-report.md` — CGEvent API採用決定の根拠、完全な実装コード例
- `.kiro/steering/tech.md` — 技術スタック定義（.NET 8, Avalonia 11, ReactiveUI）
- `.kiro/steering/structure.md` — MVVMディレクトリ構成、DIパターン

### 要件関連
- `.kiro/specs/screenshot-capture-system/requirements.md` — 全13要件の詳細仕様
