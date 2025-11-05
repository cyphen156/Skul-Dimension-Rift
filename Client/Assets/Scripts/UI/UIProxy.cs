using UnityEngine;
using UnityEngine.InputSystem;

public class UIProxy : InteractiveUIBehaviour
{
    [SerializeField] public InteractiveUIBehaviour bound;

    public void Bind(InteractiveUIBehaviour bindTargetUI)
    {
        bound = bindTargetUI;
    }

    public override void Execute(InputAction.CallbackContext ctx)
    {
        bound.Execute(ctx);
    }
}
