using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dissonance.Config;
using NAudio.Wave;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Audio.Capture
{
    internal class WebRtcPreprocessingPipeline
        : BasePreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(WebRtcPreprocessingPipeline).Name);

        private bool _isVadDetectingSpeech;
        protected override bool VadIsSpeechDetected
        {
            get { return _isVadDetectingSpeech; }
        }

        private readonly bool _isMobilePlatform;

        private WebRtcPreprocessor _preprocessor;

        private bool _isOutputMuted;
        public override bool IsOutputMuted
        {
            set
            {
                _isOutputMuted = value;
            }
        }
        #endregion

        #region construction
        public WebRtcPreprocessingPipeline([NotNull] WaveFormat inputFormat, bool mobilePlatform)
            : base(inputFormat, 480, 48000, 480, 48000)
        {
            _isMobilePlatform = mobilePlatform;
        }
        #endregion

        protected override void ThreadStart()
        {
            _preprocessor = new WebRtcPreprocessor(_isMobilePlatform);

            base.ThreadStart();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_preprocessor != null)
                _preprocessor.Dispose();
        }

        protected override void ApplyReset()
        {
            if (_preprocessor != null)
                _preprocessor.Reset();

            base.ApplyReset();
        }

        protected override void PreprocessAudioFrame(float[] frame)
        {
            var config = AudioSettingsWatcher.Instance.Configuration;

            var captureLatencyMs = PreprocessorLatencyMs;
            var playbackLatencyMs = (int)(1000 * ((float)config.dspBufferSize / config.sampleRate));
            var latencyMs = captureLatencyMs + playbackLatencyMs;

            _isVadDetectingSpeech = _preprocessor.Process(WebRtcPreprocessor.SampleRates.SampleRate48KHz, frame, frame, latencyMs, _isOutputMuted);

            SendSamplesToSubscribers(frame);
        }

        internal static WebRtcPreprocessor.FilterState GetAecFilterState()
        {
            return (WebRtcPreprocessor.FilterState)WebRtcPreprocessor.Dissonance_GetFilterState();
        }

        internal sealed class WebRtcPreprocessor
            : IDisposable
        {
            #region native methods

#if UNITY_IOS && !UNITY_EDITOR
            private const string ImportString = "__Internal";
            private const CallingConvention Convention = default(CallingConvention);
#else
            private const string ImportString = "AudioPluginDissonance";
            private const CallingConvention Convention = CallingConvention.Cdecl;
#endif

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern IntPtr Dissonance_CreatePreprocessor(
                NoiseSuppressionLevels nsLevel,
                AecSuppressionLevels aecLevel, bool aecDelayAgnostic, bool aecExtended, bool aecRefined,
                AecmRoutingMode aecmRoutingMode, bool aecmComfortNoise
            );

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_DestroyPreprocessor(IntPtr handle);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_ConfigureNoiseSuppression(IntPtr handle, NoiseSuppressionLevels nsLevel);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_ConfigureVadSensitivity(IntPtr handle, VadSensitivityLevels nsLevel);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_ConfigureAecSuppression(IntPtr handle, AecSuppressionLevels aecLevel, AecmRoutingMode aecmRouting);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern bool Dissonance_GetVadSpeechState(IntPtr handle);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern ProcessorErrors Dissonance_PreprocessCaptureFrame(IntPtr handle, int sampleRate, float[] input, float[] output, int streamDelay);

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern bool Dissonance_PreprocessorExchangeInstance(IntPtr previous, IntPtr replacement);

            [DllImport(ImportString, CallingConvention = Convention)]
            internal static extern int Dissonance_GetFilterState();

            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_GetAecMetrics(IntPtr floatBuffer, int bufferLength);

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_WSA)
            [DllImport(ImportString, CallingConvention = Convention)]
            private static extern void Dissonance_SetAgcIsOutputMutedState(IntPtr handle, bool isMuted);
#else
            private static bool _setAgcMutedStatePlatformWarningSent;
            private static void Dissonance_SetAgcIsOutputMutedState(IntPtr handle, bool isMuted)
            {
                if (!_setAgcMutedStatePlatformWarningSent)
                    Log.Debug("`Dissonance_SetAgcIsOutputMutedState` is not available on this platform");
                _setAgcMutedStatePlatformWarningSent = true;
            }
