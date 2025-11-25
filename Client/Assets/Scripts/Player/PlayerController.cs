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
    [SerializeField] private GroundDetector groundDetector;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Animator animator;
    [SerializeField] private bool isFlipped;

    // 스테이트 머신으로 넘길 수 있는 영역
    [SerializeField] private int jumpCount;

    private IMoveable playerMoter;
    private IInteractable currentTarget;

    #region Unity Methods
    private void Awake()
    {
        playerMoter = GetComponent<IMoveable>();
        playerManager = GetComponent<PlayerManager>();
        animator = GetComponent<Animator>();
        detector = GetComponentInChildren<InteractableDetector>();

        isFlipped = false;

        if (detector != null)
        {
            detector.OnTargetChanged += HandleTargetChanged;
        }
    }

    private void OnEnable()
    {
        if (groundDetector != null)
        {
            groundDetector.isGroundedChanged += (isGrounded) =>
            {
                if (isGrounded)
                {
                    jumpCount = 0;
                }
            };
        }
}
    private void Start()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.RegisterPlayerInputAction(this);
        }
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
            animator.SetBool("IsWalking", false);
            return;
        }

        Vector2 input = ctx.ReadValue<Vector2>();
        velocity = input * playerManager.GetStat().moveSpeed;

        if (velocity.x != 0)
        {
            bool currentFlip = input.x < 0;
            if (isFlipped != currentFlip)
            {
                isFlipped = currentFlip;
                transform.localScale = new Vector3(isFlipped ? -1 : 1, 1, 1);
            }   
        }

        animator.SetBool("IsWalking", velocity.x != 0);
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        if (velocity.y < 0)
        {
            // TODO : fall animation
            return;
        }

        if (jumpCount < playerManager.GetStat().maxJumpCount)
        {
            playerMoter.Jump(velocity, playerManager.GetStat().jumpPower);
            animator.SetTrigger("Jump");
            jumpCount++;
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        animator.SetTrigger("Dash");
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        animator.SetTrigger("Attack");
    }

    public void OnSkill1(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

    }

    public void OnSkill2(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

    }

    public void OnSpirit(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

    }

    public void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

    }

    public void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

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
