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
    [SerializeField] private InteractableDetector detector;
    [SerializeField] private PlayerManager playerManager;

    private IMoveable playerMoter;
    private IInteractable currentTarget;

    #region Unity Methods
    private void Awake()
    {
        playerMoter = GetComponent<IMoveable>();
        playerManager = GetComponent<PlayerManager>();
        detector = GetComponentInChildren<InteractableDetector>();

        if (detector != null)
        {
            detector.OnTargetChanged += HandleTargetChanged;
        }
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

        velocity = ctx.ReadValue<Vector2>() * playerManager.GetStat().moveSpeed;
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
        if (detector == null)
        {
            return;
        }

        if (ctx.started)
        {
            if (currentTarget != null)
            {
                SetPromptInteracting(true);
                currentTarget.Interact();
            }
        }

        if (ctx.canceled)
        {
            SetPromptInteracting(false);
        }
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

    private void HandleTargetChanged(IInteractable target)
    {
        currentTarget = target;

        if (target == null)
        {
            HidePrompt();
        }
        else
        {
            ShowPrompt(target);
        }
    }

    private void ShowPrompt(IInteractable target)
    {
        UIManager.instance.Show("Prompt");

        GameObject obj = UIManager.instance.TryGetUI("Prompt");
        if (obj == null)
        {
            return;
        }

        PromptWidget widget = obj.GetComponent<PromptWidget>();

        if (widget != null)
        {
            widget.SetPrompt(Assets.Scripts.Data.PromptType.Interact);
            widget.SetInteracting(false);
        }

        MonoBehaviour mb = target as MonoBehaviour;
        if (mb != null)
        {
            Transform anchor = mb.transform.Find("PromptAnchor");
            if (anchor != null)
            {
                UIManager.instance.SetPromptTarget(anchor);
            }
        }
    }

    private void HidePrompt()
    {
        UIManager.instance.Hide("Prompt");
        UIManager.instance.SetPromptTarget(null);
    }

    private void SetPromptInteracting(bool flag)
    {
        GameObject obj = UIManager.instance.TryGetUI("Prompt");

        if (obj == null)
        {
            return;
        }

        PromptWidget widget = obj.GetComponent<PromptWidget>();

        if (widget != null)
        {
            widget.SetInteracting(flag);
        }
    }
}
