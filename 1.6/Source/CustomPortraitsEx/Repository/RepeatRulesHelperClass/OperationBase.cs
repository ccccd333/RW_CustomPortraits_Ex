
using System.Collections.Generic;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass
{
    public enum OperationType
    {
        portrait_context_name,
        rand_value,
        last_context_name,
        last_context_and_rand
    }

    public enum InequalitySign
    {
        more,
        less,
        more_than,
        less_than,
        equal,
        def,
        unk
    };

    public class MultTypeValue
    {
        int i_v;
        float f_v;
        double d_v;
        string s_v;

        bool i_v_is_set = false;
        bool f_v_is_set = false;
        bool d_v_is_set = false;
        bool s_v_is_set = false;


        void SetIValue(int v)
        {
            i_v = v;
            i_v_is_set = true;
        }

        void SetFValue(float v)
        {
            f_v = v;
            f_v_is_set = true;
        }
        void SetDValue(double v)
        {
            d_v = v;
            d_v_is_set = true;
        }
        void SetSValue(string v)
        {
            s_v = v;
            s_v_is_set = true;
        }

        bool GetIValue(out int v)
        {
            if (i_v_is_set)
            {
                v = i_v;
                return true;
            }
            v = 0;
            return false;
        }

        bool GetFValue(out float v)
        {
            if (f_v_is_set)
            {
                v = f_v;
                return true;
            }
            v = 0.0f;
            return false;
        }

        bool GetDValue(out double v)
        {
            if (d_v_is_set)
            {
                v = d_v;
                return true;
            }
            v = 0.0;
            return false;
        }

        bool GetSValue(out string v)
        {
            if (s_v_is_set)
            {
                v = s_v;
                return true;
            }
            v = null;
            return false;
        }
    }

    public class Operation
    {
        public OperationType operation_type;
        public InequalitySign inequality_sign;
        public string operation_base_value;
        public string override_portrait_name;
        public int? override_min_count;
        public int? override_max_count;
        public int? override_reset_max_count;
        public List<string> interrupt_contexts = new List<string>();
    }

    public readonly struct ValidationContext
    {
        // 外部からは読み取り専用
        public List<string> ValidContextNames { get; }

        // コンストラクタで実体を注入する
        public ValidationContext(List<string> valid_context_names)
        {
            ValidContextNames = valid_context_names;
        }
    }

    public readonly struct EvaluationArgs
    {
        public List<string> ActiveContexts { get; }
        public string LastContextName { get; }
        public MultTypeValue Left { get; }
        public MultTypeValue Right { get; }

        public EvaluationArgs(
            List<string> active_contexts = null,
            string last_context_name = null,
            MultTypeValue left = null,
            MultTypeValue right = null)
        {
            ActiveContexts = active_contexts;
            LastContextName = last_context_name;
            Left = left;
            Right = right;
        }
    }

    public abstract class OperationBase
    {
        public abstract bool Init(Operation op, ValidationContext vc);

        public virtual bool InitWithObject(Operation op, Newtonsoft.Json.Linq.JObject base_object, ValidationContext vc)
        {
            return Init(op, vc);
        }

        public abstract bool Evaluate(EvaluationArgs arg);

        public abstract string ResolveOverrideContext();

        public abstract int? ResolveOverrideMinCount();

        public abstract int? ResolveOverrideMaxCount();

        public abstract int? ResolveOverrideResetMaxCount();

        public abstract bool JudgeInterruptContexts(string portrait_context_name);
    }
}
