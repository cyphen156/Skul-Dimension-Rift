using Assets.Scripts.Interface;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 처리하는 클래스
/// </summary>
public class PlayerController : NetworkBehaviour, IInteractive
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 velocity;

    #region Unity Methods

    private void Update()
    {
        Vector3 move = new Vector3(velocity.x, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
    #endregion Unity Methods

    #region Input Methods
    public void Execute(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        string actionName = ctx.action.name;
        switch (actionName)
        {
            case "Move":
                {
                    OnMove(ctx);
                }
                break;

            default:
                break;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        velocity = ctx.ReadValue<Vector2>();
        Debug.Log("Called");
    }
    #endregion Input Methods
}
