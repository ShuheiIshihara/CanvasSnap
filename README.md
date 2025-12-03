# CanvasSnap

ゲームスクリーンショット撮影アプリ - WebブラウザでプレイしているゲームのCanvas要素を、ゲームに干渉することなく安全にスクリーンショット撮影できるデスクトップアプリケーション。

## 開発環境のセットアップ

### 必要な環境

- .NET 10.0 SDK

### 環境変数の設定

このプロジェクトは .NET 10.0 を使用しています。ビルドする前に、以下の環境変数を設定してください：

```bash
export DOTNET_ROOT="/usr/local/share/dotnet"
export PATH="/usr/local/share/dotnet:$PATH"
```

### ビルド方法

```bash
# 環境変数を設定
export DOTNET_ROOT="/usr/local/share/dotnet"
export PATH="/usr/local/share/dotnet:$PATH"

# ビルド
dotnet build

# 実行
dotnet run --project src/CanvasSnap/CanvasSnap.csproj
```