using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass
{
    /// <summary>
    /// LastContextName と RandValue の AND 条件。
    /// operation_base_value に { "last_context_name": "xxx", "rand_value": 50 } を渡す。
    /// 両方の条件が true の場合に override_portrait_name を返す。
    /// </summary>
    public class LastContextAndRand : OperationBase
    {
        private Operation operation;
        private string last_context_name_value;
        private int rand_threshold;

        public override bool Init(Operation op, ValidationContext vc)
        {
            // operation_base_object が必要なので通常の Init では初期化できない
            Log.Error("[PortraitsEx] LastContextAndRand: must be initialized via InitWithObject, not Init.");
            return false;
        }

        public override bool InitWithObject(Operation op, Newtonsoft.Json.Linq.JObject base_object, ValidationContext vc)
        {
            operation = op;

            if (base_object == null)
            {
                Log.Error("[PortraitsEx] LastContextAndRand: operation_base_value must be a JSON object.");
                return false;
            }

            // last_context_name を取得
            var lcn_token = base_object["last_context_name"];
            if (lcn_token == null)
            {
                Log.Error("[PortraitsEx] LastContextAndRand: operation_base_value must contain 'last_context_name'.");
                return false;
            }
            last_context_name_value = (string)lcn_token;

            if (!vc.ValidContextNames.Contains(last_context_name_value))
            {
                Log.Error($"[PortraitsEx] LastContextAndRand: valid_context_names does not contain last_context_name '{last_context_name_value}'.");
                return false;
            }

            // rand_value を取得
            var rv_token = base_object["rand_value"];
            if (rv_token == null)
            {
                Log.Error("[PortraitsEx] LastContextAndRand: operation_base_value must contain 'rand_value'.");
                return false;
            }
            rand_threshold = (int)rv_token;

            if (operation.override_portrait_name.NullOrEmpty())
            {
                Log.Error("[PortraitsEx] LastContextAndRand: override_portrait_name is null or empty.");
                return false;
            }

            if (!vc.ValidContextNames.Contains(operation.override_portrait_name))
            {
                Log.Error($"[PortraitsEx] LastContextAndRand: valid_context_names does not contain override_portrait_name '{operation.override_portrait_name}'.");
                return false;
            }

            return true;
        }

        public override bool Evaluate(EvaluationArgs arg)
        {
            // LastContextName AND RandValue の両方が true の場合に true を返す
            bool last_ctx_match = arg.LastContextName == last_context_name_value;
            if (!last_ctx_match) return false;

            int random_value = UnityEngine.Random.Range(0, 100);
            return random_value < rand_threshold;
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
