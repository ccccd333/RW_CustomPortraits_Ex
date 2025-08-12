using System;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class PawnCacheEntry
    {
        public PawnCacheEntry(Pawn p, string mk, float cds)
        {
            pawn = p;
            matched_key = mk;
            cached_at = Time.realtimeSinceStartup;
            cache_duration_seconds = cds;
        }

        public Pawn pawn;
        public string matched_key = "";
        public float cached_at;
        public float cache_duration_seconds = 12.0f;
    }
}
