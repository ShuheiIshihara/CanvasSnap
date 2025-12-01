# Requirements Document

## Project Description (Input)
ゲームスクリーンショット撮影アプリ（CanvasSnap）- WebブラウザでプレイしているゲームのCanvas要素を、ゲームに干渉することなく安全にスクリーンショット撮影できるデスクトップアプリケーション。OS標準APIによる非侵襲的キャプチャ、プライバシーマスク機能、ホットキー操作を提供。macOS優先（フェーズ1）、Windows対応予定（フェーズ2）

## はじめに

本文書は、ゲームスクリーンショット撮影アプリケーション（CanvasSnap）の要件を定義します。本アプリケーションは、Webブラウザで実行されるゲームの画面を、ゲームプロセスに一切干渉することなくOS標準APIのみで安全にキャプチャすることを目的としています。アンチチートシステムによる検知を回避し、プライバシー保護のためのマスク機能とホットキー操作による高速なワークフローを提供します。

## Requirements

### Requirement 1: 安全なスクリーンキャプチャ
**Objective:** ゲームプレイヤーとして、アンチチートシステムに検知されることなくゲーム画面をキャプチャしたい。そのために、OS標準の画面キャプチャAPIのみを使用し、ゲームプロセスに干渉しない方式で実装する。

#### Acceptance Criteria
1. When ユーザーがホットキーを押下した、the CanvasSnapアプリケーション shall OS標準の画面キャプチャAPIを使用してスクリーンショットを撮影する
2. The CanvasSnapアプリケーション shall ゲームプロセス・ブラウザプロセスへのコード注入を一切行わない
3. The CanvasSnapアプリケーション shall メモリの読み取り・書き込みを一切行わない
4. The CanvasSnapアプリケーション shall ネットワーク通信の傍受・改変を一切行わない
5. The CanvasSnapアプリケーション shall ゲームのDOM/Canvas要素への直接アクセスを一切行わない
6. The CanvasSnapアプリケーション shall ローカルストレージへの画像保存のみを行う
7. When macOS環境で実行される、the CanvasSnapアプリケーション shall `screencapture`コマンドを使用してキャプチャする
8. When Windows環境で実行される、the CanvasSnapアプリケーション shall BitBltまたはWindows.Graphics.Capture APIを使用してキャプチャする

### Requirement 2: キャプチャ領域管理
**Objective:** ユーザーとして、ゲームCanvas領域を正確にキャプチャするために、任意の矩形領域を指定して保存したい。

#### Acceptance Criteria
1. When ユーザーがキャプチャ領域選択モードを開始した、the CanvasSnapアプリケーション shall 全画面半透明オーバーレイを表示する
2. When ユーザーがオーバーレイ上でドラッグ操作を行った、the CanvasSnapアプリケーション shall 矩形選択範囲を視覚的に表示する
3. When 矩形選択中である、the CanvasSnapアプリケーション shall 座標とサイズ情報をリアルタイムで表示する
4. When ユーザーがドラッグを完了した、the CanvasSnapアプリケーション shall 選択された領域の座標（X, Y, Width, Height）を実画素ベースで保存する
5. When ユーザーがESCキーを押下した、the CanvasSnapアプリケーション shall 範囲選択をキャンセルしオーバーレイを閉じる
6. While マルチディスプレイ環境である、the CanvasSnapアプリケーション shall 仮想スクリーンスペース全体から範囲選択可能にする
7. The CanvasSnapアプリケーション shall キャプチャ座標をプライマリディスプレイ左上を原点とする絶対座標で管理する
8. When ディスプレイ構成が変更された、the CanvasSnapアプリケーション shall ユーザーに再設定を促す通知を表示する
9. If キャプチャ実行時に対象ディスプレイが利用不可である（接続切断等）、then the CanvasSnapアプリケーション shall キャプチャを中止しユーザーに失敗を通知する（通知の詳細仕様は Requirement 10 に従う）

### Requirement 3: HiDPI・Retinaディスプレイ対応
**Objective:** ユーザーとして、高解像度ディスプレイ環境でも正確にキャプチャ領域を指定できるようにしたい。

#### Acceptance Criteria
1. When macOS Retina環境で実行される、the CanvasSnapアプリケーション shall 実画素ベースでキャプチャ座標を管理する
2. When Windows HiDPI環境で実行される、the CanvasSnapアプリケーション shall DPIスケーリングを考慮した座標変換を実装する
3. When 設定UIで座標を表示する、the CanvasSnapアプリケーション shall 論理座標を表示する
4. When 座標を保存する、the CanvasSnapアプリケーション shall 内部的に実画素座標に変換して保存する

