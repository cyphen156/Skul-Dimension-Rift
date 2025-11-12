using Assets.Scripts.Interface;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 처리하는 클래스
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 velocity;

    #region Unity Methods
    private void Awake()
    {
    }

    private void Update()
    {
        Vector3 move = new Vector3(velocity.x, 0) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }

    #endregion Unity Methods

    #region Input Methods
    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            velocity = Vector2.zero;
            return;
        }
        
        if (ctx.performed == true)
        {
            velocity = ctx.ReadValue<Vector2>();
            return;
        }

    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnSkill1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnSkill2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnSpirit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnScroll(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    private void OnArrowButton(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }
    #endregion Input Methods
}
