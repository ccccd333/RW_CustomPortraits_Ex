
using System.Collections.Generic;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass
{
    public enum OperationType
    {
        portrait_context_name,
        rand_value
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
        public MultTypeValue Left { get; }
        public MultTypeValue Right { get; }

        public EvaluationArgs(List<string> active_contexts = null, MultTypeValue left = null, MultTypeValue right = null)
        {
            ActiveContexts = active_contexts;
            Left = left;
            Right = right;
        }
    }

    public abstract class OperationBase
    {
        public abstract bool Init(Operation op, ValidationContext vc);

        public abstract bool Evaluate(EvaluationArgs arg);

        public abstract string ResolveOverrideContext();
    }
}
