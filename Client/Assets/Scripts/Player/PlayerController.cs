using Assets.Scripts.Common;
using Assets.Scripts.Interface;
using Assets.Scripts.Player;
using Assets.Scripts.Utility;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private Animator animator;

    [SerializeField] private Vector3 baseScale;
    [SerializeField] private bool isFlipped;


    [SerializeField] private int jumpCount;
    [SerializeField] private float waitingTime;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool rawGrounded;

    private Coroutine waitingCoroutine;

    private IMoveable playerMoter;
    private IInteractable currentTarget;

#if UNITY_EDITOR
    [SerializeField] private List<SerializableKeyValuePair> currentStates = new List<SerializableKeyValuePair>();
#endif
    #region Unity Methods
    private void Awake()
    {
        playerMoter = GetComponent<IMoveable>();
        playerManager = GetComponent<PlayerManager>();
        animator = GetComponent<Animator>();
        detector = GetComponentInChildren<InteractableDetector>();
        groundChecker = GetComponentInChildren<GroundChecker>();

        if (playerManager != null)
        {
            stateMachine = playerManager.GetStateMachine();
        }

        baseScale = transform.localScale;
        isFlipped = false;
        waitingTime = 5f;
        jumpCount = 0;
        coyoteTime = 0.1f;
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
    private void Update()
    {
        //if (!IsOwner)
        //{
        //    return;
        //}
        playerMoter.Move(velocity);
#if UNITY_EDITOR
        // Debugging code can be placed here
        currentStates = Serializer.ToDebugList(stateMachine.GetAllCurrentStates());
#endif
    }

    private void FixedUpdate()
    {
        UpdateGroundState();
    }

    #endregion Unity Methods

    #region Input Methods
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            velocity = Vector2.zero;
            stateMachine.ChangeState(MovementState.Idle);
            animator.SetBool("IsWalking", false);
            if (waitingCoroutine == null)
            {
                waitingCoroutine = StartCoroutine(C_WaitingCounter());
            }
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

        stateMachine.ChangeState(MovementState.Moving);

        animator.SetBool("IsWalking", velocity.x != 0.0f);
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }


        if (CanJump())
        {
            if (velocity.y < 0)
            {
                DownJump();
                return;
            }
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

            if (!isGrounded)
            {
                jumpCount = 0;
            }
        }

        else
        {
            if (lastGroundedTime > 0f)
            {
                lastGroundedTime -= Time.fixedDeltaTime;
            }
        }

        bool bufferedGround = rawGrounded || (lastGroundedTime > 0f);

        if (bufferedGround != isGrounded)
        {
            isGrounded = bufferedGround;
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private bool CanJump()
    {
        return isGrounded && jumpCount < playerManager.GetStat().maxJumpCount;
    }

    private void DownJump()
    {
        if (groundChecker == null)
        {
            return;
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

    private IEnumerator C_WaitingCounter()
    {
        float duration = waitingTime;

        while (duration > 0.0f)
        {
            if (stateMachine.GetState<MovementState>() != MovementState.Idle)
            {
                yield break;
            }

            duration -= Time.deltaTime;
            yield return null;
        }

        stateMachine.ChangeState(MovementState.Waiting);
        animator.SetTrigger("Wait");
    }
}
