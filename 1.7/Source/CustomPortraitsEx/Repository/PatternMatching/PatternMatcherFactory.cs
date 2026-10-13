using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching
{
    public static class PatternMatcherFactory
    {
        public static IPatternMatcher Create(string pattern)
        {
            if (Utility.IsComplexRegexPattern(pattern))
                return new RegexPattern(pattern);

            return new SimpleWildcardPattern(pattern);
        }
    }
}
