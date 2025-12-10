using Assets.Scripts.Interface;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static State;

/// <summary>
/// 플레이어의 입력을 처리하는 클래스
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Vector2 velocity;
    [SerializeField] private InteractableDetector detector;
    [SerializeField] private GroundChecker groundChecker;
    [SerializeField] private PlayerManager playerManager;

    [SerializeField] private Vector3 baseScale;
    [SerializeField] private bool isFlipped;


    [SerializeField] private float coyoteTime;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private bool rawGrounded;
    [SerializeField] private bool isGrounded;

    private IMoveable playerMoter;
    private IInteractable currentTarget;

    #region Unity Methods
    private void Awake()
    {
        playerMoter = GetComponent<IMoveable>();
        playerManager = GetComponent<PlayerManager>();
        detector = GetComponentInChildren<InteractableDetector>();
        groundChecker = GetComponentInChildren<GroundChecker>();

        baseScale = transform.localScale;
        isFlipped = false;
        coyoteTime = 0.1f;
        if (CameraManager.instance != null)
        {
            CameraManager.instance.SetPlayerFollow(transform);
        }
    }

    private void OnEnable()
    {
        if (detector != null)
        {
            detector.OnTargetChanged += HandleTargetChanged;
        }
    }

    private void OnDisable()
    {
        if (detector != null)
        {
            detector.OnTargetChanged -= HandleTargetChanged;
        }
    }
    private void Start()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.RegisterPlayerInputAction(this);
        }
    }

    private void FixedUpdate()
    {
        playerMoter.Move(velocity);
        UpdateGroundState();
    }

    #endregion Unity Methods

    #region Input Methods
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            velocity = Vector2.zero;
            playerManager.TryChangeState(MovementState.Idle);
            return;
        }

        Vector2 input = ctx.ReadValue<Vector2>();
        velocity = input * playerManager.GetStat().moveSpeed;

        if (velocity.x != 0)
        {
            bool currentFlip = input.x < 0.0f;

            if (isFlipped != currentFlip)
            {
                isFlipped = currentFlip;
                float sign = isFlipped ? -1.0f : 1.0f;

                transform.localScale = new Vector3
                (
                    baseScale.x * sign,
                    baseScale.y,
                    baseScale.z
                );
            }
        }
        MovementState nextMovement = Mathf.Approximately(velocity.x, 0.0f)
                ? MovementState.Idle
                : MovementState.Move;

        playerManager.TryChangeState(nextMovement);
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        if (velocity.y < 0)
        {
            DownJump();
            return;
        }

        bool canJump = playerManager.CanJump();
        if (canJump && playerManager.TryChangeState(GroundState.Jump, canJump))
        {
            playerMoter.Jump(playerManager.GetStat().jumpPower);
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }
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
    private void UpdateGroundState()
    {
        if (groundChecker == null)
        {
            return;
        }

        rawGrounded = groundChecker.CheckGround();

        if (rawGrounded)
        {
            lastGroundedTime = coyoteTime;
        }
        else
        {
            if (lastGroundedTime > 0f)
            {
                lastGroundedTime -= Time.fixedDeltaTime;
            }
        }

        bool bufferedGround = rawGrounded || (lastGroundedTime > 0.0f);

        if (bufferedGround != isGrounded)
        {
            isGrounded = bufferedGround;

            if (isGrounded)
            {
                playerManager.TryChangeState(GroundState.Ground);
            }
            else
            {
                playerManager.TryChangeState(GroundState.Fall);
            }
        }
    }

    private void DownJump()
    {
        if (groundChecker == null)
        {
            return;
        }
        if(playerManager.TryChangeState(GroundState.Fall))
        {
            groundChecker.IgnoreCollider();
        }
    }

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
