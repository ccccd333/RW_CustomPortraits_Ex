using System;
using System.Text.RegularExpressions;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class Utility
    {
        public static readonly string[] n_type = new[] { ".png", ".jpeg", ".jpg", ".dds" };
        public static readonly string[] d_type = new[] { ".dds" };

        private static readonly Regex RegexSpecialChars =
            new Regex(@"\.|\*|\+|\?|\[|\]|\(|\)|\{|\}|\||\\|\^|\$", RegexOptions.Compiled);

        private static readonly Regex ComplexRegexChars =
            new Regex(@"(?<!\.)\.(?!\*)|\+|\?|\[|\]|\(|\)|\{|\}|\||\\|\^|\$", RegexOptions.Compiled);

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
                    break;
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
                    break;
                }
            }
            return result;
        }

        public static bool IsRegexPattern(string pattern)
        {
            return !string.IsNullOrEmpty(pattern) && RegexSpecialChars.IsMatch(pattern);
        }

        public static bool IsComplexRegexPattern(string pattern)
        {
            return !string.IsNullOrEmpty(pattern) && ComplexRegexChars.IsMatch(pattern);
        }

        /// <summary>
        /// Flip DXT1 compressed Mip0 data vertically without per-block heap allocations.
        /// </summary>
        public static void FlipDXT1(byte[] data, int width, int height)
        {
            const int block_size = 8;
            int blocks_per_row = (width + 3) / 4;
            int blocks_per_col = (height + 3) / 4;
            int mip0_length = Math.Min(data.Length, blocks_per_row * blocks_per_col * block_size);
            int row_bytes = blocks_per_row * block_size;

            SwapBlockRows(data, row_bytes, blocks_per_col);

            // Flip 4x4 pixel rows inside each block (last 4 bytes: 1 byte per row)
            for (int i = 0; i + block_size <= mip0_length; i += block_size)
            {
                byte r0 = data[i + 4];
                byte r1 = data[i + 5];
                data[i + 4] = data[i + 7];
                data[i + 5] = data[i + 6];
                data[i + 6] = r1;
                data[i + 7] = r0;
            }
        }

        /// <summary>
        /// Flip DXT5 compressed Mip0 data vertically (including alpha) without per-block heap allocations.
        /// </summary>
        public static void FlipDXT5(byte[] data, int width, int height)
        {
            const int block_size = 16;
            int blocks_per_row = (width + 3) / 4;
            int blocks_per_col = (height + 3) / 4;
            int mip0_length = Math.Min(data.Length, blocks_per_row * blocks_per_col * block_size);
            int row_bytes = blocks_per_row * block_size;

            SwapBlockRows(data, row_bytes, blocks_per_col);

            // Flip rows inside each block (alpha + color)
            for (int i = 0; i + block_size <= mip0_length; i += block_size)
            {
                // --- Alpha part (bytes 2..7: 4 rows x 12 bits) ---
                ulong alpha_bits =
                    ((ulong)data[i + 2]) |
                    ((ulong)data[i + 3] << 8) |
                    ((ulong)data[i + 4] << 16) |
                    ((ulong)data[i + 5] << 24) |
                    ((ulong)data[i + 6] << 32) |
                    ((ulong)data[i + 7] << 40);

                ulong row_0 = alpha_bits & 0xFFFuL;
                ulong row_1 = (alpha_bits >> 12) & 0xFFFuL;
                ulong row_2 = (alpha_bits >> 24) & 0xFFFuL;
                ulong row_3 = (alpha_bits >> 36) & 0xFFFuL;

                ulong flipped_alpha = row_3 | (row_2 << 12) | (row_1 << 24) | (row_0 << 36);

                data[i + 2] = (byte)flipped_alpha;
                data[i + 3] = (byte)(flipped_alpha >> 8);
                data[i + 4] = (byte)(flipped_alpha >> 16);
                data[i + 5] = (byte)(flipped_alpha >> 24);
                data[i + 6] = (byte)(flipped_alpha >> 32);
                data[i + 7] = (byte)(flipped_alpha >> 40);

                // --- Color part (bytes 12..15: 1 byte per row) ---
                byte c0 = data[i + 12];
                byte c1 = data[i + 13];
                data[i + 12] = data[i + 15];
                data[i + 13] = data[i + 14];
                data[i + 14] = c1;
                data[i + 15] = c0;
            }
        }

        private static void SwapBlockRows(byte[] data, int row_bytes, int blocks_per_col)
        {
            if (blocks_per_col < 2 || row_bytes <= 0)
                return;

            Span<byte> temp = row_bytes <= 4096
                ? stackalloc byte[row_bytes]
                : new byte[row_bytes];

            int top = 0;
            int bottom = blocks_per_col - 1;
            while (top < bottom)
            {
                int top_offset = top * row_bytes;
                int bottom_offset = bottom * row_bytes;
                if (bottom_offset + row_bytes <= data.Length)
                {
                    Span<byte> top_span = data.AsSpan(top_offset, row_bytes);
                    Span<byte> bottom_span = data.AsSpan(bottom_offset, row_bytes);
                    top_span.CopyTo(temp);
                    bottom_span.CopyTo(top_span);
                    temp.CopyTo(bottom_span);
                }
                top++;
                bottom--;
            }
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            path = path.TrimEnd('/');
            return path + "/";
        }
    }
}
