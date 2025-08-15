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

        /// <summary>
        /// Flip DXT1 compressed data vertically
        /// </summary>
        /// <param name="data">DXT1 compressed DDS byte array (excluding header)</param>
        /// <param name="width">Texture width (multiple of 4)</param>
        /// <param name="height">Texture height (multiple of 4)</param>
        public static void FlipDXT1(byte[] data, int width, int height)
        {
            // Source:https://hub.jmonkeyengine.org/t/dds-texture-flip-problem/4448/3
            int block_size = 8;
            int blocks_per_row = width / 4;
            int blocks_per_col = height / 4;

            byte[] rowBuffer = new byte[block_size];
            // Swap block rows vertically
            for (int y = 0; y < blocks_per_col / 2; y++)
            {
                for (int x = 0; x < blocks_per_row; x++)
                {
                    int topIndex = (y * blocks_per_row + x) * block_size;
                    int bottomIndex = ((blocks_per_col - 1 - y) * blocks_per_row + x) * block_size;
                    // Swap top and bottom blocks
                    Buffer.BlockCopy(data, topIndex, rowBuffer, 0, block_size);
                    Buffer.BlockCopy(data, bottomIndex, data, topIndex, block_size);
                    Buffer.BlockCopy(rowBuffer, 0, data, bottomIndex, block_size);
                }
            }

            // Flip rows inside each block
            for (int i = 0; i < data.Length; i += block_size)
            {
                // DXT1 block structure: 16bit palette[2] + 32bit data
                // Flip the data part (4x4 pixels, 2 bits per pixel) vertically
                // data is the last 4 bytes
                uint data32 = BitConverter.ToUInt32(data, i + 4);

                // Extract 4 rows (each 2 bits x 4 pixels = 1 byte)
                uint row0 = (data32 >> 0) & 0xFF;
                uint row1 = (data32 >> 8) & 0xFF;
                uint row2 = (data32 >> 16) & 0xFF;
                uint row3 = (data32 >> 24) & 0xFF;

                // Rearrange rows in reversed order
                uint flipped = (row3 << 0) | (row2 << 8) | (row1 << 16) | (row0 << 24);

                byte[] flipped_bytes = BitConverter.GetBytes(flipped);
                Array.Copy(flipped_bytes, 0, data, i + 4, 4);
            }
        }

        /// <summary>
        /// Flip DXT5 compressed data vertically, including alpha
        /// </summary>
        /// <param name="data">DXT5 compressed DDS byte array (excluding header)</param>
        /// <param name="width">Texture width (multiple of 4)</param>
        /// <param name="height">Texture height (multiple of 4)</param>
        public static void FlipDXT5(byte[] data, int width, int height)
        {
            int block_size = 16; // DXT5: 1 block = 16 bytes
            int blocks_per_row = width / 4;
            int blocks_per_col = height / 4;

            byte[] row_buffer = new byte[block_size];

            // Swap block rows vertically
            for (int y = 0; y < blocks_per_col / 2; y++)
            {
                for (int x = 0; x < blocks_per_row; x++)
                {
                    int top_index = (y * blocks_per_row + x) * block_size;
                    int bottom_index = ((blocks_per_col - 1 - y) * blocks_per_row + x) * block_size;

                    // Swap top and bottom blocks
                    Buffer.BlockCopy(data, top_index, row_buffer, 0, block_size);
                    Buffer.BlockCopy(data, bottom_index, data, top_index, block_size);
                    Buffer.BlockCopy(row_buffer, 0, data, bottom_index, block_size);
                }
            }

            // Flip rows inside each block (alpha + color)
            for (int i = 0; i < data.Length; i += block_size)
            {
                // --- Alpha part (first 8 bytes) ---
                ulong alpha_bits = 0;
                for (int j = 0; j < 6; j++)
                    alpha_bits |= ((ulong)data[i + 2 + j]) << (8 * j);

                ulong row_0 = alpha_bits & 0xFFF;
                ulong row_1 = (alpha_bits >> 12) & 0xFFF;
                ulong row_2 = (alpha_bits >> 24) & 0xFFF;
                ulong row_3 = (alpha_bits >> 36) & 0xFFF;

                ulong flipped_alpha = (row_3) | (row_2 << 12) | (row_1 << 24) | (row_0 << 36);

                for (int j = 0; j < 6; j++)
                    data[i + 2 + j] = (byte)((flipped_alpha >> (8 * j)) & 0xFF);

                // --- Color part (last 8 bytes) ---
                uint color_data = BitConverter.ToUInt32(data, i + 12);

                uint c_row_0 = (color_data >> 0) & 0xFF;
                uint c_row_1 = (color_data >> 8) & 0xFF;
                uint c_row_2 = (color_data >> 16) & 0xFF;
                uint c_row_3 = (color_data >> 24) & 0xFF;

                uint flipped_color = (c_row_3 << 0) | (c_row_2 << 8) | (c_row_1 << 16) | (c_row_0 << 24);

                byte[] flipped_color_bytes = BitConverter.GetBytes(flipped_color);
                Array.Copy(flipped_color_bytes, 0, data, i + 12, 4);
            }
        }
    }
}
