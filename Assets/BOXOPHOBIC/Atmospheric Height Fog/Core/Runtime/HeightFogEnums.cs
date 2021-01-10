// Cristian Pop - https://boxophobic.com/

namespace AtmosphericHeightFog
{
    public enum FogMode
    {
        UseScriptSettings = 10,
        UsePresetSettings = 15,
        UseTimeOfDay = 20,
    }

    public enum FogAxisMode
    {
        XAxis = 0,
        YAxis = 1,
        ZAxis = 2,
    }

    public enum FogLayersMode
    {
        MultiplyDistanceAndHeight = 10,
        AdditiveDistanceAndHeight = 20,
    }

    public enum FogDirectionalMode
    {
        Off = 0,
        On = 1
    }

    public enum FogNoiseMode
    {
        Off = 0,
        Procedural3D = 3
    }
}

