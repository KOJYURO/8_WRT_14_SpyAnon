# WRT SpyAnon — 匿名化 + 表示専用チャット (7 Days to Die server-side modlet)

**Server-side anonymization for 7 Days to Die.** Masks the in-game display name (floating
nameplates, owner labels, chat sender) for **all** players, and optionally turns text chat into
a display-only feed. Built for a "Spy vs Spy" style server where everyone should look anonymous
in-game while the operator keeps real names for scoreboards / rankings.

サーバーサイドの匿名化MODletです。頭上ネームプレート・所有者ラベル・チャット送信者名など、
**ゲーム内の表示名を全プレイヤーに対してマスク**します。さらにテキストチャットを「表示専用」化
（プレイヤーの通常発言を他クライアントへ中継しない）できます。実名はスコアボード等が参照する
別フィールドに残るため、**ゲーム内は匿名／集計は実名**を両立できます。

> Server-side only. Self-contained single modlet. No client install required.
> サーバーサイドのみ・単独MODlet・クライアント導入不要。

---

## What it does / できること

| 機能 | 説明 |
|---|---|
| **① 表示名の匿名化 (`HideNames`)** | 全プレイヤーの表示名を固定文字列（既定 `名無しのサバイバー`）に差し替え。頭上ネームプレート、ESCのプレイヤー一覧、参加/退出メッセージ、チャット送信者名すべてに適用。 |
| **② 表示専用チャット (`ChatDisplayOnly`)** | プレイヤーの通常発言を他クライアントへ中継しない（＝チャットが読めるが会話にならない演出）。`/` で始まるコマンドは通し、サーバー/botの発言は従来どおり表示。 |

### 名前が漏れない仕組み / How masking is complete

7DTDでプレイヤー名が「見える」経路は2系統あり、両方を塞いでいます：

- **(A) サーバー生成テキスト** — 参加/退出やチャット送信者名はサーバーが `PersistentPlayerName.get_DisplayName`
  で組み立てる → Postfix で固定名に差し替え。
- **(B) クライアント描画** — 頭上ネームプレート/一覧は「サーバーから同期された名前データ」を各クライアントが
  描画する。名前は `AuthoredText.ToStream` でシリアライズされるが、これは**ディスク保存でも共通**のため、
  **ネット送信パッケージの `write()` 実行中だけ立つ `ThreadStatic` フラグ**でゲートしてネット送信名のみ空/固定化。
  ディスク保存経路は一切触らない＝**セーブ名は実名のまま破損ゼロ**。

> ⚠️ ボイスチャット（EOS RTC）はサーバー側で名前を差し替えられません。本MODはテキスト表示名のみ対象です。
> Voice chat (EOS RTC) identities are not maskable server-side; this modlet covers text/display names only.

---

## Install / 導入

1. サーバーの `Mods/` フォルダにこのフォルダごと配置：
   ```
   <server>/Mods/8_WRT_14_SpyAnon/
     ├─ ModInfo.xml
     └─ WrtSpyAnon.dll
   ```
2. サーバーを再起動。ログに `[WRT-Anon] loaded` が出れば有効。
3. `ModInfo.xml` は `SkipWithAntiCheat=true`。EAC有効サーバーでもロードされます（Harmony DLL のため EAC 環境で読み込ませたい場合はこの設定を維持）。

> DLL のみ配布でも動きます。`ModInfo.xml` + `WrtSpyAnon.dll` の2ファイルが必須です。

---

## Configuration / 設定

設定は現状 **ソース内の定数** (`Cfg`) です。変更するには編集して再ビルドしてください（[Build](#build--ビルド) 参照）。

```csharp
internal static class Cfg
{
    internal const bool   ChatDisplayOnly = true;   // 通常チャットを表示専用に
    internal const bool   HideNames       = true;   // 表示名を匿名化
    internal const string Mask            = "名無しのサバイバー"; // 匿名時の固定表示名(空文字で完全非表示)
    internal const string CmdPrefix       = "/";    // 通すコマンド前置詞
}
```

- 匿名化だけ欲しい → `ChatDisplayOnly = false`。
- チャット制御だけ欲しい → `HideNames = false`。
- 完全非表示（名前欄を空に） → `Mask = ""`。

---

## Build / ビルド

`netstandard2.0` / C# 9。ゲーム同梱のManaged DLL群とTFP Harmonyを参照します（**再配布不可のゲームDLLはこのリポジトリに含みません**）。

```bash
dotnet build -c Release \
  -p:ManagedDir=/path/to/7DaysToDieServer_Data/Managed \
  -p:HarmonyDir=/path/to/Mods/0_TFP_Harmony
```

出力 `WrtSpyAnon.dll` をこのフォルダに置けばOK。

### 依存参照 / References
- `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `mscorlib.dll`, `netstandard.dll`,
  `System*.dll`, `LogLibrary.dll` … ゲームの `Managed/` から
- `0Harmony.dll` … `Mods/0_TFP_Harmony/`

---

## Compatibility / 互換性

- **Server-side only** — クライアント改変なし・EACと非干渉（`SkipWithAntiCheat`）。
- 単独で自己完結。他MOD非依存。フラグで各機能を即無効化可能。
- 7 Days to Die の Harmony 対応版（TFP Harmony 同梱ビルド）を想定。

---

## License

MIT © 2026 [7DAYSTODIE.JP](https://7daystodie.jp) — see [LICENSE](LICENSE).
