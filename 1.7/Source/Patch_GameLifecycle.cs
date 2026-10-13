using CustomPortraits;
using HarmonyLib;
using Verse;
using Foxy.CustomPortraits.CustomPortraitsEx;

namespace Foxy.CustomPortraits
{
    /// <summary>
    /// ゲームロード開始時・メインメニュー退出時・ゲーム破棄時に、
    /// メインスレッド側で即座に動画再生とポートレート状態を停止・リセットするパッチ。
    /// 
    /// 【背景と理由】
    /// RimWorldのセーブデータ読み込みは LongEventHandler により非同期（バックグラウンドスレッド）で実行されます。
    /// GameComponent.LoadedGame() もそのバックグラウンドスレッド上で呼ばれるため、
    /// そこで VideoPlayer.Stop() などの Unity ネイティブ C++ API を呼ぶと
    /// アクセス違反（0xC0000005: 無効なアドレスにアクセスしようとしています）が発生し Unity ごとクラッシュします。
    /// また、LoadedGame のタイミングまで停止しないと、ロード画面中にも前の動画や音声が再生され続けてしまいます。
    /// 
    /// そのため、ロード画面に入る直前の「メインスレッド」で呼ばれる以下の各処理をフックし、
    /// セーブ読み込みスレッドが起動する前に安全かつ即座に ResetAll() を実行します。
    /// </summary>
    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.LoadGame), new System.Type[] { typeof(string) })]
    public static class Patch_GameDataSaveLoader_LoadGame
    {
        public static void Prefix(string saveFileName)
        {
            if (Settings.Instance.debug)
                Log.Message($"[PortraitsEx] Patch_GameDataSaveLoader_LoadGame: Resetting all portraits before loading {saveFileName}");
            ConditionDrivenPortrait.ResetAll();
        }
    }

    [HarmonyPatch(typeof(GenScene), nameof(GenScene.GoToMainMenu))]
    public static class Patch_GenScene_GoToMainMenu
    {
        public static void Prefix()
        {
            if (Settings.Instance.debug)
                Log.Message("[PortraitsEx] Patch_GenScene_GoToMainMenu: Resetting all portraits before returning to main menu");
            ConditionDrivenPortrait.ResetAll();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Dispose))]
    public static class Patch_Game_Dispose
    {
        public static void Prefix()
        {
            if (Settings.Instance.debug)
                Log.Message("[PortraitsEx] Patch_Game_Dispose: Resetting all portraits on game dispose");
            ConditionDrivenPortrait.ResetAll();
        }
    }
}