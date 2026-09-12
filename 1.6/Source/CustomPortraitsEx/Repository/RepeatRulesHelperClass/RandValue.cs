
using System;
using System.Threading;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass
{
    public class RandValue : OperationBase
    {
        private static readonly ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        Operation operation;
        int parsed_value;

        public override bool Init(Operation op, ValidationContext vc)
        {
            operation = op;
            if (operation.operation_base_value.NullOrEmpty())
            {
                Log.Error("[PortraitsEx] RandValue: operation_base_value is null or empty. Please check the operation configuration.");
                return false;
            }

            if (operation.override_portrait_name.NullOrEmpty())
            {
                Log.Error("[PortraitsEx] RandValue: override_portrait_name is null or empty. Please check the operation configuration.");
                return false;
            }

            if (!vc.ValidContextNames.Contains(operation.override_portrait_name))
            {
                Log.Error($"[PortraitsEx] RandValue: valid_context_names does not contain override_portrait_name {operation.override_portrait_name}.");
                return false;
            }

            if (!int.TryParse(operation.operation_base_value, out parsed_value))
            {
                Log.Error($"[PortraitsEx] RandValue: operation_base_value {operation.operation_base_value} is not a valid integer.");
                return false;
            }

            return true;
        }

        public override bool Evaluate(EvaluationArgs arg)
        {
            int random_value = rand.Value.Next(0, 100);
            return random_value < parsed_value;
        }

        public override string ResolveOverrideContext()
        {
            return operation.override_portrait_name;
        }

        public override int? ResolveOverrideMinCount()
        {
            return operation.override_min_count;
        }

        public override int? ResolveOverrideMaxCount()
        {
            return operation.override_max_count;
        }

        public override int? ResolveOverrideResetMaxCount()
        {
            return operation.override_reset_max_count;
        }

        public override bool JudgeInterruptContexts(string portrait_context_name)
        {
            return operation.interrupt_contexts.Contains(portrait_context_name);
        }
    }
}
