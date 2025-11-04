using System;

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
        public Resolution resolution = new Resolution();
        public Window window = Window.FullScreen;
        public LightingEffect lightingEffect = LightingEffect.Off;
        public ParticlePerformance particlePerformance = ParticlePerformance.High;
        public float windowEarthQuaqingEffect = 0.5f;
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
        public Languages language = Languages.Korean;
        public RukiMode rukiMode = RukiMode.Off;
        public ShowTimer showTimer = ShowTimer.Off;
        public ShowUIs showUIs = ShowUIs.All;
    }

    [Serializable]
    public struct Resolution
    {
        public int width;
        public int height;

        public Resolution(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return width + " x " + height;
        }
    }

    [Serializable]
    public enum Window
    {
        FullScreen,
        Window,
        BorderlessFullScreen,
    }

    [Serializable]
    public enum LightingEffect
    {
        On,
        Off,
    }

    [Serializable]
    public enum ParticlePerformance
    {
        Low,
        Middle,
        High,
    }

    [Serializable]
    public enum Languages
    {
        Korean,
        English,
        Japanese,
    }

    [Serializable]
    public enum RukiMode
    {
        On,
        Off,
    }
   
    [Serializable]
    public enum ShowTimer
    {
        On,
        Off,
    }

    [Serializable]
    public enum ShowUIs
    {
        All,
        None,
        InGame,
    }
}
