using System;
using System.IO;
using HarmonyLib;

// WRT SpyAnon ─ 2F(Spy vs Spy鯖)向け 匿名化 + 表示専用チャット
//
// 設計(2026-07-19):
//  ① 匿名化(表示名を全員に対し空文字化)。名前が出る面は2系統あり、両方を塞ぐ:
//     (A) サーバー生成テキスト(参加/退出メッセージ, サーバー経由チャット送信者名)
//         = サーバーが get_DisplayName で文面を組む → Postfixで空文字化。
//     (B) クライアント描画(頭上ネームプレート, ESCプレイヤー一覧)
//         = 各クライアントが「サーバーから同期された名前データ」を描画する。専用サーバーは描画しないので、
//           サーバーが送出する名前を空にすればクライアント側の描画結果が空になる。
//           名前は persistentPlayers 同期で運ばれ、PersistentPlayerData.Write → PersistentPlayerName.get_AuthoredName
//           → AuthoredText.ToStream(AuthoredText, BinaryWriter) でシリアライズされる。ただし ToStream はディスク保存でも
//           共通なので、★ネット送信パッケージ(NetPackageWorldInfo.write / NetPackagePersistentPlayerState.write)の実行中
//           だけ立つ ThreadStatic フラグでゲートし、ディスク保存経路は絶対に触らない(セーブ名破損ゼロ)。
//     ★Scoreboard→WPランキングは別フィールド ClientInfo.playerName を参照(WrtScoreboard.ResolveName)。本MODは
//       ClientInfo を一切変更しない=ゲーム内匿名・管理(lp)/WPは実名、が両立。
//  ② 表示専用チャット: GameManager.ChatMessageServer を Prefix。実プレイヤー発言(EMessageSender.SenderIdAsPlayer=2)
//     を破棄=他クライアントへ非中継。Server/None(bot/システム)と '/' コマンドは通す。
//
// サーバーサイドのみ。単独MODletで着脱可能・全パッチはフラグ即無効化可(Cfg)。

namespace WrtSpyAnon
{
    public class Api : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out("[WRT-Anon] loaded (v1.1) — chatDisplayOnly=" + Cfg.ChatDisplayOnly
                    + " hideNames=" + Cfg.HideNames);
            try { new Harmony("wrt.spyanon").PatchAll(System.Reflection.Assembly.GetExecutingAssembly()); }
            catch (Exception e) { Log.Warning("[WRT-Anon] Harmony patch failed: " + e.Message); }
        }
    }

    internal static class Cfg
    {
        internal static readonly bool ChatDisplayOnly = true; // 通常チャットを表示専用に
        internal static readonly bool HideNames = true;       // 表示名を匿名化
        internal const string CmdPrefix = "/";                // コマンド前置詞(通す)

        // 匿名時の表示名。Config/Localization.csv の "wrtSpyAnonMask"(EN/JP)で解決し
        // サーバー言語に追従する(英語サーバー=Anonymous Survivor / 日本語=名無しのサバイバー)。
        // ★同期される単一値なので、各クライアントの言語別ではなくサーバー言語で決まる。
        internal const string MaskKey = "wrtSpyAnonMask";
        internal static readonly string MaskOverride = "";        // 非空にすると CSV より優先(固定文字列)。完全非表示は CSV 値を空に
        internal const string MaskFallback = "名無しのサバイバー"; // Localization 未ロード/キー欠落時の保険

        [ThreadStatic] private static string _maskCache;
        private static bool _logged;
        internal static string Mask
        {
            get
            {
                if (!string.IsNullOrEmpty(MaskOverride)) return MaskOverride;
                if (_maskCache != null) return _maskCache;
                try
                {
                    string s = Localization.Get(MaskKey, false, null);
                    if (!string.IsNullOrEmpty(s) && s != MaskKey)
                    {
                        _maskCache = s;
                        if (!_logged) { _logged = true; Log.Out("[WRT-Anon] mask resolved from Localization '" + MaskKey + "' = " + s); }
                        return s;
                    }
                }
                catch { /* localization 未初期化などは fallback */ }
                return MaskFallback; // 成功するまでキャッシュしない(後で言語ロード後に再解決)
            }
        }
    }

    // ネット送信中のみ true にする(ディスク保存経路と区別)。ThreadStatic=送信スレッド局所。
    internal static class NetMask
    {
        [ThreadStatic] internal static bool Active;
    }

    // ── ② 表示専用チャット ──────────────────────────────────────
    [HarmonyPatch(typeof(GameManager), "ChatMessageServer")]
    internal static class Patch_ChatDisplayOnly
    {
        static bool Prefix(string _msg, EMessageSender _msgSender)
        {
            if (!Cfg.ChatDisplayOnly) return true;
            try
            {
                if (_msgSender != EMessageSender.SenderIdAsPlayer) return true; // bot/システム=表示
                if (_msg != null && _msg.TrimStart().StartsWith(Cfg.CmdPrefix, StringComparison.Ordinal))
                    return true;                                               // コマンドは通す
                return false;                                                  // プレイヤー通常発言=破棄
            }
            catch { return true; }
        }
    }

    // ── ①(A) サーバー生成テキストの表示名マスク ─────────────────
    [HarmonyPatch(typeof(PersistentPlayerName), "get_DisplayName")]
    internal static class Patch_HideDisplayName
    {
        static void Postfix(ref string __result) { if (Cfg.HideNames) __result = Cfg.Mask; }
    }

    [HarmonyPatch(typeof(PersistentPlayerName), "get_SafeDisplayName")]
    internal static class Patch_HideSafeDisplayName
    {
        static void Postfix(ref string __result) { if (Cfg.HideNames) __result = Cfg.Mask; }
    }

    // ── ①(B) クライアント同期される名前データのマスク ───────────
    // ネット送信パッケージの write() 実行中だけフラグを立てる(Finalizerで必ず戻す)。
    [HarmonyPatch(typeof(NetPackageWorldInfo), "write")]
    internal static class Patch_WorldInfoWrite
    {
        static void Prefix() { if (Cfg.HideNames) NetMask.Active = true; }
        static void Finalizer() { NetMask.Active = false; }
    }

    [HarmonyPatch(typeof(NetPackagePersistentPlayerState), "write")]
    internal static class Patch_PPStateWrite
    {
        static void Prefix() { if (Cfg.HideNames) NetMask.Active = true; }
        static void Finalizer() { NetMask.Active = false; }
    }

    // 名前(AuthoredText)のネットシリアライズ時、フラグが立っていれば空名に差し替え。
    // ディスク保存(SavePersistentPlayerData→PersistentPlayerList.Write(string))ではフラグは立たない=実名のまま保存。
    [HarmonyPatch(typeof(AuthoredText), "ToStream", new Type[] { typeof(AuthoredText), typeof(BinaryWriter) })]
    internal static class Patch_AuthoredNameWire
    {
        static void Prefix(ref AuthoredText _instance)
        {
            if (Cfg.HideNames && NetMask.Active)
            {
                string m = Cfg.Mask;
                _instance = string.IsNullOrEmpty(m) ? new AuthoredText() : new AuthoredText(m, null);
            }
        }
    }
}
