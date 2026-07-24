using Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass;
using System;
using System.Collections.Generic;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class RepeatLoopSettings
    {
        public int min_count = -1;
        public int max_count = -1;
        public List<string> interrupt_contexts = new List<string>();
    }

    public class RepeatEvaluationGroup
    {
        

        // min_count～max_countの範囲で、同じポートレートコンテキストが選ばれるんだけど、
        // interrupt_contextsに含まれるものが選ばれた場合は異なるポートレートコンテキストが選ばれるようにする
        public List<string> interrupt_contexts = new List<string>();
        public List<OperationBase> operation_list = new List<OperationBase>();
    }


    // 例えばIdleが選出された際に、
    // 更にまたIdleが選出されたら10%の確立で"ずぶ濡れ"の画像に(Idle状態のままで)、
    // 20%の確立で"Idle2"の画像に、"死んだ"が成立していたら"Idle2"の画像を表示みたいな
    // 要はパチンコの演出みたいな感じ

    //{
    //  "idle": {
    //    "loop": {
    //      "min_count": 2,
    //      "max_count": 4,
    //      "interrupt_contexts": [
    //        "idle2"
    //      ]
    //    },
    //    "repeat_events": {
    //      "0": {
    //        "operations": [
    //          {
    //            "operation_type": "rand_value",
    //            "operation_base_value": "50",
    //            "override_portrait_name": "idle2"
    //          }
    //        ]
    //      },
    //      "2": {
    //        "operations": [
    //          {
    //            "operation_type": "portrait_context_name",
    //            "operation_base_value": "wet",
    //            "override_portrait_name": "idle_wet"
    //          }
    //        ],
    //        "interrupt_contexts": [
    //          "idle"
    //        ]
    //      }
    //    }
    //  }
    //}

    public class RepeatRules
    {
        public bool is_enabled = false;

        private readonly Dictionary<string, RepeatLoopSettings> loop_settings_by_context =
            new Dictionary<string, RepeatLoopSettings>();

        // 以下のようなイメージ(keyが(string, int)になってるのだけ後で読み返した際の注意)
        // key = ポートレートコンテキスト名(idleとか)
        // value = key= idleが選ばれた後再度idleが選ばれた際、番号指定で挙動を変えるためのindex番号
        //         value=オペレーション、jsonの定義内容を上から順に格納する
        // "idle" : {
        //    "1" : [ { "operation_type" : "rand_value", "inequality_sign" : "more", "value" : 50, "result_context_name" : "idle2" } ],
        //        ※これは、idleが選ばれた後、再度idleが選ばれた際に、50%の確率でidle2に変化するという意味
        //    "2" : [ { "operation_type" : "rand_value", "inequality_sign" : "more", "value" : 20, "result_context_name" : "ずぶ濡れ" } ]
        //        ※これは、またまたidleが選ばれた際に、20%の確率でwet_idleに変化するという意味

        private readonly Dictionary<(string context, int index), RepeatEvaluationGroup> operations =
            new Dictionary<(string, int), RepeatEvaluationGroup>();

        public void SetLoopSettings(string context_name, RepeatLoopSettings settings)
        {
            loop_settings_by_context[context_name] = settings;
        }

        public bool TryGetLoopSettings(string context_name, out RepeatLoopSettings result)
        {
            if (loop_settings_by_context.TryGetValue(context_name, out result))
            {
                return true;
            }

            result = null;
            return false;
        }


        public void SetOperations(string context_name, int index, RepeatEvaluationGroup operation_list)
        {
            operations[(context_name, index)] = operation_list;
        }

        public bool TryGetOperations(string context_name, int index, out RepeatEvaluationGroup result)
        {
            if (operations.TryGetValue((context_name, index), out var list))
            {
                result = list;
                return true;
            }

            result = null;
            return false;
        }

        public bool TryResolveVariantContext(
            string portrait_context_name,
            int repeat_index,
            List<string> candidate_context_names,
            string previous_context_result,
            out string resolved_context_name,
            out bool should_increment_repeat)
        {
            resolved_context_name = portrait_context_name;
            should_increment_repeat = false;

            if (!TryGetOperations(portrait_context_name, repeat_index, out var group))
            {
                return false;
            }

            if (group.interrupt_contexts.Contains(portrait_context_name))
            {
                return true;
            }

            var args = new EvaluationArgs(candidate_context_names);
            foreach (var op in group.operation_list)
            {
                if (op.Evaluate(args))
                {
                    var override_ctx = op.ResolveOverrideContext();
                    if (!string.IsNullOrEmpty(override_ctx))
                    {
                        resolved_context_name = override_ctx;
                        break;
                    }
                }
            }

            should_increment_repeat = previous_context_result == resolved_context_name;
            return true;
        }

        // 戻り値:
        //  1: 適用成功 (Applied)
        //  0: 適用外 (NotApplied / min未満 / 割り込み / 設定なし)
        // -1: 超過 (Exceeded / max_count超過)
        public int TryApplyLoopRepeatEvent(
            string bef_context_name,
            string portrait_context_name,
            int repeat_index,
            List<string> candidate_context_names,
            out string resolved_context_name)
        {
            resolved_context_name = portrait_context_name;


            if (!TryGetLoopSettings(bef_context_name, out var loop_settings))
            {
                return 0;
            }

            if (loop_settings.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            if (loop_settings.min_count > repeat_index)
            {
                return 0;
            }

            if (repeat_index > loop_settings.max_count)
            {
                return -1;
            }

            resolved_context_name = bef_context_name;

            if (!TryGetOperations(bef_context_name, repeat_index, out var group))
            {
                //resolved_context_name = bef_context_name;
                return 1;
            }

            

            var args = new EvaluationArgs(candidate_context_names);
            foreach (var op in group.operation_list)
            {
                if (op.Evaluate(args))
                {
                    var override_ctx = op.ResolveOverrideContext();
                    if (!string.IsNullOrEmpty(override_ctx))
                    {
                        resolved_context_name = override_ctx;
                        return 1;
                    }
                }
            }

            return 1;
        }
    }
}
