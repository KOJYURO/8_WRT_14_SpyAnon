# WRT SpyAnon

A **server-side** modlet that masks every player's in-game display name (floating nameplates,
owner labels, chat sender) and can turn text chat into a display-only feed. Built for a
"Spy vs Spy" style server where everyone looks anonymous in-game while the operator keeps real
names for scoreboards / rankings. Clients stay vanilla (no client download, no XUi).

> ### Download
> Drop-in package (`ModInfo.xml` + `WrtSpyAnon.dll`):
> **https://github.com/KOJYURO/8_WRT_14_SpyAnon/releases/latest/download/8_WRT_14_SpyAnon.zip**

---

## English

### Overview
Two independent, flag-toggleable features:

- **Display-name anonymization** — every player's shown name is replaced with a fixed string
  (default `名無しのサバイバー`) on floating nameplates, the ESC player list, join/leave messages,
  and chat sender labels.
- **Display-only chat** — a player's normal messages are not relayed to other clients (chat
  becomes readable-but-not-conversational). `/`-prefixed commands still pass, and server/bot
  messages still show.

Real names are preserved in the separate field the scoreboard reads, so **in-game is anonymous
while your rankings/admin tools stay real-name.**

### Install
- Server-side only: drop the folder into the **server's** `Mods/`. No client action.
  ```
  <server>/Mods/8_WRT_14_SpyAnon/
    ├─ ModInfo.xml
    └─ WrtSpyAnon.dll
  ```
- Restart the server. `[WRT-Anon] loaded` in the log means it is active.
- `SkipWithAntiCheat=true` — loads on EAC-enabled servers (kept because it is a Harmony DLL).

### How it works
Player names surface through two paths; both are closed:

- **(A) Server-generated text** (join/leave, chat sender) is built via
  `PersistentPlayerName.get_DisplayName` → Harmony **Postfix** swaps in the fixed name.
- **(B) Client-rendered data** (nameplates, ESC list) is drawn from name data the server
  syncs. Names serialize through `AuthoredText.ToStream`, which is **shared with disk saves** —
  so masking is gated by a `ThreadStatic` flag set only during the net-send packages'
  `write()` (`NetPackageWorldInfo` / `NetPackagePersistentPlayerState`). The disk-save path is
  never touched → **saved names stay real, zero corruption.**

### Compatibility & notes
- Server-side only, self-contained, no dependency on other mods; each feature toggles off via
  `Cfg` flags in source.
- Voice chat (EOS RTC) identities are **not** maskable server-side — this modlet covers
  text/display names only.
- Config lives as `Cfg` constants in `src/WrtSpyAnon.cs`; edit and rebuild to change
  (see **Build**).

### Build
`netstandard2.0` / C# 9. References the game's Managed DLLs and TFP Harmony (the
non-redistributable game DLLs are **not** included in this repo).

```bash
dotnet build -c Release \
  -p:ManagedDir=/path/to/7DaysToDieServer_Data/Managed \
  -p:HarmonyDir=/path/to/Mods/0_TFP_Harmony
```

Put the resulting `WrtSpyAnon.dll` in this folder.

---

## 日本語

### 概要
独立した2機能（各フラグで着脱可）：

- **表示名の匿名化** — 全プレイヤーの表示名を固定文字列（既定 `名無しのサバイバー`）に差し替え。
  頭上ネームプレート・ESCのプレイヤー一覧・参加/退出メッセージ・チャット送信者名すべてに適用。
- **表示専用チャット** — プレイヤーの通常発言を他クライアントへ中継しない（読めるが会話にならない演出）。
  `/` で始まるコマンドは通し、サーバー/botの発言は従来どおり表示。

実名はスコアボードが参照する別フィールドに残るため、**ゲーム内は匿名／集計・管理は実名**を両立します。

### 導入
- サーバーサイドのみ：サーバーの `Mods/` にフォルダごと配置。クライアント作業不要。
  ```
  <server>/Mods/8_WRT_14_SpyAnon/
    ├─ ModInfo.xml
    └─ WrtSpyAnon.dll
  ```
- サーバー再起動。ログに `[WRT-Anon] loaded` が出れば有効。
- `SkipWithAntiCheat=true`。EAC有効サーバーでもロードされます（Harmony DLLのため維持）。

### 仕組み
名前が「見える」経路は2系統あり、両方を塞いでいます：

- **(A) サーバー生成テキスト**（参加/退出・チャット送信者名）は `PersistentPlayerName.get_DisplayName`
  で組み立て → Harmony **Postfix** で固定名に差し替え。
- **(B) クライアント描画データ**（ネームプレート・一覧）は、サーバーが同期する名前データを各クライアントが
  描画する。名前は `AuthoredText.ToStream` でシリアライズされるが、これは**ディスク保存と共通**のため、
  ネット送信パッケージ（`NetPackageWorldInfo` / `NetPackagePersistentPlayerState`）の `write()` 実行中だけ立つ
  `ThreadStatic` フラグでゲート。ディスク保存経路は一切触らない＝**セーブ名は実名のまま破損ゼロ**。

### 互換性と補足
- サーバーサイドのみ・単独で自己完結・他MOD非依存。各機能はソース内 `Cfg` フラグで即無効化可能。
- ボイスチャット（EOS RTC）の名前はサーバー側で差し替え不可＝本MODはテキスト/表示名のみ対象。
- 設定は `src/WrtSpyAnon.cs` の `Cfg` 定数。変更は編集して再ビルド（**Build** 参照）。

---

## License

MIT © 2026 [7DAYSTODIE.JP](https://7daystodie.jp) — see [LICENSE](LICENSE).
