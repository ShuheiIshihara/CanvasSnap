---
title: "CanvasSnap 開発記録 #1: .NET 8 + Avalonia MVVM プロジェクトのセットアップ"
emoji: "🖼️"
type: "tech"
topics: ["dotnet", "avalonia", "csharp", "mvvm", "reactiveui"]
published: false
---

## はじめに

本記事は、ゲームスクリーンショット撮影アプリ「CanvasSnap」の開発記録シリーズの第1回です。Phase 1（macOS MVP）の実装タスク 1.1「.NET 8プロジェクト作成とAvalonia 11.x初期設定」の作業内容を記録します。

### タスク概要

- ✅ .NET 8.0 プロジェクトの作成
- ✅ Avalonia 11.x MVVM テンプレートの適用
- ✅ 必要なパッケージのインストール
- ✅ MVVM ディレクトリ構造の構築
- ✅ nullable reference types の有効化

**対応要件**: Requirements 13.2（.NET 8.0以降で実装）, 13.3（Avalonia 11.xを使用してUIを実装）

## 環境構築

### 1. .NET 8 SDK のインストール

macOS 環境のため、Homebrew を使用してインストールしました。

```bash
brew install dotnet@8
```

インストール後、環境変数を設定：

```bash
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
```

:::message
dotnet@8 は keg-only のため、PATH に明示的に追加する必要があります。永続化する場合は `~/.zshrc` に記述してください。
:::

バージョン確認：

```bash
$ dotnet --version
8.0.122
```

### 2. Avalonia テンプレートのインストール

```bash
dotnet new install Avalonia.Templates
```

利用可能なテンプレート：

```
テンプレート名                       短い名前
-----------------------------------  -------------------------
Avalonia .NET App                    avalonia.app
Avalonia .NET MVVM App               avalonia.mvvm
Avalonia Cross Platform Application  avalonia.xplat
```

今回は **avalonia.mvvm** テンプレートを使用します。

## プロジェクト作成

### 3. Avalonia MVVM プロジェクトの生成

```bash
mkdir -p src
cd src
dotnet new avalonia.mvvm -n CanvasSnap
```

:::message alert
Avalonia テンプレート（11.3.9）はデフォルトで .NET 9.0 をターゲットにしています。.NET 8.0 SDK では復元に失敗するため、修正が必要です。
:::

### 4. TargetFramework の修正

`src/CanvasSnap/CanvasSnap.csproj` を編集：

```diff xml
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
-   <TargetFramework>net9.0</TargetFramework>
+   <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    ...
  </PropertyGroup>
```

### 5. パッケージ構成の変更

Avalonia テンプレートは CommunityToolkit.Mvvm を使用していますが、プロジェクト設計では **ReactiveUI** を採用しています。理由は以下の通り：

- Avalonia 公式推奨
- リアクティブプログラミングとの高い親和性
- MVVM パターンのベストプラクティス

`CanvasSnap.csproj` のパッケージ参照を修正：

```diff xml
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.9" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.9" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.9" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.9" />
    <PackageReference Include="Avalonia.Diagnostics" Version="11.3.9" />
-   <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
+   <PackageReference Include="ReactiveUI.Fody" Version="19.5.41" />
+   <PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
  </ItemGroup>
```

:::message
**ImageSharp のバージョンに注意**

初期インストール時は 3.1.6 を使用しましたが、既知の脆弱性（GHSA-2cmq-823j-5qj8、GHSA-rxmq-m78w-7wmc）が報告されているため、**3.1.12** に更新しました。
:::

### 6. ViewModelBase の修正

テンプレート生成の `ViewModelBase.cs` を ReactiveUI に対応：

```diff csharp
- using CommunityToolkit.Mvvm.ComponentModel;
+ using ReactiveUI;

namespace CanvasSnap.ViewModels;

- public abstract class ViewModelBase : ObservableObject
+ public abstract class ViewModelBase : ReactiveObject
{
}
```

### 7. パッケージの復元とビルド

```bash
dotnet restore
dotnet build
```

**ビルド結果**:

```
MSBuild のバージョン 17.8.43+f0cbb1397 (.NET)
  CanvasSnap -> /Users/.../CanvasSnap/src/CanvasSnap/bin/Debug/net8.0/CanvasSnap.dll

ビルドに成功しました。
    1 個の警告
    0 エラー
```

:::details Fody に関する警告
ReactiveUI.Fody は初回ビルド時に `FodyWeavers.xml` を自動生成します。警告は無視して問題ありません。
:::

