using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class GroupPatternEntry
    {
        public GroupPatternEntry(string key)
        {
            this.key = key;
        }

        public string key;

        public List<string> alias_targets = new List<string>();
    }
}
