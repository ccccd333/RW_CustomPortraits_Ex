using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class Textures
    {
        public Textures() { }
        public Textures(Textures other)
        {
            IsAnimation = other.IsAnimation;
            display_duration = other.display_duration;

            // Listのディープコピー（参照を共有したくない場合）
            txs = new List<Texture2D>(other.txs);

            file_path = other.file_path;
            file_base_path = other.file_base_path;
            file_path_first = other.file_path_first;
            file_path_second = other.file_path_second;
            d = other.d;
        }


        public bool IsAnimation = false;
        public float display_duration = 2.0f;
        public List<Texture2D> txs = new List<Texture2D>();
        public string file_path = "";
        public string file_base_path = "";
        public string file_path_first = "";
        public string file_path_second = "";
        public string d = "";
    }

    public class TextureMeta
    {
        public TextureMeta() { }

        public TextureMeta(Textures other) {
            IsAnimation = other.IsAnimation;
            display_duration = other.display_duration;
            file_path = other.file_path;
            file_base_path = other.file_base_path;
            file_path_first = other.file_path_first;
            file_path_second = other.file_path_second;
            d = other.d;
        }

        public TextureMeta(TextureMeta other)
        {
            IsAnimation = other.IsAnimation;
            display_duration = other.display_duration;
            file_path = other.file_path;
            file_base_path = other.file_base_path;
            file_path_first = other.file_path_first;
            file_path_second = other.file_path_second;
            d = other.d;
        }

        public bool IsAnimation = false;
        public float display_duration = 2.0f;
        public string file_path = "";
        public string file_base_path = "";
        public string file_path_first = "";
        public string file_path_second = "";
        public string d = ".dds";
    }
}
