using Assets.Scripts.Data;
using System;
using UnityEngine;

public class GraphicManager : MonoBehaviour
{
    public static GraphicManager instance;
    [SerializeField] private Graphic userGraphicData;

    internal void ApplyOption(Widget widget)
    {
        throw new NotImplementedException();
    }

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
    }
}