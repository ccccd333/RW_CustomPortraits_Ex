using UnityEngine;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class VideoEntry
    {
        public float display_duration = 5.0f;

        public bool loop = true;

        public string file_path = "";

        public string fallback_texture_path = "";
        public Texture2D fallback_texture = null;
    }
}