### Requirement 4: プライバシーマスク機能
**Objective:** ユーザーとして、スクリーンショット内の個人情報を含む領域を自動的に黒塗りマスクすることで、プライバシーを保護したい。

#### Acceptance Criteria
1. When ユーザーがマスク領域選択モードを開始した、the CanvasSnapアプリケーション shall キャプチャ領域内でドラッグ操作により矩形範囲を選択可能にする
2. When マスク領域が選択された、the CanvasSnapアプリケーション shall マスク座標をキャプチャ領域左上を(0,0)とする相対座標で保存する
3. When マスク機能が有効である、the CanvasSnapアプリケーション shall キャプチャ時に指定領域を黒（#000000）で塗りつぶす
4. The CanvasSnapアプリケーション shall マスク機能の有効/無効をチェックボックスで切り替え可能にする
5. When マスク機能が無効である、the CanvasSnapアプリケーション shall マスク処理を行わずキャプチャする

### Requirement 5: ホットキー操作
**Objective:** ユーザーとして、ゲームプレイ中に素早くスクリーンショットを撮影するために、グローバルホットキーでキャプチャを実行したい。

#### Acceptance Criteria
1. When ユーザーがホットキーを押下した、the CanvasSnapアプリケーション shall 即座にキャプチャ処理を開始する
2. The CanvasSnapアプリケーション shall デフォルトホットキーとしてCmd+Shift+S（macOS）/ Ctrl+Shift+S（Windows）を設定する
3. When ユーザーがホットキーをカスタマイズした、the CanvasSnapアプリケーション shall 修飾キー（Cmd/Ctrl, Shift, Alt）と任意のキーの組み合わせを受け付ける
4. If ホットキーがOS予約ショートカットと競合する、then the CanvasSnapアプリケーション shall 登録を拒否しユーザーに別の組み合わせを選択させる
5. While IMEがオンである、the CanvasSnapアプリケーション shall ホットキーに反応する
6. When ホットキー登録に失敗した、the CanvasSnapアプリケーション shall エラーメッセージを表示する

### Requirement 6: ファイル保存
**Objective:** ユーザーとして、キャプチャした画像を一意のファイル名で自動的に保存したい。

#### Acceptance Criteria
1. When キャプチャが完了した、the CanvasSnapアプリケーション shall 画像をPNG形式で保存する
2. The CanvasSnapアプリケーション shall ファイル名を `screenshot_yyyyMMdd_HHmmssfff.png` 形式で生成する
3. If 同名ファイルが既に存在する、then the CanvasSnapアプリケーション shall `_001`, `_002`などの連番を付与する
4. The CanvasSnapアプリケーション shall ユーザーが設定で指定したフォルダに保存する
5. The CanvasSnapアプリケーション shall デフォルト保存先としてピクチャフォルダを使用する
6. When 保存が成功した、the CanvasSnapアプリケーション shall OS標準通知で「スクリーンショットを保存しました」と表示する（通知の詳細仕様は Requirement 10 に従う）
7. If 保存先フォルダにアクセスできない、then the CanvasSnapアプリケーション shall クリティカルエラーダイアログを表示し対処方法を案内する
8. If ディスク容量が不足している、then the CanvasSnapアプリケーション shall クリティカルエラーダイアログを表示し空き容量確保を案内する

### Requirement 7: 設定の永続化
**Objective:** ユーザーとして、一度設定した内容をアプリケーション再起動後も維持したい。

#### Acceptance Criteria
1. When アプリケーションが起動した、the CanvasSnapアプリケーション shall 設定ファイルを自動的に読み込む
2. When ユーザーが設定を保存した、the CanvasSnapアプリケーション shall JSON形式で設定ファイルに書き込む
3. When macOS環境で実行される、the CanvasSnapアプリケーション shall `~/Library/Application Support/CanvasSnap/config.json`に設定を保存する
4. When Windows環境で実行される、the CanvasSnapアプリケーション shall `%AppData%\CanvasSnap\config.json`に設定を保存する
5. If 設定ファイルのJSONパースに失敗した、then the CanvasSnapアプリケーション shall デフォルト設定で起動する
6. If 設定ファイルが破損している、then the CanvasSnapアプリケーション shall 破損ファイルを`.backup`拡張子でリネームし保持する
7. If 設定ファイル破損により復元した、then the CanvasSnapアプリケーション shall ユーザーに通知し設定の再構成を促す

### Requirement 8: 設定画面UI
**Objective:** ユーザーとして、直感的なUIで各種設定を変更したい。

