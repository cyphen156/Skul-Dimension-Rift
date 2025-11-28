using System;

namespace Assets.Scripts.Player
{
    [Serializable]
    public struct Stat
    {
        public float maxHealth;
        public float currentHealth;
        public float moveSpeed;
        public float jumpPower;
        public float attackDamage;
        public float attackSpeed;
        public float defense;
        public int maxJumpCount;
        public int currentJumpCount;
    }
}
