using System;
using System.Collections.Generic;
using System.Numerics;
using T = Types;

namespace Assets.Scripts.Data
{
    [Serializable]
    public class UserData
    {
        public int version = 1;
        public string createdAt;
        public string lastModified;

        public ControlData control = new ControlData();
        public OptionsData options = new OptionsData();
    }

    [Serializable]
    public class ControlData
    {
        public string bindings = "[]";
    }

    [Serializable]
    public class OptionsData
    {
        public Graphic graphic = new Graphic();
        public Data data = new Data();
        public Audio audio = new Audio();
        public GamePlay gameplay = new GamePlay();
    }

    [Serializable]
    public class Graphic
    {
        public T.Resolution resolution = T.Resolution.FHD;
        public T.Window window = T.Window.FullScreen;
        public T.LightingEffect lightingEffect = T.LightingEffect.Off;
        public T.ParticlePerformance particlePerformance = T.ParticlePerformance.High;
        public float windowEarthQuakeEffect = 0.5f;
        public float shakingEffect = 0.5f;
    }

    [Serializable]
    public class Data
    {
        public string currentSceneName = "Title";
        public string currentStage = "";
        public int maxScore = 0;
    }

    [Serializable]
    public class Audio
    {
        public float masterVolume = 0.5f;
        public float BGMVolume = 0.5f;
        public float SFXVolume = 0.5f;
    }

    [Serializable]
    public class GamePlay
    {
        public T.Languages language = T.Languages.Korean;
        public T.RukiMode rukiMode = T.RukiMode.Off;
        public T.ShowTimer showTimer = T.ShowTimer.Off;
        public T.ShowUIs showUIs = T.ShowUIs.All;
    }
}
