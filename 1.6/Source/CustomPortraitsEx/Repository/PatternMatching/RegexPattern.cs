using System.Text.RegularExpressions;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching
{
    public class RegexPattern : IPatternMatcher
    {
        private readonly Regex regex;

        public RegexPattern(string pattern)
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string input)
        {
            return regex.IsMatch(input);
        }
    }
}
