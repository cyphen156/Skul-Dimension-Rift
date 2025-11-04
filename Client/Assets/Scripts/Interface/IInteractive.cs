using UnityEngine.InputSystem;

namespace Assets.Scripts.Interface
{
    internal interface IInteractive
    {
        void Execute(InputAction.CallbackContext ctx);
    }
}
