using Assets.Scripts.Player;
using UnityEngine;

/// <summary>
/// 플레이어의 상태와 속성을 관리하는 클래스
/// 플레이어의 체력, 경험치, 레벨 등을 관리
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private Stat playerStat;
    private void Awake()
    {
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
            attackDamage = 10f,
            attackSpeed = 1f,
            defense = 5f
        };
    }
    public void TakeDamage(float damage)
    {
        playerStat.currentHealth -= damage - playerStat.defense;
        if (playerStat.currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        // 플레이어 사망 처리 로직
        Debug.Log("Player has died.");
    }
}
