using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class Utility
    {
        public static readonly string[] n_type = new[] { ".png", ".jpeg", "jpg", ".dds" };
        public static readonly string[] d_type = new[] { ".dds" };
        public static string Delimiter(string target, out string d)
        {
            string result = target;
            d = "";
            foreach (string ext in n_type)
            {
                if (target.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    result = target.Substring(0, target.Length - ext.Length);
                    d = ext;
                    break;  // 見つかったらループ抜ける
                }
            }
            return result;
        }

        public static string DDelimiter(string target, out string d)
        {
            string result = target;
            d = "";
            foreach (string ext in d_type)
            {
                if (target.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    result = target.Substring(0, target.Length - ext.Length);
                    d = ext;
                    break;  // 見つかったらループ抜ける
                }
            }
            return result;
        }

            public static bool IsRegexPattern(string pattern)
        {
            string regexSpecialChars = @"\.|\*|\+|\?|\[|\]|\(|\)|\{|\}|\||\\|\^|\$";
            return Regex.IsMatch(pattern, regexSpecialChars);
        }

        public static void FlipDXT1(byte[] data, int width, int height)
        {
            // Source:https://hub.jmonkeyengine.org/t/dds-texture-flip-problem/4448/3
            int block_size = 8;
            int blocks_per_row = width / 4;
            int blocks_per_col = height / 4;

            byte[] rowBuffer = new byte[block_size];

            for (int y = 0; y < blocks_per_col / 2; y++)
            {
                for (int x = 0; x < blocks_per_row; x++)
                {
                    int topIndex = (y * blocks_per_row + x) * block_size;
                    int bottomIndex = ((blocks_per_col - 1 - y) * blocks_per_row + x) * block_size;

                    Buffer.BlockCopy(data, topIndex, rowBuffer, 0, block_size);
                    Buffer.BlockCopy(data, bottomIndex, data, topIndex, block_size);
                    Buffer.BlockCopy(rowBuffer, 0, data, bottomIndex, block_size);
                }
            }
        }
    }
}
