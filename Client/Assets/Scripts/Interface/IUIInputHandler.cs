using UnityEngine.InputSystem;

namespace Assets.Scripts.Interface
{
    internal interface IUIInputHandler
    {
        void Execute(InputAction.CallbackContext ctx);
    }
}
