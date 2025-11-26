using Assets.Scripts.Player;
using UnityEngine;

/// <summary>
/// 플레이어의 상태와 속성을 관리하는 클래스
/// 플레이어의 체력, 경험치, 레벨 등을 관리
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private Stat playerStat;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStateMachine stateMachine;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        // 플레이어 초기화 로직
        InitializePlayer();
    }
    private void InitializePlayer()
    {
        playerStat = new Stat
        {
            maxHealth = 100f,
            currentHealth = 100f,
            moveSpeed = 5f,
            jumpPower = 7f,
            attackDamage = 10f,
            attackSpeed = 1f,
            defense = 5f,
            maxJumpCount = 2,
        };

        stateMachine = new PlayerStateMachine();
    }

    public PlayerStateMachine GetStateMachine()
    {
        return stateMachine;
    }

    public Stat GetStat()
    {
        return playerStat;
    }

    public Animator GetAnimator()
    {
        return animator;
    }
}
