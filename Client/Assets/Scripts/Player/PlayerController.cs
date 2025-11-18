using Assets.Scripts.Interface;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 처리하는 클래스
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Vector2 velocity;
    [SerializeField] private IMoveable playerMoter;

    #region Unity Methods
    private void Awake()
    {
        playerMoter = GetComponent<IMoveable>();
    }

    private void Start()
    {
        InputManager.instance.RegisterPlayerInputAction(this);
    }
    private void Update()
    {
        //if (!IsOwner)
        //{
        //    return;
        //}

        playerMoter.Move(velocity);
    }

    #endregion Unity Methods

    #region Input Methods
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            velocity = Vector2.zero;
            return;
        }
        
        if (ctx.performed)
        {
            velocity = ctx.ReadValue<Vector2>();
            return;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnSkill1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnSkill2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnSpirit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnScroll(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }

    public void OnArrowButton(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }
        // TODO
    }
    #endregion Input Methods
}
