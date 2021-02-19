using System.Globalization;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class DissonanceAecFilterInspector
        : IAudioEffectPluginGUI
    {
        private bool _initialized;
        private Texture2D _logo;

        private void Initialize()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");

            _initialized = true;
        }

        public override bool OnGUI([NotNull] IAudioEffectPlugin plugin)
        {
            if (!_initialized)
                Initialize();

            GUILayout.Label(_logo);
            EditorGUILayout.HelpBox("This filter captures data to drive acoustic echo cancellation. All audio which passes through this filter will be played through your " +
                                    "speakers, the filter will watch you microphone for this audio coming back as an echo and remove it", MessageType.Info);

            if (Application.isPlaying)
            {
                var state = WebRtcPreprocessingPipeline.GetAecFilterState();
                switch (state)
                {
                    case WebRtcPreprocessingPipeline.WebRtcPreprocessor.FilterState.FilterNoInstance:
                        EditorGUILayout.HelpBox("AEC filter is running, but it is not associated with a microphone preprocessor - Microphone not running?", MessageType.Info);
                        break;

                    case WebRtcPreprocessingPipeline.WebRtcPreprocessor.FilterState.FilterNoSamplesSubmitted:
                        EditorGUILayout.HelpBox("AEC filter is running, but no samples were submitted in the last frame - Could indicate audio thread starvation", MessageType.Warning);
                        break;

                    case WebRtcPreprocessingPipeline.WebRtcPreprocessor.FilterState.FilterNotRunning:
                        EditorGUILayout.HelpBox("AEC filter is not running - Audio device not initialized?", MessageType.Warning);
                        break;

                    case WebRtcPreprocessingPipeline.WebRtcPreprocessor.FilterState.FilterOk:
                        EditorGUILayout.HelpBox("AEC filter is running.", MessageType.Info);
                        break;

                    default:
                        EditorGUILayout.HelpBox("Unknown Filter State!", MessageType.Error);
                        break;
                }

                // `GetFloatBuffer` (a built in Unity method) causes a null reference exception when called. This bug seems to be limited to Unity 2019.3 on MacOS.
                // See tracking issue: https://github.com/Placeholder-Software/Dissonance/issues/177
#if (UNITY_EDITOR_OSX && UNITY_2019_3)
                EditorGUILayout.HelpBox("Cannot show detailed statistics in Unity 2019.3 due to an editor bug. Please update to Unity 2019.4 or newer!", MessageType.Error);
#else
                float[] data;
                if (plugin.GetFloatBuffer("AecMetrics", out data, 10))
                {
                    EditorGUILayout.LabelField(
                        "Delay Median (samples)",
                        data[0].ToString(CultureInfo.InvariantCulture)
                    );

                    EditorGUILayout.LabelField(
                        "Delay Deviation",
                        data[1].ToString(CultureInfo.InvariantCulture)
                    );

                    EditorGUILayout.LabelField(
                        "Fraction Poor Delays",
                        (data[2] * 100).ToString(CultureInfo.InvariantCulture) + "%"
                    );

                    EditorGUILayout.LabelField(
                        "Echo Return Loss",
                        data[3].ToString(CultureInfo.InvariantCulture)
                    );

                    EditorGUILayout.LabelField(
                        "Echo Return Loss Enhancement",
                        data[6].ToString(CultureInfo.InvariantCulture)
                    );

                    EditorGUILayout.LabelField(
                        "Residual Echo Likelihood",
                        (data[9] * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%"
                    );
                }
#endif
            }

            return false;
        }

        [NotNull] public override string Name
        {
            get { return "Dissonance Echo Cancellation"; }
        }

        [NotNull] public override string Description
        {
            get { return "Captures audio for Dissonance Acoustic Echo Cancellation"; }
        }

        [NotNull] public override string Vendor
        {
            get { return "Placeholder Software"; }
        }
    }
}
