using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Control : MonoBehaviour, IUIInputHandler   
{
    [SerializeField] private List<Button> buttons;

    private void Awake()
    {
        buttons = ComponentRegistrar.RegisterComponentsInChildren<Button>(transform, includeInactive: true);
    }

    public void Execute(InputAction.CallbackContext ctx)
    {
    }
}
