using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching
{
    public interface IPatternMatcher
    {
        bool IsMatch(string input);
    }
}
