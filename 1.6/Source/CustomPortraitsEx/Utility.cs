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
    }
}
