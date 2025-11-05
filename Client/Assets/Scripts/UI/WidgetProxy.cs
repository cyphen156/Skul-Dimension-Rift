using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WidgetProxy : InteractiveUIBehaviour
{
    [SerializeField] public InteractiveUIBehaviour bindUI;
    [SerializeField] private List<Button> cachedButtons = new List<Button>();

    public void Bind(InteractiveUIBehaviour bind)
    {
    }

    public new void Execute(InputAction.CallbackContext ctx)
    {
        bindUI.Execute(ctx);
    }
}
