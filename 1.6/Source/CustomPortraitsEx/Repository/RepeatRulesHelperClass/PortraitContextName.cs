using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass
{
    public class PortraitContextName : OperationBase
    {
        Operation operation;
        

        public override bool Init(Operation op, ValidationContext vc)
        {
            operation = op;
            if(operation.operation_base_value.NullOrEmpty())
            {
                Log.Error("[PortraitsEx] PortraitContextName: operation_base_value is null or empty. Please check the operation configuration.");
                return false;
            }

            if(operation.override_portrait_name.NullOrEmpty())
            {
                Log.Error("[PortraitsEx] PortraitContextName: override_portrait_name is null or empty. Please check the operation configuration.");
                return false;
            }

            if (!vc.ValidContextNames.Contains(operation.operation_base_value))
            {
                Log.Error($"[PortraitsEx] PortraitContextName: valid_context_names does not contain operation_base_value {operation.operation_base_value}.");
                return false;
            }

            if (!vc.ValidContextNames.Contains(operation.override_portrait_name))
            {
                Log.Error($"[PortraitsEx] PortraitContextName: valid_context_names does not contain override_portrait_name {operation.override_portrait_name}.");
                return false;
            }

            return true;
        }

        public override bool Evaluate(EvaluationArgs arg)
        {
            return arg.ActiveContexts.Contains(operation.operation_base_value);
        }

        public override string ResolveOverrideContext()
        {
            return operation.override_portrait_name;
        }
    }
}
