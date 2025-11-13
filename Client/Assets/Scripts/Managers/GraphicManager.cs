using Assets.Scripts.Common;
using Assets.Scripts.Data;
using UnityEngine;
using static Types;

public class GraphicManager : MonoBehaviour
{
    public static GraphicManager instance;
    [SerializeField] private Graphic userGraphicData;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        Initialize();
    }

    private void Initialize()
    {
        userGraphicData = ResourceManager.instance.GetUserOptionsData().graphic;

        mainCamera = Camera.main;
        ApplyResolutionSetting();
    }

    public void ApplyOption(UIWidgetContainer widget)
    {
        if (widget == null)
        {
            return;
        }

        GraphicType type;
        Types.graphicType.TryGetValue(widget.parentName, out type);

        float value = widget.GetValue();
        int delta = ((int)value == 0) ? -1 : +1;

        string data = string.Empty;

        switch (type)
        {
            case GraphicType.Resolution:
                userGraphicData.resolution = EnumUtility.ShiftWrap(userGraphicData.resolution, delta);
                Vector2 source = Types.resolutionMap[userGraphicData.resolution];
                data = Formatter.Vec2Resolution(source);
                break;
            case GraphicType.Window:
                userGraphicData.window = EnumUtility.ShiftWrap(userGraphicData.window, delta);
                data = EnumUtility.ToString(userGraphicData.window);
                break;
            case GraphicType.LightingEffect:
                userGraphicData.lightingEffect = EnumUtility.ShiftWrap(userGraphicData.lightingEffect, delta);
                data = EnumUtility.ToString(userGraphicData.lightingEffect);
                break;
            case GraphicType.ParticlePerformance:
                userGraphicData.particlePerformance = EnumUtility.ShiftWrap(userGraphicData.particlePerformance, delta);
                data = EnumUtility.ToString(userGraphicData.particlePerformance);
                break;
            case GraphicType.windowEarthQuakeEffect:
                userGraphicData.windowEarthQuakeEffect = value;
                break;
            case GraphicType.shakingEffect:
                userGraphicData.shakingEffect = value;
                break;
            default:
                break;
        }
        GameManager.instance.ReplyOptionResult("", data);
    }

    private FullScreenMode GetFullScreenMode(Window window)
    {
        switch (window)
        {
            case Window.FullScreen:
                return FullScreenMode.FullScreenWindow;
            case Window.BorderlessFullScreen:
                return FullScreenMode.MaximizedWindow;
            case Window.Window:
                return FullScreenMode.Windowed;
            default:
                return FullScreenMode.FullScreenWindow;
        }
    }

    public void ApplyResolutionSetting()
    {
        Vector2 size;

        if (!Types.resolutionMap.TryGetValue(userGraphicData.resolution, out size))
        {
            return;
        }

        int width = (int)size.x;
        int height = (int)size.y;

        // Custom 혹은 잘못된 값이면 현재 기기 해상도를 가져와서 적용함
        // 모바일, 모니터 해상도 필요
        if (width <= 0 || height <= 0)
        {
            width = Display.main.systemWidth;
            height = Display.main.systemHeight;
        }

        FullScreenMode mode = GetFullScreenMode(userGraphicData.window);

        // 동일 상태면 스킵
        if (Screen.width == width && Screen.height == height && Screen.fullScreenMode == mode)
        {
            return;
        }

        Screen.SetResolution(width, height, mode);

#if UNITY_EDITOR
        Debug.Log($"[Resolution] {Screen.width} x {Screen.height} / Mode: {Screen.fullScreenMode}");
#endif
    }
}