#endif

            public enum SampleRates
            {
                // ReSharper disable UnusedMember.Local
                SampleRate8KHz = 8000,
                SampleRate16KHz = 16000,
                SampleRate32KHz = 32000,
                SampleRate48KHz = 48000,
                // ReSharper restore UnusedMember.Local
            }

            private enum ProcessorErrors
            {
                // ReSharper disable UnusedMember.Local
                Ok,

                Unspecified = -1,
                CreationFailed = -2,
                UnsupportedComponent = -3,
                UnsupportedFunction = -4,
                NullPointer = -5,
                BadParameter = -6,
                BadSampleRate = -7,
                BadDataLength = -8,
                BadNumberChannels = -9,
                FileError = -10,
                StreamParameterNotSet = -11,
                NotEnabled = -12,
                // ReSharper restore UnusedMember.Local
            }

            internal enum FilterState
            {
                // ReSharper disable UnusedMember.Local
                FilterNotRunning,
                FilterNoInstance,
                FilterNoSamplesSubmitted,
                FilterOk
                // ReSharper restore UnusedMember.Local
            }
            #endregion

            #region properties and fields
            private readonly LockedValue<IntPtr> _handle;

            private readonly List<PropertyChangedEventHandler> _subscribed = new List<PropertyChangedEventHandler>();

            private readonly bool _useMobileAec;

            private NoiseSuppressionLevels _nsLevel;
            private NoiseSuppressionLevels NoiseSuppressionLevel
            {
                get { return _nsLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        //Lumin (magic leap) has built in noise suppression applied to the mic signal before we even get it. This disables the Dissonance Noise suppressor.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                        _nsLevel = NoiseSuppressionLevels.Disabled;
                        Log.Debug("`NoiseSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #endif

                        _nsLevel = value;
                        if (handle.Value != IntPtr.Zero)
                            Dissonance_ConfigureNoiseSuppression(handle.Value, _nsLevel);
                    }
                }
            }

            private VadSensitivityLevels _vadlevel;
            private VadSensitivityLevels VadSensitivityLevel
            {
                get { return _vadlevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        _vadlevel = value;
                        if (handle.Value != IntPtr.Zero)
                            Dissonance_ConfigureVadSensitivity(handle.Value, _vadlevel);
                    }
                }
            }

            private AecSuppressionLevels _aecLevel;
            private AecSuppressionLevels AecSuppressionLevel
            {
                get { return _aecLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        //Lumin (magic leap) has built in AEC applied to the mic signal before we even get it. This disables the Dissonance AEC. Technically this is the desktop AEC so it shouldn't even be running
                        //on the magic leap anyway.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                        value = AecSuppressionLevels.Disabled;
                        Log.Debug("`AecSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #endif

                        _aecLevel = value;
                        if (!_useMobileAec)
                        {
                            if (handle.Value != IntPtr.Zero)
                                Dissonance_ConfigureAecSuppression(handle.Value, _aecLevel, AecmRoutingMode.Disabled);
                        }
                    }
                }
            }

            private AecmRoutingMode _aecmLevel;
            private AecmRoutingMode AecmSuppressionLevel
            {
                get { return _aecmLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        //Lumin (magic leap) has built in AEC applied to the mic signal before we even get it. This disables the Dissonance AECM.
                        #if PLATFORM_LUMIN && !UNITY_EDITOR
                        value = AecmRoutingMode.Disabled;
                        Log.Debug("`AecmSuppressionLevel` was set to `{0}` but PLATFORM_LUMIN is defined, overriding to `Disabled`");
                        #endif

                        _aecmLevel = value;
                        if (_useMobileAec)
                        {
                            if (handle.Value != IntPtr.Zero)
                                Dissonance_ConfigureAecSuppression(handle.Value, AecSuppressionLevels.Disabled, _aecmLevel);
                        }
                    }
                }
            }
            #endregion

            public WebRtcPreprocessor(bool useMobileAec)
            {
                _useMobileAec = useMobileAec;
                _handle = new LockedValue<IntPtr>(IntPtr.Zero);

                NoiseSuppressionLevel = VoiceSettings.Instance.DenoiseAmount;
                AecSuppressionLevel = VoiceSettings.Instance.AecSuppressionAmount;
                AecmSuppressionLevel = VoiceSettings.Instance.AecmRoutingMode;
            }

            public bool Process(SampleRates inputSampleRate, float[] input, float[] output, int estimatedStreamDelay, bool isOutputMuted)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "5C97EF6A-353B-4B96-871F-1073746B5708");

                    Dissonance_SetAgcIsOutputMutedState(handle.Value, isOutputMuted);
                    Log.Trace("Set IsOutputMuted to `{0}`", isOutputMuted);

                    var result = Dissonance_PreprocessCaptureFrame(handle.Value, (int)inputSampleRate, input, output, estimatedStreamDelay);
                    if (result != ProcessorErrors.Ok)
                        throw Log.CreatePossibleBugException(string.Format("Preprocessor error: '{0}'", result), "0A89A5E7-F527-4856-BA01-5A19578C6D88");

                    return Dissonance_GetVadSpeechState(handle.Value);
                }
            }

            public void Reset()
            {
                using (var handle = _handle.Lock())
                {
                    Log.Debug("Resetting WebRtcPreprocessor");

                    if (handle.Value != IntPtr.Zero)
                    {
                        //Clear from playback filter. This internally acquires a lock and will not complete until it is safe to (i.e. no one else is using the preprocessor concurrently).
                        ClearFilterPreprocessor();

                        //Destroy it
                        Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }

                    //Create a new one
                    handle.Value = CreatePreprocessor();

                    //Associate with playback filter
                    SetFilterPreprocessor(handle.Value);
                }
            }

            private IntPtr CreatePreprocessor()
            {
                var instance = VoiceSettings.Instance;

                //Disable one of the echo cancellers, depending upon platform
                var pcLevel = AecSuppressionLevel;
                var mobLevel = AecmSuppressionLevel;
                if (_useMobileAec)
                    pcLevel = AecSuppressionLevels.Disabled;
                else
                    mobLevel = AecmRoutingMode.Disabled;

                Log.Debug("Creating new preprocessor instance - Mob:{0} NS:{1} AEC:{2} DelayAg:{3} Ext:{4}, Refined:{5} Aecm:{6}, Comfort:{7}",
                    _useMobileAec,
                    NoiseSuppressionLevel,
                    pcLevel,
                    instance.AecDelayAgnostic,
                    instance.AecExtendedFilter,
                    instance.AecRefinedAdaptiveFilter,
                    mobLevel,
                    instance.AecmComfortNoise
                );

                return Dissonance_CreatePreprocessor(
                    NoiseSuppressionLevel,
                    pcLevel,
                    instance.AecDelayAgnostic,
                    instance.AecExtendedFilter,
                    instance.AecRefinedAdaptiveFilter,
                    mobLevel,
                    instance.AecmComfortNoise
                );
            }

            private void SetFilterPreprocessor(IntPtr preprocessor)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "3BA66D46-A7A6-41E8-BE38-52AFE5212ACD");

                    Log.Debug("Exchanging preprocessor instance in playback filter...");

                    if (!Dissonance_PreprocessorExchangeInstance(IntPtr.Zero, handle.Value))
                        throw Log.CreatePossibleBugException("Cannot associate preprocessor with Playback filter - one already exists", "D5862DD2-B44E-4605-8D1C-29DD2C72A70C");

                    Log.Debug("...Exchanged preprocessor instance in playback filter");

                    var state = (FilterState)Dissonance_GetFilterState();
                    if (state == FilterState.FilterNotRunning)
                        Log.Debug("Associated preprocessor with playback filter - but filter is not running");

                    Bind(s => s.DenoiseAmount, "DenoiseAmount", v => NoiseSuppressionLevel = (NoiseSuppressionLevels)v);
                    Bind(s => s.AecSuppressionAmount, "AecSuppressionAmount", v => AecSuppressionLevel = (AecSuppressionLevels)v);
                    Bind(s => s.AecmRoutingMode, "AecmRoutingMode", v => AecmSuppressionLevel = (AecmRoutingMode)v);
                    Bind(s => s.VadSensitivity, "VadSensitivity", v => VadSensitivityLevel = v);
                }
            }

            private void Bind<T>(Func<VoiceSettings, T> getValue, string propertyName, Action<T> setValue)
            {
                var settings = VoiceSettings.Instance;

                //Bind for value changes in the future
                PropertyChangedEventHandler subbed;
                settings.PropertyChanged += subbed = (sender, args) => {
                    if (args.PropertyName == propertyName)
                        setValue(getValue(settings));
                };

                //Save this subscription so we can *unsub* later
                _subscribed.Add(subbed);

                //Invoke immediately to pull the current value
                subbed.Invoke(settings, new PropertyChangedEventArgs(propertyName));
            }

            private bool ClearFilterPreprocessor(bool throwOnError = true)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted to access a null WebRtc Preprocessor encoder", "2DBC7779-F1B9-45F2-9372-3268FD8D7EBA");

                    Log.Debug("Clearing preprocessor instance in playback filter...");

                    //Clear binding in native code
                    if (!Dissonance_PreprocessorExchangeInstance(handle.Value, IntPtr.Zero))
                    {
                        if (throwOnError)
                            throw Log.CreatePossibleBugException("Cannot clear preprocessor from Playback filter. Editor restart required!", "6323106A-04BD-4217-9ECA-6FD49BF04FF0");
                        else
                            Log.Error("Failed to clear preprocessor from playback filter. Editor restart required!", "CBC6D727-BE07-4073-AA5A-F750A0CC023D");

                        return false;
                    }

                    //Clear event handlers from voice settings
                    var settings = VoiceSettings.Instance;
                    for (var i = 0; i < _subscribed.Count; i++)
                        settings.PropertyChanged -= _subscribed[i];
                    _subscribed.Clear();

                    Log.Debug("...Cleared preprocessor instance in playback filter");
                    return true;
                }
            }

            #region dispose
            private void ReleaseUnmanagedResources()
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value != IntPtr.Zero)
                    {
                        ClearFilterPreprocessor(throwOnError: false);

                        Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }
                }
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~WebRtcPreprocessor()
            {
                ReleaseUnmanagedResources();
            }
            #endregion
        }
    }
}
