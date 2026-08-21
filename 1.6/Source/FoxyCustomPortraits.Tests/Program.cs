using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FoxyCustomPortraits.Tests
{
    public enum OperationType
    {
        portrait_context_name,
        rand_value,
        last_context_name
    }

    public enum InequalitySign
    {
        more, less, more_than, less_than, equal, def, unk
    }

    public class MultTypeValue { }

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
        public List<string> ValidContextNames { get; }
        public ValidationContext(List<string> valid_context_names)
        {
            ValidContextNames = valid_context_names;
        }
    }

    public readonly struct EvaluationArgs
    {
        public List<string> ActiveContexts { get; }
        public string LastContextName { get; }

        public EvaluationArgs(List<string> active_contexts = null, string last_context_name = null)
        {
            ActiveContexts = active_contexts;
            LastContextName = last_context_name;
        }
    }

    public abstract class OperationBase
    {
        

        public abstract bool Init(Operation op, ValidationContext vc);
        public abstract bool Evaluate(EvaluationArgs arg);
        public abstract string ResolveOverrideContext();
        public abstract int? ResolveOverrideMinCount();
        public abstract int? ResolveOverrideMaxCount();
        public abstract int? ResolveOverrideResetMaxCount();

        public abstract bool JudgeInterruptContexts(string portrait_context_name);
    }

    public class PortraitContextName : OperationBase
    {
        Operation operation;
        public override bool Init(Operation op, ValidationContext vc)
        {
            operation = op;
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

    public class RandValue : OperationBase
    {
        Operation operation;
        int parsed_value;
        private static Random rand = new Random(42);
        public override bool Init(Operation op, ValidationContext vc)
        {
            operation = op;
            int.TryParse(operation.operation_base_value, out parsed_value);
            return true;
        }
        public override bool Evaluate(EvaluationArgs arg)
        {
            int random_value = rand.Next(0, 100);
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

    public class LastContextName : OperationBase
    {
        Operation operation;

        public override bool Init(Operation op, ValidationContext vc)
        {
            operation = op;
            return true;
        }

        public override bool Evaluate(EvaluationArgs arg)
        {
            return arg.LastContextName == operation.operation_base_value;
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

    public class RepeatLoopSettings
    {
        public int min_count = -1;
        public int max_count = -1;
        public int reset_max_count = -1;
        public List<string> interrupt_contexts = new List<string>();
    }

    public class RepeatEvaluationGroup
    {
        public List<string> interrupt_contexts = new List<string>();
        public List<OperationBase> operation_list = new List<OperationBase>();
    }

    public class RepeatRules
    {
        public const string CONTEXT_BREAK = "[BREAK]";
        public bool is_enabled = false;
        private readonly Dictionary<string, RepeatLoopSettings> loop_settings_by_context = new Dictionary<string, RepeatLoopSettings>();
        private readonly Dictionary<(string context, int index), RepeatEvaluationGroup> operations = new Dictionary<(string, int), RepeatEvaluationGroup>();

        public void SetLoopSettings(string context_name, RepeatLoopSettings settings)
        {
            loop_settings_by_context[context_name] = settings;
        }

        public bool TryGetLoopSettings(string context_name, out RepeatLoopSettings result)
        {
            return loop_settings_by_context.TryGetValue(context_name, out result);
        }

        public void SetOperations(string context_name, int index, RepeatEvaluationGroup operation_list)
        {
            operations[(context_name, index)] = operation_list;
        }

        public bool TryGetOperations(string context_name, int index, out RepeatEvaluationGroup result)
        {
            return operations.TryGetValue((context_name, index), out result);
        }

        private static bool IsBelowMinCount(RepeatLoopSettings settings, int repeat_index, int? override_min_count)
        {
            int min_count = override_min_count ?? settings.min_count;
            return min_count >= 0 && repeat_index < min_count;
        }

        private static bool IsAboveMaxCount(RepeatLoopSettings settings, int repeat_index, int? override_max_count)
        {
            int max_count = override_max_count ?? settings.max_count;
            return max_count >= 0 && repeat_index > max_count;
        }

        private static bool IsAboveResetMaxCount(
            RepeatLoopSettings settings,
            int repeat_index,
            int? override_max_count,
            int? override_reset_max_count)
        {
            int reset_max_count = override_reset_max_count
                ?? (settings.reset_max_count >= 0
                    ? settings.reset_max_count
                    : (override_max_count ?? settings.max_count));
            return reset_max_count >= 0 && repeat_index > reset_max_count;
        }

        public int TryResolveVariantContext(
            string portrait_context_name,
            int repeat_index,
            List<string> candidate_context_names,
            string previous_context_result,
            string last_context_name,
            out string resolved_context_name,
            out bool should_increment_repeat,
            out int? applied_override_min_count,
            out int? applied_override_max_count,
            out int? applied_override_reset_max_count)
        {
            resolved_context_name = portrait_context_name;
            should_increment_repeat = false;
            applied_override_min_count = null;
            applied_override_max_count = null;
            applied_override_reset_max_count = null;

            if (!TryGetOperations(portrait_context_name, repeat_index, out var group))
            {
                return -1;
            }

            if (group.interrupt_contexts.Contains(CONTEXT_BREAK) ||
                group.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            var args = new EvaluationArgs(candidate_context_names, last_context_name);
            foreach (var op in group.operation_list)
            {
                if (op.Evaluate(args))
                {
                    if (op.JudgeInterruptContexts(CONTEXT_BREAK) || op.JudgeInterruptContexts(portrait_context_name))
                    {
                        return 0;
                    }

                    var override_ctx = op.ResolveOverrideContext();
                    if (!string.IsNullOrEmpty(override_ctx))
                    {
                        applied_override_min_count = op.ResolveOverrideMinCount();
                        applied_override_max_count = op.ResolveOverrideMaxCount();
                        applied_override_reset_max_count = op.ResolveOverrideResetMaxCount();
                        resolved_context_name = override_ctx;
                        break;
                    }
                }
            }

            should_increment_repeat = previous_context_result == resolved_context_name;
            return 1;
        }

        public int TryApplyLoopRepeatEvent(
            string bef_context_name,
            string portrait_context_name,
            string last_override_context,
            int repeat_index,
            List<string> candidate_context_names,
            int? override_min_count,
            int? override_max_count,
            int? override_reset_max_count,
            out string resolved_context_name,
            out int? applied_override_min_count,
            out int? applied_override_max_count,
            out int? applied_override_reset_max_count)
        {
            resolved_context_name = portrait_context_name;
            applied_override_min_count = null;
            applied_override_max_count = null;
            applied_override_reset_max_count = null;


            if (!TryGetLoopSettings(bef_context_name, out var loop_settings))
            {
                return 0;
            }

            if (IsBelowMinCount(loop_settings, repeat_index, override_min_count))
            {
                return portrait_context_name == bef_context_name ? 1 : 0;
            }

            if (IsAboveMaxCount(loop_settings, repeat_index, override_max_count))
            {
                if (portrait_context_name != bef_context_name)
                {
                    return 0;
                }

                if (IsAboveResetMaxCount(loop_settings, repeat_index, override_max_count, override_reset_max_count))
                {
                    return -1;
                }
            }

            // ループ設定はあるけど、repeat_indexに対応する操作がない場合は、min_countとmax_countの範囲で適用するかどうかを判断する
            // max_countを超えている場合は、ループが切れているためportrait_context_nameの内容で実行

            resolved_context_name = bef_context_name;

            if (!TryGetOperations(bef_context_name, repeat_index, out var group))
            {

                return 1;
            }

            if (group.interrupt_contexts.Contains(CONTEXT_BREAK) || group.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            if (loop_settings.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            var args = new EvaluationArgs(candidate_context_names, last_override_context);
            foreach (var op in group.operation_list)
            {
                if (op.Evaluate(args))
                {
                    if (op.JudgeInterruptContexts(CONTEXT_BREAK) || op.JudgeInterruptContexts(portrait_context_name))
                    {
                        return 0;
                    }

                    var override_ctx = op.ResolveOverrideContext();
                    if (!string.IsNullOrEmpty(override_ctx))
                    {
                        applied_override_min_count = op.ResolveOverrideMinCount();
                        applied_override_max_count = op.ResolveOverrideMaxCount();
                        applied_override_reset_max_count = op.ResolveOverrideResetMaxCount();
                        resolved_context_name = override_ctx;
                        return 1;
                    }
                }
            }

            // ループ設定でrepeat_indexに対応する操作がある場合は、割り込みコンテキストの判定を行う
            if (group.interrupt_contexts.Contains(CONTEXT_BREAK) || group.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            if (loop_settings.interrupt_contexts.Contains(portrait_context_name))
            {
                return 0;
            }

            // repeat_indexに対応する操作の割り込みコンテキスト判定を行った後、min_countとmax_countの範囲で適用するかどうかを判断する
            // これは操作側より先にmin~max判定するとこっちが評価されないためである。なんのために操作側に抜ける条件書いたのってなるので。
            return 1;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting repeat_rules test (standalone)...");

            string jsonFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "RepeatRulesTestPatterns.json");
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"Repeat rules JSON not found at '{jsonFilePath}'.", jsonFilePath);
            }

            string json = File.ReadAllText(jsonFilePath);
            Console.WriteLine($"Loaded repeat rules from: {jsonFilePath}");

            RepeatRules repeat_rules = new RepeatRules();
            repeat_rules.is_enabled = true;

            List<string> validNames = new List<string>
            {
                "Idle", "Idle1t1", "Idle1t2", "Walk", "CombatContext",
                "Chain", "ChainEx1", "ChainEx2", "ChainEx3",
                "Other", "OtherEx", "Boundary", "Boundary0", "Boundary1", "Boundary2",
                "MinCountOverride", "MinEx1", "MinEx2", "MinEx3", "MinEx4", "Interrupt",
                "OpInterruptTest", "OpInt0", "OpInt1", "OpInt2", "Enemy"
            };
            ValidationContext vc = new ValidationContext(validNames);

            JObject root = JObject.Parse(json);
            foreach (var contextToken in root)
            {
                string contextName = contextToken.Key;
                JObject repeatObject = (JObject)contextToken.Value;

                if (repeatObject.TryGetValue("loop", out JToken loopToken) && loopToken is JObject loopObject)
                {
                    RepeatLoopSettings loopSettings = new RepeatLoopSettings();
                    if (loopObject.TryGetValue("min_count", out JToken minToken))
                        loopSettings.min_count = minToken.Value<int>();
                    if (loopObject.TryGetValue("max_count", out JToken maxToken))
                        loopSettings.max_count = maxToken.Value<int>();
                    if (loopObject.TryGetValue("reset_max_count", out JToken resetMaxToken))
                        loopSettings.reset_max_count = resetMaxToken.Value<int>();
                    if (loopObject.TryGetValue("interrupt_contexts", out JToken intrToken) && intrToken is JArray intrArray)
                    {
                        foreach (var item in intrArray)
                            loopSettings.interrupt_contexts.Add(item.ToString());
                    }
                    repeat_rules.SetLoopSettings(contextName, loopSettings);
                }

                if (repeatObject.TryGetValue("repeat_events", out JToken repeatEventsToken) && repeatEventsToken is JObject repeatEventsObject)
                {
                    foreach (var repeatToken in repeatEventsObject)
                    {
                        int repeatIndex = int.Parse(repeatToken.Key);
                        RepeatEvaluationGroup group = LoadRepeatEvaluationGroup(repeatToken.Value, vc);
                        repeat_rules.SetOperations(contextName, repeatIndex, group);
                    }
                }
            }

            Console.WriteLine("JSON loaded successfully.");

            try
            {
                Console.WriteLine("\n--- SIMULATION 1: Enter 'Idle' then 'Walk' (should not interrupt) then 'CombatContext' (should interrupt) ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Idle", new List<string>()),
                    new SimulationInput("Walk", new List<string>()),
                    new SimulationInput("Walk", new List<string>()),
                    new SimulationInput("CombatContext", new List<string>()),
                    new SimulationInput("Idle", new List<string>()),
                });

                Console.WriteLine("\n--- SIMULATION 2: Enter 'Idle' then change to 'sad' then back to 'Idle' ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("sad", new List<string>()),
                    new SimulationInput("sad", new List<string>()),
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                });

                Console.WriteLine("\n--- SIMULATION 3:  ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("wet", new List<string> { "wet skin" }),
                    new SimulationInput("idle3", new List<string> { "wet skin" }),
                    new SimulationInput("Idle", new List<string> { "wet skin" }),
                    new SimulationInput("idle3", new List<string> { "wet skin" }),
                    new SimulationInput("Idle", new List<string> { "wet skin" })
                });

                Console.WriteLine("\n--- TEST: Chain with operation-level max/reset overrides ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>())
                });

                Console.WriteLine("\n--- TEST: Different context after max_count starts its own sequence ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Chain", new List<string>()),
                    new SimulationInput("Other", new List<string>()),
                    new SimulationInput("Other", new List<string>())
                });

                Console.WriteLine("\n--- TEST: max_count and reset_max_count boundaries are inclusive ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("Boundary", new List<string>()),
                    new SimulationInput("Boundary", new List<string>()),
                    new SimulationInput("Boundary", new List<string>()),
                    new SimulationInput("Boundary", new List<string>())
                });

                Console.WriteLine("\n--- TEST: override_min_count extends the minimum threshold ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("MinCountOverride", new List<string>()), // repeat=0 => overrides min_count to 4
                    new SimulationInput("MinCountOverride", new List<string>()), // repeat=1 => should continue since min_count is 4
                    new SimulationInput("Interrupt", new List<string>()),        // repeat=2 => should still be blocked from interrupting if min_count is respected... wait, IsBelowMinCount ignores different contexts? Let's stay in context
                    new SimulationInput("MinCountOverride", new List<string>()), // repeat=2 => should continue
                    new SimulationInput("MinCountOverride", new List<string>()), // repeat=3 => should continue
                    new SimulationInput("MinCountOverride", new List<string>()), // repeat=4 => should exit min_count loop
                });

                Console.WriteLine("\n--- TEST: operation level interrupt_contexts breaks the base context ---");
                RunSimulation(repeat_rules, new List<SimulationInput>
                {
                    new SimulationInput("OpInterruptTest", new List<string>()), // repeat=0, min_count is 1, operation 0 returns OpInt0
                    new SimulationInput("Enemy", new List<string>()),           // repeat=1, max_count is 5. We are BETWEEN min and max. It evaluates op 1. op 1 has interrupt_contexts=["Enemy"]. Returns 0 (interrupt).
                    new SimulationInput("OpInterruptTest", new List<string>()), // repeat=1 for Enemy. Then repeat=0 for OpInterruptTest again.
                    new SimulationInput("OpInterruptTest", new List<string>())  // repeat=1, but NO enemy context this time, should evaluate op 1 normally.
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nEXCEPTION: {ex.GetType().FullName}: {ex.Message}");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        static RepeatEvaluationGroup LoadRepeatEvaluationGroup(JToken groupToken, ValidationContext vc)
        {
            RepeatEvaluationGroup group = new RepeatEvaluationGroup();
            JToken operationsToken = groupToken;

            if (groupToken is JObject groupObject)
            {
                if (groupObject.TryGetValue("interrupt_contexts", out JToken intrToken) && intrToken is JArray intrArray)
                {
                    foreach (var item in intrArray)
                        group.interrupt_contexts.Add(item.ToString());
                }
                if (groupObject.TryGetValue("operations", out JToken ops))
                    operationsToken = ops;
            }

            JArray opArray = (JArray)operationsToken;
            foreach (var opToken in opArray)
            {
                JObject opObj = (JObject)opToken;
                Operation op = new Operation();
                op.operation_type = (OperationType)Enum.Parse(typeof(OperationType), opObj.Value<string>("operation_type"));
                op.operation_base_value = opObj.Value<string>("operation_base_value");
                op.override_portrait_name = opObj.Value<string>("override_portrait_name");
                if (opObj.TryGetValue("override_min_count", out JToken overrideMinToken))
                    op.override_min_count = overrideMinToken.Value<int>();
                if (opObj.TryGetValue("override_max_count", out JToken overrideMaxToken))
                    op.override_max_count = overrideMaxToken.Value<int>();
                if (opObj.TryGetValue("override_reset_max_count", out JToken overrideResetMaxToken))
                    op.override_reset_max_count = overrideResetMaxToken.Value<int>();

                OperationBase opInstance = null;
                if (op.operation_type == OperationType.portrait_context_name)
                    opInstance = new PortraitContextName();
                else if (op.operation_type == OperationType.rand_value)
                    opInstance = new RandValue();
                else if (op.operation_type == OperationType.last_context_name)
                    opInstance = new LastContextName();
                
                if (opObj.TryGetValue("interrupt_contexts", out JToken intrToken) && intrToken is JArray intrArray)
                {
                    foreach (var item in intrArray)
                        op.interrupt_contexts.Add(item.ToString());
                }

                opInstance.Init(op, vc);
                group.operation_list.Add(opInstance);
            }
            return group;
        }

        struct SimulationInput
        {
            public string context_name;
            public List<string> candidate_contexts;

            public SimulationInput(string ctx, List<string> cand)
            {
                context_name = ctx;
                candidate_contexts = cand;
            }
        }

        static void RunSimulation(RepeatRules repeat_rules, List<SimulationInput> inputs)
        {
            string repeat_base_context = null;
            int repeat_count = 0;
            string pending_context_result = null;
            int? repeat_override_min_count = null;
            int? repeat_override_max_count = null;
            int? repeat_override_reset_max_count = null;

            int step = 1;
            foreach (var input in inputs)
            {
                string portrait_context_name = input.context_name;
                List<string> candidate_context_names = input.candidate_contexts;
                bool is_resolved = true;
                bool is_repeat = false;

                Console.WriteLine($"[Step {step}] Input Context: '{portrait_context_name}', Candidates: [{string.Join(", ", candidate_context_names)}]");
                Console.WriteLine($"   Before: repeat_count = {repeat_count}, repeat_base_context = '{(repeat_base_context ?? "null")}'");

                // repeat_rulesの事前評価(主にリピート)
                if (repeat_rules.is_enabled)
                {
                    if (repeat_base_context != null)
                    {
                        int repeat_index = repeat_count;
                        int loop_result = repeat_rules.TryApplyLoopRepeatEvent(
                            repeat_base_context,
                            portrait_context_name,
                            pending_context_result,
                            repeat_index,
                            candidate_context_names,
                            repeat_override_min_count,
                            repeat_override_max_count,
                            repeat_override_reset_max_count,
                            out var loop_resolved_context_name,
                            out var applied_override_min_count,
                            out var applied_override_max_count,
                            out var applied_override_reset_max_count);

                        if (loop_result == 1)
                        {
                            pending_context_result = loop_resolved_context_name;
                            if (applied_override_min_count.HasValue)
                                repeat_override_min_count = applied_override_min_count;
                            if (applied_override_max_count.HasValue)
                                repeat_override_max_count = applied_override_max_count;
                            if (applied_override_reset_max_count.HasValue)
                                repeat_override_reset_max_count = applied_override_reset_max_count;
                            is_repeat = true;
                            ++repeat_count;
                        }
                        else if (loop_result == -1)
                        {
                            repeat_count = 0;
                            repeat_override_min_count = null;
                            repeat_override_max_count = null;
                            repeat_override_reset_max_count = null;
                        }

                        Console.WriteLine($"[Step {step}] TryApplyLoopRepeatEvent result: {loop_result}, resolved_context_name: '{loop_resolved_context_name}', repeat_count: {repeat_count}");
                
                    }
                }

                // repeat_rulesの事後評価
                if (!is_repeat)
                {
                    if(is_resolved && repeat_rules.is_enabled){
                        // 同じコンテキストかどうかを判定する (前回の元のコンテキスト名 repeat_base_context と今回の元のコンテキスト名 portrait_context_name が同じか)
                        bool is_same_context = (repeat_base_context != null && portrait_context_name == repeat_base_context);
                        if (!is_same_context)
                        {
                            repeat_count = 0;
                            repeat_override_min_count = null;
                            repeat_override_max_count = null;
                            repeat_override_reset_max_count = null;
                        }

                        int repeat_index = repeat_count;

                        int loop_result = repeat_rules.TryResolveVariantContext(
                            portrait_context_name,
                            repeat_index,
                            candidate_context_names,
                            repeat_base_context,
                            pending_context_result,
                            out var resolved_context_name,
                            out var should_increment_repeat,
                            out var applied_override_min_count,
                            out var applied_override_max_count,
                            out var applied_override_reset_max_count);
                        if (loop_result == 1)
                        {
                            if (is_same_context)
                            {
                                repeat_count++;
                            }
                            else
                            {
                                repeat_count = 1;
                                repeat_base_context = portrait_context_name;
                            }

                            if (applied_override_min_count.HasValue)
                                repeat_override_min_count = applied_override_min_count;
                            if (applied_override_max_count.HasValue)
                                repeat_override_max_count = applied_override_max_count;
                            if (applied_override_reset_max_count.HasValue)
                                repeat_override_reset_max_count = applied_override_reset_max_count;

                            portrait_context_name = resolved_context_name;
                        }
                        else if (loop_result == -1)
                        {
                            // 該当する repeat_rules 操作がない場合
                            if (!is_same_context)
                            {
                                repeat_count = 1;
                                repeat_base_context = portrait_context_name;
                            }
                            else
                            {
                                // 同じコンテキスト名が続いているが、リピートイベントが存在しない場合
                                repeat_count++;
                            }
                        }else if (loop_result == 0)
                        {
                            // interrupt_contexts に該当する場合
                                repeat_count = 1;
                                repeat_base_context = portrait_context_name;
                        }
                    }
                    pending_context_result = portrait_context_name;
                }

                Console.WriteLine($"   After:  repeat_count = {repeat_count}, repeat_base_context = '{(repeat_base_context ?? "null")}', Result: '{pending_context_result}'");
                Console.WriteLine();
                step++;
            }

        }
    }
}
