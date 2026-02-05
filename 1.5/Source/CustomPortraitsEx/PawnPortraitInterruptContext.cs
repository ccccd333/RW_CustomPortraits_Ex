using Foxy.CustomPortraits.CustomPortraitsEx.Interrupt;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class PawnPortraitInterruptContext
    {
        public static PainInterruptContextResolver pain_interrupt_context_resolver = new PainInterruptContextResolver();
        public static DownedInterruptContextResolver down_interrupt_context_resolve = new DownedInterruptContextResolver();
        public static Dictionary<string, float> ComposeImpactMap(Pawn pawn, PortraitInterrupt interrupt, bool is_interrupt_active, out bool is_value_fetched)
        {
            Dictionary<string, float> impact_map = new Dictionary<string, float>();
            // TODO:数が多くなってきたらスレッド化して監視のインターバルを作るかも

            if (interrupt.enabled_monitors[(int)MonitorType.PainIncrease])
            {
                // 割り込み可能、不可能どちらでも計算だけはしておく
                // もし割り込み不可の時で返信した際はスキップされる
                pain_interrupt_context_resolver.TryResolveInterruptContext(pawn, interrupt, impact_map);
            }

            if (interrupt.enabled_monitors[(int)MonitorType.Downed] && !is_interrupt_active)
            {
                // ダウン判定は割り込み可能時のみ計算する
                down_interrupt_context_resolve.TryResolveInterruptContext(pawn, interrupt, impact_map);
            }

            if (impact_map.Count > 0)
            {
                is_value_fetched = true;
            }
            else
            {
                is_value_fetched = false;
            }

            return impact_map;
        }

        // 以降特に値の保持による監視をする必要ない場合はここにメソッドを書いていく
        // 特定の値が必要ならInterrupt配下にクラスを作って
    }
}
