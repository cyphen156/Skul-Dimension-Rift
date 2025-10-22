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
        public string resolution = "1920x1080";
        public string window = "FullScreen";
        public bool lightingEffect = false;
        public string particlePerformance = "High";
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
        public string language = "ko";
        public bool rukiMode = false;
        public bool showTimer = false;
        public string ShowUIs = "All";
    }
}
