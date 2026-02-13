using UnityEngine.InputSystem;

namespace Assets.Scripts.Interface
{
    public interface IInteractive
    {
        void Execute(InputAction.CallbackContext ctx);
    }
}
