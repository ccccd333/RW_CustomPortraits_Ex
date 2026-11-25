using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class ConditionDrivenPortrait
    {
        private static readonly List<Texture2D> EmptyTextures = new List<Texture2D>(0);
        private static int repeat_count = 0;
        private static List<Texture2D> temp = EmptyTextures;
        private static int temp_index = 0;
        private static string temp_refs_key = "";
        private static bool temp_animation_mode = false;
        private static string temp_preset_name = "";
        private static float temp_display_duration = 2.0f;

        // ---- 同一描画フレーム内の OnGUI 重複評価防止用キャッシュ ----
        private static int _last_evaluated_frame = -1;
        private static Pawn _last_evaluated_pawn = null;
        private static string _last_evaluated_filename = null;
        private static Texture2D _last_frame_texture = null;

        // ---- ビデオ再生用 -----------------------------------------------
        /// <summary>現在ビデオを再生中かどうか。</summary>
        private static bool temp_is_video = false;
        /// <summary>現在再生中のビデオがループかどうか（false = 1回再生後に次へ）。</summary>
        private static bool temp_video_loop = true;
        /// <summary>現在アクティブなキャッシュ済み VideoPlayer。null の場合はシングルトン方式。</summary>
        private static Repository.CachedVideoPlayer temp_cached_video_player = null;

        /// <summary>現在いずれかの方法でビデオモードがアクティブかどうか（非同期 Prepare 完了前も true を維持してチラつきを防ぐ）</summary>
        public static bool IsVideoActive => temp_is_video && (temp_cached_video_player != null || Repository.VideoPlayerManager.HasInstance);

        /// <summary>アクティブなビデオから現在のテクスチャを取得する</summary>
        public static UnityEngine.Texture GetActiveVideoTexture(UnityEngine.Texture defaultFallback = null)
        {
            TickVideoStepForward();

            if (temp_cached_video_player != null)
                return temp_cached_video_player.GetTexture(defaultFallback);
            return Repository.VideoPlayerManager.Instance.GetTexture(defaultFallback);
        }
        // ----------------------------------------------------------------

        // ---- FPS計測 & ステップモード ------------------------------------
        /// <summary>現在のビデオがステップモードで再生中か</summary>
        private static bool temp_video_step_mode = false;

        private static int _last_step_frame = -1;

        /// <summary>ビデオがステップモード中なら1フレーム進める。描画側から毎フレーム呼ぶ。</summary>
        public static void TickVideoStepForward()
        {
            if (!temp_is_video || !temp_video_step_mode) return;
            if (_last_step_frame == Time.frameCount) return;
            _last_step_frame = Time.frameCount;

            if (temp_cached_video_player != null)
                temp_cached_video_player.StepForward();
            else if (Repository.VideoPlayerManager.HasInstance)
                Repository.VideoPlayerManager.Instance.StepForward();
        }

        /// <summary>現在再生中のビデオをステップモードへ移行する。</summary>
        private static void SwitchToStepMode()
        {
            temp_video_step_mode = true;
            if (temp_cached_video_player != null)
                temp_cached_video_player.SwitchToStepMode();
            else if (Repository.VideoPlayerManager.HasInstance)
                Repository.VideoPlayerManager.Instance.SwitchToStepMode();
        }

        /// <summary>現在再生中のビデオを通常再生（PlayMode）へ復帰させる。</summary>
        private static void SwitchToPlayMode()
        {
            temp_video_step_mode = false;
            if (temp_cached_video_player != null)
                temp_cached_video_player.SwitchToPlayMode();
            else if (Repository.VideoPlayerManager.HasInstance)
                Repository.VideoPlayerManager.Instance.SwitchToPlayMode();
        }
        // ----------------------------------------------------------------

        private static float disp_last_update_time = Time.realtimeSinceStartup;

        private static bool is_interrupt_active = false;

        private class AsyncRequest
        {
            public string preset_name;
            public Refs refs;
            public bool is_interrupt_active;
            public bool intr_is_value_fetched;
            public Dictionary<string, float> intr_impact_map;
            public bool steady_is_value_fetched;
            public Dictionary<string, float> steady_impact_map;
            public string last_context_name;
        }

        private static System.Collections.Concurrent.ConcurrentQueue<AsyncRequest> request_queue = new System.Collections.Concurrent.ConcurrentQueue<AsyncRequest>();
        private static AutoResetEvent thread_event = new AutoResetEvent(false);
        private static Thread worker_thread;
        private static volatile string pending_context_result = null;
        private static volatile bool is_calculating = false;
        private static volatile string current_calculating_preset = null;
        private static volatile string repeat_base_context = null;
        // repeat_rules の操作で指定された min_count。ベースコンテキストが変わるまで有効。
        private static int? repeat_override_min_count = null;
        // repeat_rules の操作で指定された max_count。ベースコンテキストが変わるまで有効。
        private static int? repeat_override_max_count = null;
        // repeat_rules の操作で指定された reset_max_count。ベースコンテキストが変わるまで有効。
        private static int? repeat_override_reset_max_count = null;

        private static readonly ThreadLocal<System.Random> rand = new ThreadLocal<System.Random>(() => new System.Random(Guid.NewGuid().GetHashCode()));

        static ConditionDrivenPortrait()
        {
            worker_thread = new Thread(WorkerLoop);
            worker_thread.IsBackground = true;
            worker_thread.Name = "CustomPortraitsAsyncWorker";
            worker_thread.Start();
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                thread_event.WaitOne();
                while (request_queue.TryDequeue(out AsyncRequest req))
                {
                    if (req.preset_name != current_calculating_preset) continue;

                    try
                    {
                        string portrait_context_name = "";
                        bool is_resolved = false;
                        bool no_match = false;
                        List<string> candidate_context_names = new List<string>();
                        bool is_repeat = false;

                        if (req.is_interrupt_active && req.intr_is_value_fetched && req.intr_impact_map != null)
                        {
                            portrait_context_name = ResolveInterruptPortraitContextName(req.refs, req.intr_impact_map, out is_resolved, out no_match, candidate_context_names);
                            if (!is_resolved)
                            {
                                portrait_context_name = "def";
                            }
                            else if (no_match && PortraitCacheEx.Settings.interrupt_fallback_to_steady)
                            {
                                if (req.steady_is_value_fetched && req.steady_impact_map != null)
                                {
                                    candidate_context_names.Clear();
                                    portrait_context_name = ResolveSteadyPortraitContextName(req.refs, req.steady_impact_map, out is_resolved, candidate_context_names);
                                    if (!is_resolved) portrait_context_name = "def";
                                }
                                else
                                {
                                    portrait_context_name = "def";
                                }
                            }
                        }
                        else
                        {
                            if (req.steady_is_value_fetched && req.steady_impact_map != null)
                            {
                                portrait_context_name = ResolveSteadyPortraitContextName(req.refs, req.steady_impact_map, out is_resolved, candidate_context_names);
                                if (!is_resolved) portrait_context_name = "def";
                            }
                            else
                            {
                                portrait_context_name = "def";
                            }
                        }

                        // repeat_rulesの事前評価(主にリピート)
                        if (req.refs.repeat_rules.is_enabled)
                        {
                            if (repeat_base_context != null)
                            {
                                int repeat_index = repeat_count;
                                int loop_result = req.refs.repeat_rules.TryApplyLoopRepeatEvent(
                                    repeat_base_context,
                                    portrait_context_name,
                                    req.last_context_name,
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
                            }
                        }

                        // repeat_rulesの事後評価
                        if (!is_repeat)
                        {
                            if (is_resolved && req.refs.repeat_rules.is_enabled)
                            {
                                // そもそもis_resolvedがtrueじゃないとjsonのコンテキスト名と一致してないので、repeat_rulesの事後評価はしない
                                // defで変えるパターンを今のところ見たことないけど
                                bool is_same_context = (repeat_base_context != null && portrait_context_name == repeat_base_context);
                                if (!is_same_context)
                                {
                                    repeat_count = 0;
                                    repeat_override_min_count = null;
                                    repeat_override_max_count = null;
                                    repeat_override_reset_max_count = null;
                                }

                                int repeat_index = repeat_count;
                                int loop_result = req.refs.repeat_rules.TryResolveVariantContext(
                                    portrait_context_name,
                                    repeat_index,
                                    candidate_context_names,
                                    repeat_base_context,
                                    req.last_context_name,
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
                                    if (!is_same_context)
                                    {
                                        repeat_count = 1;
                                        repeat_base_context = portrait_context_name;
                                    }
                                    else
                                    {
                                        repeat_count++;
                                    }
                                }
                                else
                                {
                                    repeat_count = 1;
                                    repeat_base_context = portrait_context_name;
                                }
                            }

                            pending_context_result = portrait_context_name;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[PortraitsEx] Async Worker Exception: {ex}");
                        pending_context_result = "def";
                    }
                    finally
                    {
                        is_calculating = false;
                    }
                }
            }
        }

        public static void ResetDisplayState()
        {
            while (request_queue.TryDequeue(out _)) { }
            is_calculating = false;
            current_calculating_preset = null;
            pending_context_result = null;

            _last_evaluated_frame = -1;
            _last_evaluated_pawn = null;
            _last_evaluated_filename = null;
            _last_frame_texture = null;

            // temp は Refs 内の tt.txs リストへの参照を持つため Clear() せず空リストへ差し替える（別ポーンのDDS残留防止）
            temp = EmptyTextures;
            temp_index = 0;
            temp_refs_key = "";
            temp_animation_mode = false;
            temp_preset_name = "";
            temp_display_duration = PortraitCacheEx.Settings.display_duration;
            PortraitTimeManager.ResetAllTimers();
            disp_last_update_time = Time.realtimeSinceStartup;
            is_interrupt_active = false;

            // ビデオ状態もリセット
            temp_is_video = false;
            temp_video_loop = true;
            temp_video_step_mode = false;
            temp_cached_video_player = null;

            if (UnityData.IsInMainThread)
            {
                // インスタンスが既に存在する場合のみ Stop()。
                // 未初期化状態で Instance にアクセスするとシーンロード中に new GameObject が走りクラッシュするため。
                Repository.VideoPlayerManager.StopIfActive();
                // プールも止める
                foreach (var r in PortraitCacheEx.Refs.Values)
                {
                    foreach (var cvp in r.cached_videos.Values)
                    {
                        cvp.Stop();
                    }
                }
            }
            else
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    Repository.VideoPlayerManager.StopIfActive();
                    foreach (var r in PortraitCacheEx.Refs.Values)
                    {
                        foreach (var cvp in r.cached_videos.Values)
                        {
                            cvp.Stop();
                        }
                    }
                });
            }
        }

        public static void ResetRepeatState()
        {
            repeat_base_context = null;
            repeat_count = 0;
            repeat_override_min_count = null;
            repeat_override_max_count = null;
            repeat_override_reset_max_count = null;
        }

        /// <summary>
        /// ポーン/プリセット切替時に呼ぶリセット。
        /// </summary>
        public static void ResetAll()
        {
            ResetDisplayState();
            ResetRepeatState();
        }

        /// <summary>
        /// セーブデータのロード開始・メインメニュー退出・ゲーム破棄時に呼び出し、
        /// 旧セーブの Pawn 参照や社交ログキャッシュも含めて完全クリアする。
        /// </summary>
        public static void ResetForGameLifecycle()
        {
            ResetAll();
            PlayLogEntry_Interaction_ctor.Clear();
            PawnPortraitInterruptContext.ClearTracking();
        }

        public static Texture2D GetPortraitTexture(Pawn pawn, string filename, Texture2D def)
        {
            if (Current.ProgramState != ProgramState.Playing || Scribe.mode != LoadSaveMode.Inactive)
            {
                return def;
            }

            // OnGUI は 1 描画フレームに複数回（Layout / Repaint / 入力イベント）呼ばれるため、
            // 同一描画フレーム・同一ポーン・同一ファイル名では最初の 1 回だけ状態評価を行い、
            // 先頭フレーム飛ばしや重複計算を防ぐ。
            int frame_count = Time.frameCount;
            if (_last_evaluated_frame == frame_count &&
                ReferenceEquals(_last_evaluated_pawn, pawn) &&
                _last_evaluated_filename == filename)
            {
                return _last_frame_texture;
            }

            Texture2D evaluated = EvaluatePortraitTextureForFrame(pawn, filename, def);
            _last_evaluated_frame = frame_count;
            _last_evaluated_pawn = pawn;
            _last_evaluated_filename = filename;
            _last_frame_texture = evaluated;
            return evaluated;
        }

        /// <summary>
        /// ポーン選択直後の非同期コンテキスト計算中（まだ初回動画が決まる前の1〜2フレーム）に、
        /// def ではなくプリセット側の fallback_texture を優先して返す。
        /// </summary>
        private static Texture2D ResolveInitialLoadingFallback(Refs refs, Texture2D def)
        {
            if (refs != null && refs.videos.Count > 0)
            {
                if (!string.IsNullOrEmpty(refs.fallback_mood) &&
                    refs.videos.TryGetValue(refs.fallback_mood, out var fbVideo) &&
                    fbVideo?.fallback_texture != null)
                {
                    return fbVideo.fallback_texture;
                }

                foreach (var ve in refs.videos.Values)
                {
                    if (ve?.fallback_texture != null)
                    {
                        return ve.fallback_texture;
                    }
                }
            }
            return def;
        }

        private static Texture2D EvaluatePortraitTextureForFrame(Pawn pawn, string filename, Texture2D def)
        {
            PortraitTimeManager.UpdateFpsMeasurement(temp_is_video, temp_video_step_mode, SwitchToStepMode, SwitchToPlayMode);

            if (temp_is_video)
            {
                bool hasFrame = temp_cached_video_player != null ? temp_cached_video_player.HasFrame : Repository.VideoPlayerManager.Instance.HasFrame;
                bool isVideoEnded = temp_cached_video_player != null ? temp_cached_video_player.is_video_ended : Repository.VideoPlayerManager.Instance.IsVideoEnded;
                long currentFrame = temp_cached_video_player != null ? temp_cached_video_player.CurrentFrame : Repository.VideoPlayerManager.Instance.CurrentFrame;

                if (PortraitTimeManager.CheckVideoStall(temp_is_video, hasFrame, isVideoEnded, currentFrame))
                {
                    SwitchToStepMode();
                }
            }

            if (!string.IsNullOrEmpty(filename) && PortraitCacheEx.IsAvailable)
            {
                string preset_name = Utility.Delimiter(filename, out _);
                if (preset_name == "")
                {
                    return def;
                }

                float frame_interval_seconds = PortraitCacheEx.Settings.portrait_animation.frame_interval_seconds;
                bool portrait_skip_on_lag = PortraitCacheEx.Settings.portrait_animation.portrait_skip_on_lag;
                bool next_portrait = PortraitTimeManager.CheckAnimationInterval(frame_interval_seconds, portrait_skip_on_lag, out int skip_count);
                var mood_refs = PortraitCacheEx.Refs;

                if (mood_refs.TryGetValue(preset_name, out var refs) && !PortraitCacheEx.PresetErrorMap.ContainsKey(preset_name))
                {
                    if (temp_preset_name != preset_name)
                    {
                        bool isHandlingAsync = (is_calculating || pending_context_result != null) && current_calculating_preset == preset_name;
                        if (!isHandlingAsync)
                        {
                            ResetAll();
                        }
                    }

                    string portrait_context_name = "";

                    if (pawn.Dead)
                    {
                        if (refs.fallback_mood_on_death == "") return def;
                        portrait_context_name = refs.fallback_mood_on_death;
                    }
                    else
                    {
                        bool isHandlingAsync = (is_calculating || pending_context_result != null) && current_calculating_preset == preset_name;
                        if (isHandlingAsync)
                        {
                            if (pending_context_result != null)
                            {
                                portrait_context_name = pending_context_result;
                                pending_context_result = null;
                                disp_last_update_time = Time.realtimeSinceStartup;
                            }
                            else
                            {
                                Texture2D fallback = ResolveInitialLoadingFallback(refs, def);
                                return next_portrait ? AdvanceToNextPortrait(fallback, skip_count) : GetCurrentPortrait(fallback);
                            }
                        }
                        else
                        {
                            bool intr_is_value_fetched = false;
                            Dictionary<string, float> intr_impact_map = null;
                            if (refs.interrupt.interrupt_enabled)
                            {
                                intr_impact_map = PawnPortraitInterruptContext.ComposeImpactMap(pawn, refs.interrupt, is_interrupt_active, out intr_is_value_fetched);
                            }

                            if (!is_interrupt_active && intr_is_value_fetched)
                            {
                                is_interrupt_active = true;
                            }
                            else
                            {
                                if (temp_preset_name == preset_name)
                                {
                                    bool should_keep_showing = (Time.realtimeSinceStartup - disp_last_update_time <= temp_display_duration);

                                    if (temp_is_video && !temp_video_loop)
                                    {
                                        bool video_ended = temp_cached_video_player != null
                                            ? temp_cached_video_player.is_video_ended
                                            : Repository.VideoPlayerManager.Instance.IsVideoEnded;
                                        should_keep_showing = !video_ended;
                                    }

                                    if (should_keep_showing)
                                    {
                                        return next_portrait ? AdvanceToNextPortrait(def, skip_count) : GetCurrentPortrait(def);
                                    }
                                }

                                if (is_interrupt_active)
                                {
                                    portrait_context_name = temp_refs_key;
                                    is_interrupt_active = false;
                                    disp_last_update_time = Time.realtimeSinceStartup;
                                }
                            }

                            if (portrait_context_name == "")
                            {
                                bool steady_is_value_fetched = false;
                                Dictionary<string, float> steady_impact_map = null;

                                if ((is_interrupt_active && PortraitCacheEx.Settings.interrupt_fallback_to_steady) || !is_interrupt_active)
                                {
                                    steady_impact_map = PawnPortraitContext.ComposeImpactMap(pawn, out steady_is_value_fetched);
                                }

                                is_calculating = true;
                                current_calculating_preset = preset_name;
                                pending_context_result = null;

                                request_queue.Enqueue(new AsyncRequest
                                {
                                    preset_name = preset_name,
                                    refs = refs,
                                    is_interrupt_active = is_interrupt_active,
                                    intr_is_value_fetched = intr_is_value_fetched,
                                    intr_impact_map = intr_impact_map,
                                    steady_is_value_fetched = steady_is_value_fetched,
                                    steady_impact_map = steady_impact_map,
                                    last_context_name = temp_refs_key
                                });
                                thread_event.Set();

                                Texture2D fallback = ResolveInitialLoadingFallback(refs, def);
                                return next_portrait ? AdvanceToNextPortrait(fallback, skip_count) : GetCurrentPortrait(fallback);
                            }
                        }
                    }
                    return UpdatePortraitByContext(portrait_context_name, next_portrait, skip_count, def, refs, preset_name);
                }
            }

            return def;
        }

        private static string ResolveSteadyPortraitContextName(Refs refs, Dictionary<string, float> impact_map, out bool is_resolved, List<string> candidate_context_names)
        {
            string portrait_context_name = "";
            is_resolved = false;

            HashSet<string> filtered_groups = FilterGroupMatches(refs.g_regex_cache, refs.group_filter, impact_map);
            if (filtered_groups.Count <= 0 && impact_map.Count <= 0)
            {
                return "";
            }

            List<PriorityWeights> matched_weights = ExtractMatchedPriorityWeights(refs.priority_weight_order, refs.priority_weights, filtered_groups);

            for (int i = 0; i < matched_weights.Count; i++)
            {
                candidate_context_names.Add(matched_weights[i].filter_name);
            }

            var rng = rand.Value;
            for (int i = 0; i < matched_weights.Count; i++)
            {
                var pw = matched_weights[i];
                if (rng.Next(0, 100) < pw.weight)
                {
                    portrait_context_name = pw.filter_name;
                    break;
                }
            }

            if (portrait_context_name == "")
            {
                portrait_context_name = refs.fallback_mood == "" ? "def" : refs.fallback_mood;
            }
            is_resolved = true;
            return portrait_context_name;
        }

        private static string ResolveInterruptPortraitContextName(Refs refs, Dictionary<string, float> impact_map, out bool is_resolved, out bool no_match, List<string> candidate_context_names)
        {
            string portrait_context_name = "";
            is_resolved = false;
            no_match = false;

            HashSet<string> filtered_groups = FilterGroupMatches(null, refs.interrupt.group_filter, impact_map);
            if (filtered_groups.Count <= 0 && impact_map.Count <= 0)
            {
                return "";
            }

            List<PriorityWeights> matched_weights = ExtractMatchedPriorityWeights(refs.interrupt.priority_weight_order, refs.interrupt.priority_weights, filtered_groups);

            for (int i = 0; i < matched_weights.Count; i++)
            {
                candidate_context_names.Add(matched_weights[i].filter_name);
            }

            var rng = rand.Value;
            for (int i = 0; i < matched_weights.Count; i++)
            {
                var pw = matched_weights[i];
                if (rng.Next(0, 100) < pw.weight)
                {
                    portrait_context_name = pw.filter_name;
                    break;
                }
            }

            if (portrait_context_name == "")
            {
                no_match = true;
                portrait_context_name = refs.fallback_mood == "" ? "def" : refs.fallback_mood;
            }
            is_resolved = true;
            return portrait_context_name;
        }

        private static Texture2D UpdatePortraitByContext(string portrait_context_name, bool next_portrait, int skip_count, Texture2D def, Refs refs, string preset_name)
        {
            if (portrait_context_name != temp_refs_key)
            {
                ResetDisplayState();

                if (refs.MatchVideoKey(portrait_context_name, out string access_key))
                {
                    return SwitchToVideoAndReturnFrame(refs.videos[access_key], preset_name, access_key, def);
                }

                if (refs.MatchDictKeysByRegex(portrait_context_name, out access_key))
                {
                    if (refs.videos.TryGetValue(access_key, out var ve))
                        return SwitchToVideoAndReturnFrame(ve, preset_name, access_key, def);

                    return CacheTextureIfKeyMatches(def, refs, preset_name, access_key);
                }
                else
                {
                    if (refs.fallback_mood != "")
                    {
                        if (refs.MatchVideoKey(refs.fallback_mood, out access_key))
                        {
                            return SwitchToVideoAndReturnFrame(refs.videos[access_key], preset_name, access_key, def);
                        }
                        else if (refs.MatchDictKeysByRegex(refs.fallback_mood, out access_key))
                        {
                            if (refs.videos.TryGetValue(access_key, out var ve))
                                return SwitchToVideoAndReturnFrame(ve, preset_name, access_key, def);

                            return CacheTextureIfKeyMatches(def, refs, preset_name, access_key);
                        }
                        else
                        {
                            return ImageLoadError(preset_name, def);
                        }
                    }
                    else
                    {
                        return ImageLoadError(preset_name, def);
                    }
                }
            }
            else
            {
                if (temp_is_video)
                {
                    if (!temp_video_loop && (temp_cached_video_player != null ? temp_cached_video_player.is_video_ended : Repository.VideoPlayerManager.Instance.IsVideoEnded))
                    {
                        return SwitchToVideoAndReturnFrame(refs.videos[temp_refs_key], preset_name, temp_refs_key, def);
                    }

                    return null;
                }

                return next_portrait ? AdvanceToNextPortrait(def, skip_count) : GetCurrentPortrait(def);
            }
        }

        private static Texture2D SwitchToVideoAndReturnFrame(Repository.VideoEntry ve, string preset_name, string access_key, Texture2D def)
        {
            string abs_path = PortraitCacheEx.Directory.FullName + "/" + ve.file_path;

            Repository.CachedVideoPlayer cached = null;
            if (PortraitCacheEx.Refs.TryGetValue(preset_name, out var r) && r.cached_videos.TryGetValue(access_key, out var c))
            {
                cached = c;
            }

            if (cached != null)
            {
                if (PortraitTimeManager.LowFpsDetected)
                    cached.PrepareStepMode();
                else
                    cached.Play();

                temp_cached_video_player = cached;
                if (Settings.Instance.debug)
                    Log.Message($"[PortraitsEx] SwitchToVideo (cached) ==> key: {access_key} path: {abs_path} loop: {ve.loop} step_mode: {PortraitTimeManager.LowFpsDetected}");
            }
            else
            {
                if (PortraitTimeManager.LowFpsDetected)
                    Repository.VideoPlayerManager.Instance.SwitchClipStepMode(abs_path, ve.loop, ve.fallback_texture);
                else
                    Repository.VideoPlayerManager.Instance.SwitchClip(abs_path, ve.loop, ve.fallback_texture);

                temp_cached_video_player = null;
                if (Settings.Instance.debug)
                    Log.Message($"[PortraitsEx] SwitchToVideo (singleton) ==> key: {access_key} path: {abs_path} loop: {ve.loop} step_mode: {PortraitTimeManager.LowFpsDetected}");
            }

            temp_is_video = true;
            temp_video_loop = ve.loop;
            temp_video_step_mode = PortraitTimeManager.LowFpsDetected;
            temp_refs_key = access_key;
            temp_preset_name = preset_name;
            temp_display_duration = ve.display_duration;
            disp_last_update_time = Time.realtimeSinceStartup;
            PortraitTimeManager.ResetVideoStallTimer();

            return null;
        }

        private static Texture2D CacheTextureIfKeyMatches(Texture2D def, Refs refs, string preset_name, string access_key)
        {
            var tt = refs.txs[access_key];
            temp = tt.txs;
            temp_animation_mode = tt.IsAnimation;
            temp_index = 0;
            temp_preset_name = preset_name;
            temp_refs_key = access_key;
            temp_display_duration = tt.display_duration;

            if (temp.Count <= 0)
            {
                return ImageLoadError(preset_name, def);
            }

            Texture2D texture = temp[temp_index];
            if (temp_animation_mode) ++temp_index;

            if (texture == null)
            {
                Log.Error("[PortraitsEx] The image was successfully generated, but disappeared at the point when it was registered in the dictionary.");
                return ImageLoadError(preset_name, def);
            }
            return texture;
        }

        private static Texture2D AdvanceToNextPortrait(Texture2D def, int skip_count)
        {
            if (temp_is_video) return null;
            if (temp.Count <= 0) return def;
            if (temp_index < temp.Count)
            {
                Texture2D tx = temp[temp_index];
                if (temp_animation_mode) temp_index += skip_count;
                return tx;
            }
            else
            {
                temp_index = 0;
                Texture2D tx = temp[0];
                if (temp_animation_mode && temp.Count > 1) temp_index = 1;
                return tx;
            }
        }

        private static Texture2D GetCurrentPortrait(Texture2D def)
        {
            if (temp_is_video) return null;
            if (temp.Count <= 0) return def;
            if (temp_animation_mode && temp_index < temp.Count)
            {
                return temp[temp_index];
            }
            return temp[0];
        }

        private static HashSet<string> FilterGroupMatches(
            Dictionary<string, IPatternMatcher> g_regex_cache,
            Dictionary<string, GroupPatternEntry> group_filter,
            Dictionary<string, float> impact_map)
        {
            HashSet<string> filtered_groups = new HashSet<string>();
            foreach (var kvp in group_filter)
            {
                string group_key = kvp.Value.key;
                bool already_added = filtered_groups.Contains(group_key);
                if (already_added && kvp.Value.alias_targets.Count == 0)
                {
                    continue;
                }

                bool is_matched = false;
                if (g_regex_cache != null && g_regex_cache.TryGetValue(kvp.Key, out var matcher))
                {
                    foreach (var impact_map_key in impact_map.Keys)
                    {
                        if (matcher.IsMatch(impact_map_key))
                        {
                            filtered_groups.Add(group_key);
                            is_matched = true;
                            break;
                        }
                    }
                }
                else if (impact_map.ContainsKey(kvp.Key))
                {
                    filtered_groups.Add(group_key);
                    is_matched = true;
                }

                if (is_matched && kvp.Value.alias_targets.Count > 0)
                {
                    for (int i = 0; i < kvp.Value.alias_targets.Count; i++)
                    {
                        filtered_groups.Add(kvp.Value.alias_targets[i]);
                    }
                }
            }

            return filtered_groups;
        }

        private static List<PriorityWeights> ExtractMatchedPriorityWeights(
            List<string> priority_weight_order,
            Dictionary<string, PriorityWeights> priority_weights,
            HashSet<string> matched_groups)
        {
            var matched = new List<PriorityWeights>();
            for (int i = 0; i < priority_weight_order.Count; i++)
            {
                string kp = priority_weight_order[i];
                if (matched_groups.Contains(kp) && priority_weights.TryGetValue(kp, out var vp))
                {
                    matched.Add(vp);
                }
            }
            return matched;
        }

        private static Texture2D ImageLoadError(string preset_name, Texture2D def)
        {
            temp = new List<Texture2D>() { def };
            temp_animation_mode = false;
            temp_index = 0;
            temp_preset_name = preset_name;
            temp_refs_key = "def";
            temp_display_duration = PortraitCacheEx.Settings.display_duration;

            return def;
        }
    }
}
