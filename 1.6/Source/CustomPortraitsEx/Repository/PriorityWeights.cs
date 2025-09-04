using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public enum PriorityWeightCategory
    { 
        RefName,
        GroupName
    }

    public class PriorityWeights
    {
        public PriorityWeights()
        {

        }

        public PriorityWeights Clone()
        {
            PriorityWeights pw = new PriorityWeights();
            pw.weight = this.weight;
            pw.filter_name = this.filter_name;
            return pw;

        }

        public PriorityWeightCategory category = PriorityWeightCategory.RefName;
        public string filter_name;
        public int weight = 100;
    }
}
