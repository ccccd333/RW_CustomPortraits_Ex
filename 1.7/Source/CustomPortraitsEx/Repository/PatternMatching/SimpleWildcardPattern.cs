using System;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching
{
    public class SimpleWildcardPattern : IPatternMatcher
    {
        private readonly string[] parts;

        public SimpleWildcardPattern(string pattern)
        {
            parts = pattern.Split(new[] { ".*" }, StringSplitOptions.None);
        }

        public bool IsMatch(string input)
        {
            int pos = 0;

            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;

                pos = input.IndexOf(part, pos, StringComparison.Ordinal);
                if (pos < 0)
                    return false;

                pos += part.Length;
            }

            return true;
        }
    }
}
