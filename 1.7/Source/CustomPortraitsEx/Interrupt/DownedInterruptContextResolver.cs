using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Interrupt
{
    public class DownedInterruptContextResolver
    {
        Pawn tracked_pawn;
        bool last_downed_state = false;

        public bool TryResolveInterruptContext(Pawn target_pawn, PortraitInterrupt portrait_interrupt, Dictionary<string, float> impact_map)
        {
            // 監視対象が切り替わった場合、値を控える
            if (tracked_pawn != target_pawn)
            {
                if (!ResetTracking(target_pawn))
                {
                    // 監視対象がないためそのまま返却
                    return false;
                }
            }

            if (tracked_pawn.Dead && tracked_pawn.health == null && tracked_pawn.health.hediffSet == null)
            {

                return false;
            }

            // 現在ダウンしているか
            bool downed = tracked_pawn?.health?.Downed ?? false;

            if (downed)
            {
                if (portrait_interrupt.monitor_behaviors.downed.trigger_on_enter)
                {
                    if(!last_downed_state && downed)
                    {
                        last_downed_state = downed;
                        impact_map[PortraitContextKeys.DOWNED] = 1.0f;
                    }
                }
                else
                {
                    impact_map[PortraitContextKeys.DOWNED] = 1.0f;
                }
            }
            else
            {
                last_downed_state = false;
            }
            
            return true;
        }

        private bool ResetTracking(Pawn target_pawn)
        {
            // そもそも死んでたり対象がいないなら痛みを監視しない
            // 公式がhealthとhediffSetのnullチェックしてるから一応入れとく
            if (target_pawn == null)
            {
                if (target_pawn.health == null && target_pawn.health.hediffSet == null && target_pawn.Dead) return false;
            }

            // ここからは監視対象のポーンが切り替わった場合の値を控える場所

            // 監視対象のポーンを控えておく
            tracked_pawn = target_pawn;

            last_downed_state = tracked_pawn?.health?.Downed ?? false;
            return true;
        }
    }
}
