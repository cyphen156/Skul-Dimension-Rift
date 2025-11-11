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
    }

    public void ApplyOption(Widget widget)
    {
        if (widget == null)
        {
            return;
        }

        GraphicType type;
        Types.graphicType.TryGetValue(widget.parentName, out type);

        float value = widget.GetValue();

        switch (type)
        {
            case GraphicType.Resolution:
                {
                    int currentResolution = userGraphicData.resolution;
                    int delta = (int)value;
                    int nextIndex = currentIndex + delta;

                }
                break;
            case GraphicType.Window:
                
                break;
            case GraphicType.LightingEffect:
                break;
            case GraphicType.ParticlePerformance:
                break;
            case GraphicType.windowEarthQuakeEffect:
                break;
            case GraphicType.shakingEffect:
                break;
            default:
                break;
        }
    }

}