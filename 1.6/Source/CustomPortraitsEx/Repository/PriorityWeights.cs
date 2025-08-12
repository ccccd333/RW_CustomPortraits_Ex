using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public enum PriorityWeightCategory
    { 
        RefName,
        GroupName
    }

    public class PriorityWeights
    {
        public PriorityWeightCategory category = PriorityWeightCategory.RefName;
        public string filter_name;
        public int weight = 100;
    }
}