## プロジェクト構造

最終的なディレクトリ構成：

```
CanvasSnap/
├── CanvasSnap.sln
└── src/
    └── CanvasSnap/
        ├── CanvasSnap.csproj
        ├── Program.cs
        ├── App.axaml
        ├── App.axaml.cs
        ├── ViewLocator.cs
        ├── FodyWeavers.xml
        ├── app.manifest
        ├── Assets/
        │   └── avalonia-logo.ico
        ├── Views/           # XAML ビュー定義
        │   ├── MainWindow.axaml
        │   └── MainWindow.axaml.cs
        ├── ViewModels/      # UI ロジックと状態管理
        │   ├── ViewModelBase.cs
        │   └── MainWindowViewModel.cs
        ├── Models/          # ドメインデータ構造（今後追加）
        ├── Services/        # ビジネスロジック（今後追加）
        └── Helpers/         # ユーティリティ関数（今後追加）
```

### ディレクトリ設計のポイント

**MVVM + Service Layer** パターンを採用：

| ディレクトリ | 責務 | 依存関係 |
|------------|------|---------|
| `Views/` | AXAML UI定義 | ViewModels のみ |
| `ViewModels/` | UIロジック、状態管理 | Services, Models |
| `Services/` | ビジネスロジック、プラットフォーム抽象化 | Models |
| `Models/` | ドメインデータ | なし |
| `Helpers/` | 静的ユーティリティ | Models |

## インストール済みパッケージ

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| Avalonia | 11.3.9 | クロスプラットフォーム UI フレームワーク |
| Avalonia.Desktop | 11.3.9 | デスクトップ対応 |
| Avalonia.Themes.Fluent | 11.3.9 | Fluent Design テーマ |
| ReactiveUI.Fody | 19.5.41 | MVVM リアクティブバインディング |
| SixLabors.ImageSharp | 3.1.12 | 画像処理（マスク適用、PNG保存） |

:::message
**System.Text.Json について**

.NET 8 には標準で含まれているため、明示的なパッケージ参照は不要です。設定ファイルの JSON シリアライズに使用します。
:::

## プロジェクト設定の確認

### nullable reference types の有効化

`CanvasSnap.csproj` で確認：

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

これにより、null 安全性が向上し、コンパイル時に潜在的な NullReferenceException を検出できます。

### コンパイル済みバインディングの有効化

```xml
<PropertyGroup>
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

XAML バインディングのパフォーマンスが向上します。

## トラブルシューティング

### 問題 1: .NET 9.0 ターゲットエラー

**エラーメッセージ**:
```
error NETSDK1045: 現在の .NET SDK は、ターゲットとする .NET 9.0 をサポートしていません。
```

**解決方法**: `TargetFramework` を `net8.0` に変更

### 問題 2: CommunityToolkit.Mvvm が見つからない

**エラーメッセージ**:
```
error CS0246: 型または名前空間の名前 'CommunityToolkit' が見つかりませんでした
```

**解決方法**: ViewModelBase を ReactiveObject に変更し、パッケージ参照を ReactiveUI.Fody に置き換え

### 問題 3: ImageSharp 脆弱性警告

**警告メッセージ**:
```
warning NU1903: Package 'SixLabors.ImageSharp' 3.1.6 has a known high severity vulnerability
```

**解決方法**: バージョンを 3.1.12 に更新

## 次のステップ

タスク 1.2 では、以下を実装します：

- [ ] DI コンテナの設定（Microsoft.Extensions.DependencyInjection）
- [ ] プラットフォーム検出（OperatingSystem.IsMacOS()）
- [ ] サービス登録とライフタイム管理
- [ ] App.xaml.cs での ServiceProvider 初期化

## まとめ

タスク 1.1 では、.NET 8 + Avalonia 11.x + ReactiveUI の基盤を構築しました。主なポイント：

✅ .NET 8.0.122 SDK のインストールと設定
✅ Avalonia MVVM テンプレートの適用と修正
✅ ReactiveUI への移行（CommunityToolkit.Mvvm から）
✅ ImageSharp 脆弱性の修正（3.1.6 → 3.1.12）
✅ MVVM ディレクトリ構造の構築
✅ ビルド成功確認

次回は DI コンテナとプラットフォーム抽象化の実装を行います。

## リポジトリ

コミット: `2c8f777` - 追加: .NET 8 Avalonia MVVMプロジェクト初期化（タスク1.1）

---

_本記事は TDD に基づいた開発プロセスの一環として作成されています。_
