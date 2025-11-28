using Assets.Scripts.Player;
using Assets.Scripts.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using static State;

/// <summary>
/// 플레이어의 상태와 속성을 관리하는 클래스
/// 플레이어의 체력, 경험치, 레벨 등을 관리
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private Stat playerStat;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private int jumpCount;

    private Dictionary<string, float> clipDurations = new Dictionary<string, float>();

#if UNITY_EDITOR
    [SerializeField] private List<SerializableKeyValuePair> currentStates = new List<SerializableKeyValuePair>();
#endif
    private void Awake()
    {
        animator = GetComponent<Animator>();
        // 플레이어 초기화 로직
        InitializePlayer();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Debugging code can be placed here
        currentStates = Serializer.ToDebugList(stateMachine.GetAllCurrentStates());
    }
#endif

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
        jumpCount = 0;

        clipDurations.Clear();
        Apply();
    }

    private void Apply()
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        AnimationClip[] clips = controller.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clipDurations.ContainsKey(clip.name) == false)
            {
                clipDurations.Add(clip.name, clip.length);
            }
        }
    }
    public ref readonly Stat GetStat()
    {
        return ref playerStat;
    }

    public T GetCurrentState<T>() where T : struct, Enum
    {
        return stateMachine.GetState<T>();
    }

    // use this if animation Param is trigger
    public bool TryChangeState<T>(T state, string paramName = null) where T : struct, Enum
    {
        bool accept = stateMachine.ChangeState(state);

        if (!accept)
        {
            return false;
        }

        return true;
    }

    public bool CanJump()
    {
        return jumpCount < playerStat.maxJumpCount;
    }

    public void ResetJump()
    {
        jumpCount = 0;
    }

    public void UseJump()
    {
        jumpCount++;
        animator.SetTrigger("Jump");

        clipDurations.TryGetValue("Jump", out float duration);
        stateMachine.LockStateMachine<GroundState>(duration);
    }
}
