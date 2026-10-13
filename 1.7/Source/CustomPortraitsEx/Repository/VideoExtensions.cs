using System;

namespace UnityEngine.Video
{
    public static class VideoPlayerExtensions
    {
        public static void StepForward(this VideoPlayer player)
        {
            if (player == null) return;
            if (player.frameCount > 0 && player.frame < (long)player.frameCount - 1)
            {
                player.frame++;
            }
        }
    }
}
