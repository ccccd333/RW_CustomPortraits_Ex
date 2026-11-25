using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Interrupt
{
    public class DownedInterruptContextResolver
    {
        Pawn tracked_pawn;
        bool last_downed_state = false;

        public void ClearTracking()
        {
            tracked_pawn = null;
            last_downed_state = false;
        }

        private static bool IsValidPawn(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && !pawn.Dead;
        }

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

            if (!IsValidPawn(tracked_pawn))
            {
                tracked_pawn = null;
                return false;
            }

            // 現在ダウンしているか
            bool downed = tracked_pawn.health.Downed;

            if (downed)
            {
                if (portrait_interrupt.monitor_behaviors.downed.trigger_on_enter)
                {
                    if (!last_downed_state)
                    {
                        last_downed_state = true;
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
            // そもそも死んでたり対象がいないならダウンを監視しない
            if (!IsValidPawn(target_pawn))
            {
                tracked_pawn = null;
                last_downed_state = false;
                return false;
            }

            // 監視対象のポーンを控えておく
            tracked_pawn = target_pawn;
            last_downed_state = tracked_pawn.health.Downed;
            return true;
        }
    }
}