#### Acceptance Criteria
1. When 設定画面を開いた、the CanvasSnapアプリケーション shall 現在のホットキーを表示する
2. When 設定画面を開いた、the CanvasSnapアプリケーション shall 現在のキャプチャ領域を表示する
3. When 設定画面を開いた、the CanvasSnapアプリケーション shall 現在のマスク領域を表示する
4. When 設定画面を開いた、the CanvasSnapアプリケーション shall マスク機能の有効/無効状態を表示する
5. When 設定画面を開いた、the CanvasSnapアプリケーション shall 現在の保存先パスを表示する
6. When ユーザーがテストキャプチャボタンをクリックした、the CanvasSnapアプリケーション shall 現在の設定でキャプチャを実行し動作確認を可能にする
7. When ユーザーが保存先を選択した、the CanvasSnapアプリケーション shall フォルダ選択ダイアログを表示する
8. When 保存先フォルダが選択された、the CanvasSnapアプリケーション shall 書き込み権限をチェックする
9. If 保存先フォルダに書き込み権限がない、then the CanvasSnapアプリケーション shall エラーメッセージを表示し別のフォルダを選択させる
10. When ユーザーが設定保存ボタンをクリックした、the CanvasSnapアプリケーション shall 設定ファイルに保存し成功メッセージを表示する

### Requirement 9: システムトレイ統合
**Objective:** ユーザーとして、アプリケーションをバックグラウンドで常駐させ、必要な時だけ設定画面にアクセスしたい。

#### Acceptance Criteria
1. When アプリケーションが起動した、the CanvasSnapアプリケーション shall システムトレイにアイコンを表示する
2. When ユーザーがトレイアイコンをクリックした、the CanvasSnapアプリケーション shall コンテキストメニューを表示する
3. The CanvasSnapアプリケーション shall コンテキストメニューに「設定」と「終了」を含める
4. When ユーザーが「設定」を選択した、the CanvasSnapアプリケーション shall 設定ウィンドウを表示する
5. When ユーザーが「終了」を選択した、the CanvasSnapアプリケーション shall アプリケーションを終了する

### Requirement 10: 通知システム
**Objective:** ユーザーとして、キャプチャの成功・失敗や重要な状態変化を通知で確認したい。

#### Acceptance Criteria
1. When キャプチャが成功した、the CanvasSnapアプリケーション shall OS標準通知で成功メッセージを表示する
2. If 軽微なエラーが発生した、then the CanvasSnapアプリケーション shall OS標準通知でエラー内容を表示する
3. If クリティカルなエラーが発生した、then the CanvasSnapアプリケーション shall ダイアログで明示し対処方法を案内する
4. If 画面キャプチャ権限がない、then the CanvasSnapアプリケーション shall クリティカルエラーダイアログでシステム設定を開くボタンを表示する

### Requirement 11: パフォーマンス
**Objective:** ユーザーとして、ゲームプレイを妨げない高速なキャプチャ処理を体験したい。

#### Acceptance Criteria
1. When ホットキーを押下した、the CanvasSnapアプリケーション shall 0.5秒以内にキャプチャを完了する（Apple M1以降/Intel Core i5相当以上の環境）
2. While アイドル状態である、the CanvasSnapアプリケーション shall メモリ使用量を50MB未満に保つ
3. While バックグラウンドアイドル時、the CanvasSnapアプリケーション shall 1分間の平均CPU使用率を3%未満に保つ

### Requirement 12: 権限管理
**Objective:** ユーザーとして、OS標準の権限システムに従い、必要な権限のみを付与することでセキュリティを確保したい。

#### Acceptance Criteria
1. When macOS環境で実行される、the CanvasSnapアプリケーション shall 画面録画権限（Screen Recording）を要求する
2. If 画面録画権限が付与されていない、then the CanvasSnapアプリケーション shall 権限設定画面を開くよう案内する
3. The CanvasSnapアプリケーション shall Info.plist（macOS）に権限要求の用途を記載する
4. The CanvasSnapアプリケーション shall Phase 1（macOS版）では外部ネットワーク通信を行わない（すべての処理をローカルで完結する）

### Requirement 13: プラットフォーム互換性
**Objective:** 開発者として、macOS環境を優先しつつ、将来的にWindows環境へも移植可能なアーキテクチャを構築したい。

#### Acceptance Criteria
1. The CanvasSnapアプリケーション shall macOS Monterey 12.0以降で動作する
2. The CanvasSnapアプリケーション shall .NET 10.0以降で実装する
3. The CanvasSnapアプリケーション shall Avalonia 11.xを使用してUIを実装する
4. The CanvasSnapアプリケーション shall プラットフォーム依存機能をインターフェースで抽象化する
5. Where Windows対応が実装される、the CanvasSnapアプリケーション shall Windows 10/11で動作する
