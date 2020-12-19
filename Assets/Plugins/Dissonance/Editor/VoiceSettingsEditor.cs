#if !NCRUNCH

using Dissonance.Audio.Capture;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceSettings))]
    public class VoiceSettingsEditor : UnityEditor.Editor
    {
        private Texture2D _logo;
        private bool _showAecAdvanced;
        private bool _showAecmAdvanced;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                var settings = (VoiceSettings)target;

                GUILayout.Label(_logo);

                DrawQualitySettings(settings);
                EditorGUILayout.Space();
                DrawPreprocessorSettings(settings);
                EditorGUILayout.Space();
                DrawOtherSettings(settings);

                if (changed.changed)
                    EditorUtility.SetDirty(settings);
            }
        }

        private void DrawOtherSettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.VoiceDuckLevel = EditorGUILayout.Slider("Audio Duck Attenuation", settings.VoiceDuckLevel, 0f, 1f);
                EditorGUILayout.HelpBox("• How much remote voice volume will be reduced when local speech is being transmitted.\n" +
                                        "• A lower value will attenuate more but risks making remote speakers inaudible.", MessageType.Info);
            }
        }

        private void DrawPreprocessorSettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.DenoiseAmount = (NoiseSuppressionLevels)EditorGUILayout.EnumPopup(new GUIContent("Noise Suppression"), settings.DenoiseAmount);
                EditorGUILayout.HelpBox("• A higher value will remove more background noise but risks attenuating speech.\n" +
                                        "• A lower value will remove less noise, but will attenuate speech less.",
                                        MessageType.Info);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.VadSensitivity = (VadSensitivityLevels)EditorGUILayout.EnumPopup(new GUIContent("Voice Detector Sensitivity"), settings.VadSensitivity);
                EditorGUILayout.HelpBox("• A higher value will detect more voice, but may also allow through more non-voice.\n" +
                                        "• A lower value will allow through less non-voice, but may not detect some speech.",
                                        MessageType.Info);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox("Ensure that you have followed the AEC setup instructions before enabling AEC:\n" +
                                        "https://placeholder-software.co.uk/dissonance/docs/Tutorials/Acoustic-Echo-Cancellation", MessageType.Warning);

                settings.AecmRoutingMode = (AecmRoutingMode)EditorGUILayout.EnumPopup(new GUIContent("Mobile Echo Cancellation"), settings.AecmRoutingMode);
                settings.AecSuppressionAmount = (AecSuppressionLevels)EditorGUILayout.EnumPopup(new GUIContent("Desktop Echo Cancellation"), settings.AecSuppressionAmount);
                EditorGUILayout.HelpBox("• A higher value will remove more echo, but risks distorting speech.\n" +
                                        "• A lower value will remove less echo, but will distort speech less.",
                                        MessageType.Info);

                EditorGUI.indentLevel++;
                _showAecAdvanced = EditorGUILayout.Foldout(_showAecAdvanced, new GUIContent("Advanced Desktop Options"), true);
                EditorGUI.indentLevel--;
                if (_showAecAdvanced)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                    {
                        if (Application.isPlaying)
                            EditorGUILayout.HelpBox("AEC advanced configuration cannot be changed at runtime", MessageType.Warning);

                        settings.AecDelayAgnostic = EditorGUILayout.Toggle(new GUIContent("Delay Agnostic Mode"), settings.AecDelayAgnostic);
                        settings.AecExtendedFilter = EditorGUILayout.Toggle(new GUIContent("Extended Filter"), settings.AecExtendedFilter);
                        settings.AecRefinedAdaptiveFilter = EditorGUILayout.Toggle(new GUIContent("Refined Adaptive Filter"), settings.AecRefinedAdaptiveFilter);
                    }
                }

                EditorGUI.indentLevel++;
                _showAecmAdvanced = EditorGUILayout.Foldout(_showAecmAdvanced, new GUIContent("Advanced Mobile Options"), true);
                EditorGUI.indentLevel--;
                if (_showAecmAdvanced)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                    {
                        if (Application.isPlaying)
                            EditorGUILayout.HelpBox("AECM advanced configuration cannot be changed at runtime", MessageType.Warning);

                        settings.AecmComfortNoise = EditorGUILayout.Toggle(new GUIContent("Comfort Noise"), settings.AecmComfortNoise);
                    }
                }
            }
        }

        private void DrawQualitySettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                EditorGUILayout.Space();

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var f = (FrameSize)EditorGUILayout.EnumPopup("Frame Size", settings.FrameSize);
                    if (!Application.isPlaying)
                        settings.FrameSize = f;

                    if (f == FrameSize.Tiny)
                        EditorGUILayout.HelpBox(string.Format("'{0}' frame size is only suitable for LAN usage due to very high bandwidth overhead!", FrameSize.Tiny), MessageType.Warning);

                    EditorGUILayout.HelpBox(
                        "A smaller frame size will send smaller packets of data more frequently, improving latency at the expense of some network and CPU performance.\n\n" +
                        "A larger frame size will send larger packets of data less frequently, gaining some network and CPU performance at the expense of latency.",
                        MessageType.Info
                    );
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var q = (AudioQuality)EditorGUILayout.EnumPopup("Audio Quality", settings.Quality);
                    if (!Application.isPlaying)
                        settings.Quality = q;
                    EditorGUILayout.HelpBox(
                        "A lower quality setting uses less CPU and bandwidth, but sounds worse.\n\n" +
                        "A higher quality setting uses more CPU and bandwidth, but sounds better.",
                        MessageType.Info);
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var fec = EditorGUILayout.Toggle("Forward Error Correction", settings.ForwardErrorCorrection);
                    if (!Application.isPlaying)
                        settings.ForwardErrorCorrection = fec;
                    EditorGUILayout.HelpBox(
                        "When network conditions are bad (high packet loss) use slightly more bandwidth to significantly improve audio quality.",
                        MessageType.Info);
                }

                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Quality settings cannot be changed at runtime",
                        MessageType.Warning);
                }
            }
        }

        #region static helpers
        public static void GoToSettings()
        {
            var settings = LoadVoiceSettings();
            EditorApplication.delayCall += () =>
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = settings;
            };
        }

        private static VoiceSettings LoadVoiceSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VoiceSettings>(VoiceSettings.SettingsFilePath);
            if (asset == null)
            {
                asset = CreateInstance<VoiceSettings>();
                AssetDatabase.CreateAsset(asset, VoiceSettings.SettingsFilePath);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }
        #endregion
    }
}
#endif