using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace UnityEngine.Video
{
    public static class VideoPlayerExtensions
    {
        private static readonly bool _fastStepSupported;
        private static bool _loggedInitStatus;
        private static int _debugStepCounter;
        private static int _debugEmptyCounter;
        private static readonly Func<VideoPlayer, object> _getCorePlayer;
        private static readonly Func<object, object> _getPlaybackIfPrepared;
        private static readonly Func<object, object> _takeFirstFrame;
        private static readonly Action<object, object> _releaseFrame;
        private static readonly Action<object, object> _presentAndSyncPlayhead;
        private static readonly Func<object, object, bool> _isNonLoopingEnded;
        private static readonly Action<object> _markEndedAndNotify;

        static VideoPlayerExtensions()
        {
            try
            {
                FieldInfo coreField = typeof(VideoPlayer).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
                if (coreField == null)
                    return;

                Type corePlayerType = coreField.FieldType; // Waylith.VideoDecode.VideoPlayer
                Assembly decodeAsm = corePlayerType.Assembly;
                Type playbackType = decodeAsm.GetType("Waylith.VideoDecode.VideoPlayback");
                Type frameType = decodeAsm.GetType("Waylith.VideoDecode.VideoFrame");
                if (playbackType == null || frameType == null)
                    return;

                PropertyInfo isPreparedProp = corePlayerType.GetProperty("IsPrepared", BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo loopingProp = corePlayerType.GetProperty("Looping", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo playbackField = corePlayerType.GetField("playback", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo playheadField = corePlayerType.GetField("playhead", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo playingField = corePlayerType.GetField("playing", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo reachedEndField = corePlayerType.GetField("reachedEnd", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo loopPointField = corePlayerType.GetField("LoopPointReached", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo presentMethod = corePlayerType.GetMethod("Present", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { frameType }, null);

                MethodInfo takeFirstMethod = playbackType.GetMethod("TakeFirst", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo releaseMethod = playbackType.GetMethod("Release", BindingFlags.Instance | BindingFlags.Public, null, new[] { frameType }, null);
                PropertyInfo hasQueuedProp = playbackType.GetProperty("HasQueuedFrames", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo endedField = playbackType.GetField("Ended", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo streamTimeField = frameType.GetField("StreamTime", BindingFlags.Instance | BindingFlags.Public);

                if (isPreparedProp == null || loopingProp == null || playbackField == null ||
                    playheadField == null || playingField == null || reachedEndField == null ||
                    loopPointField == null || presentMethod == null || takeFirstMethod == null ||
                    releaseMethod == null || hasQueuedProp == null || endedField == null || streamTimeField == null)
                {
                    return;
                }

                Module mod = typeof(VideoPlayerExtensions).Module;

                // 1) (VideoPlayer vp) => (object)vp._player
                {
                    var dm = new DynamicMethod("CPEx_GetCorePlayer", typeof(object), new[] { typeof(VideoPlayer) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, coreField);
                    il.Emit(OpCodes.Ret);
                    _getCorePlayer = (Func<VideoPlayer, object>)dm.CreateDelegate(typeof(Func<VideoPlayer, object>));
                }

                // 2) (object core) => ((CorePlayer)core).IsPrepared ? (object)((CorePlayer)core).playback : null
                {
                    var dm = new DynamicMethod("CPEx_GetPlaybackIfPrepared", typeof(object), new[] { typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    Label notPrepared = il.DefineLabel();
                    LocalBuilder coreLocal = il.DeclareLocal(corePlayerType);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, corePlayerType);
                    il.Emit(OpCodes.Stloc, coreLocal);
                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Callvirt, isPreparedProp.GetGetMethod());
                    il.Emit(OpCodes.Brfalse_S, notPrepared);
                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldfld, playbackField);
                    il.Emit(OpCodes.Ret);
                    il.MarkLabel(notPrepared);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    _getPlaybackIfPrepared = (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
                }

                // 3) (object playback) => (object)((VideoPlayback)playback).TakeFirst()
                {
                    var dm = new DynamicMethod("CPEx_TakeFirstFrame", typeof(object), new[] { typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, playbackType);
                    il.Emit(OpCodes.Callvirt, takeFirstMethod);
                    il.Emit(OpCodes.Ret);
                    _takeFirstFrame = (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
                }

                // 4) (object playback, object frame) => ((VideoPlayback)playback).Release((VideoFrame)frame)
                {
                    var dm = new DynamicMethod("CPEx_ReleaseFrame", typeof(void), new[] { typeof(object), typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, playbackType);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, frameType);
                    il.Emit(OpCodes.Callvirt, releaseMethod);
                    il.Emit(OpCodes.Ret);
                    _releaseFrame = (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
                }

                // 5) (object core, object frame) => { core.Present(frame); core.playhead = frame.StreamTime; }
                {
                    var dm = new DynamicMethod("CPEx_PresentAndSyncPlayhead", typeof(void), new[] { typeof(object), typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    LocalBuilder coreLocal = il.DeclareLocal(corePlayerType);
                    LocalBuilder frameLocal = il.DeclareLocal(frameType);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, corePlayerType);
                    il.Emit(OpCodes.Stloc, coreLocal);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, frameType);
                    il.Emit(OpCodes.Stloc, frameLocal);

                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldloc, frameLocal);
                    il.Emit(OpCodes.Callvirt, presentMethod);

                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldloc, frameLocal);
                    il.Emit(OpCodes.Ldfld, streamTimeField);
                    il.Emit(OpCodes.Stfld, playheadField);
                    il.Emit(OpCodes.Ret);
                    _presentAndSyncPlayhead = (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
                }

                // 6) (object core, object playback) => !core.Looping && playback.Ended && !playback.HasQueuedFrames
                {
                    var dm = new DynamicMethod("CPEx_IsNonLoopingEnded", typeof(bool), new[] { typeof(object), typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    Label retFalse = il.DefineLabel();
                    LocalBuilder pbLocal = il.DeclareLocal(playbackType);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, corePlayerType);
                    il.Emit(OpCodes.Callvirt, loopingProp.GetGetMethod());
                    il.Emit(OpCodes.Brtrue_S, retFalse);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, playbackType);
                    il.Emit(OpCodes.Stloc, pbLocal);

                    il.Emit(OpCodes.Ldloc, pbLocal);
                    il.Emit(OpCodes.Volatile);
                    il.Emit(OpCodes.Ldfld, endedField);
                    il.Emit(OpCodes.Brfalse_S, retFalse);

                    il.Emit(OpCodes.Ldloc, pbLocal);
                    il.Emit(OpCodes.Callvirt, hasQueuedProp.GetGetMethod());
                    il.Emit(OpCodes.Brtrue_S, retFalse);

                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(retFalse);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ret);
                    _isNonLoopingEnded = (Func<object, object, bool>)dm.CreateDelegate(typeof(Func<object, object, bool>));
                }

                // 7) (object core) => { core.playing = false; core.reachedEnd = true; core.LoopPointReached?.Invoke(); }
                {
                    var dm = new DynamicMethod("CPEx_MarkEndedAndNotify", typeof(void), new[] { typeof(object) }, mod, true);
                    ILGenerator il = dm.GetILGenerator();
                    Label done = il.DefineLabel();
                    LocalBuilder coreLocal = il.DeclareLocal(corePlayerType);
                    LocalBuilder actionLocal = il.DeclareLocal(typeof(Action));

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, corePlayerType);
                    il.Emit(OpCodes.Stloc, coreLocal);

                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stfld, playingField);

                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stfld, reachedEndField);

                    il.Emit(OpCodes.Ldloc, coreLocal);
                    il.Emit(OpCodes.Ldfld, loopPointField);
                    il.Emit(OpCodes.Stloc, actionLocal);

                    il.Emit(OpCodes.Ldloc, actionLocal);
                    il.Emit(OpCodes.Brfalse_S, done);
                    il.Emit(OpCodes.Ldloc, actionLocal);
                    il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod("Invoke"));

                    il.MarkLabel(done);
                    il.Emit(OpCodes.Ret);
                    _markEndedAndNotify = (Action<object>)dm.CreateDelegate(typeof(Action<object>));
                }

                _fastStepSupported = true;
            }
            catch (Exception ex)
            {
                _fastStepSupported = false;
                Log.Warning($"[PortraitsEx] Fast Waylith StepForward initialization failed, falling back to SeekFrame: {ex.Message}");
            }
        }

        public static void StepForward(this VideoPlayer player)
        {
            StepFrames(player, 1);
        }

        /// <summary>
        /// RimWorld 1.7 (Waylith.VideoDecode) のデコード済み ready キューから
        /// Seek 無しで指定フレーム数だけ順次取り出して描画する。
        /// steps=2 (double_step_forward) 時は1枚目の GPU Blit も省略して均等2コマ送りを実現する。
        /// </summary>
        public static void StepFrames(this VideoPlayer player, int steps)
        {
            if (player == null)
                return;

            if (steps < 1)
                steps = 1;

            bool debug = CustomPortraits.Settings.Instance != null && CustomPortraits.Settings.Instance.debug;
            if (debug && !_loggedInitStatus)
            {
                _loggedInitStatus = true;
                Log.Message($"[PortraitsEx] [Diag] VideoPlayerExtensions initialized: build=1.7-ZeroSeekStep, fastStepSupported={_fastStepSupported}");
            }

            if (_fastStepSupported)
            {
                object core = _getCorePlayer(player);
                if (core == null)
                    return;

                object playback = _getPlaybackIfPrepared(core);
                if (playback == null)
                {
                    if (debug)
                    {
                        _debugEmptyCounter++;
                        if (_debugEmptyCounter == 1 || _debugEmptyCounter % 15 == 0)
                        {
                            Log.Message($"[PortraitsEx] [Diag] StepFrames waiting for Prepare (count={_debugEmptyCounter}, url={player.url})");
                        }
                    }
                    return;
                }

                long beforeFrame = debug ? player.frame : -1;
                object frame = _takeFirstFrame(playback);
                if (frame == null)
                {
                    if (_isNonLoopingEnded(core, playback))
                    {
                        if (debug)
                        {
                            Log.Message($"[PortraitsEx] [Diag] StepFrames reached end: lastFrame={beforeFrame}/{player.frameCount}, time={player.time:F2}s");
                        }
                        _markEndedAndNotify(core);
                    }
                    else if (debug)
                    {
                        _debugEmptyCounter++;
                        if (_debugEmptyCounter == 1 || _debugEmptyCounter % 15 == 0)
                        {
                            Log.Message($"[PortraitsEx] [Diag] StepFrames ready-queue empty (decode catching up): count={_debugEmptyCounter}, currentFrame={beforeFrame}/{player.frameCount}");
                        }
                    }
                    return;
                }

                _debugEmptyCounter = 0;
                int actualPopped = 1;
                for (int i = 1; i < steps; i++)
                {
                    object next = _takeFirstFrame(playback);
                    if (next == null)
                        break;

                    _releaseFrame(playback, frame);
                    frame = next;
                    actualPopped++;
                }

                _presentAndSyncPlayhead(core, frame);

                if (debug)
                {
                    _debugStepCounter++;
                    if (_debugStepCounter == 1 || _debugStepCounter % 15 == 0)
                    {
                        Log.Message($"[PortraitsEx] [Diag] StepFrames #{_debugStepCounter}: reqSteps={steps}, popped={actualPopped}, frame={beforeFrame}->{player.frame}/{player.frameCount}, time={player.time:F2}s");
                    }
                }
                return;
            }

            // フォールバック
            if (player.frameCount > 0 && player.frame < (long)player.frameCount - 1)
            {
                player.frame += steps;
            }
        }
    }
}
