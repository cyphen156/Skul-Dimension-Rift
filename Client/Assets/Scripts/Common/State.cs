using UnityEngine;

public class State
{
    public enum PlayerState
    {
        None = 0,

        Idle,
        Move,

        Jump,
        Fall,
        JumpAttack,

        Attack,
        Dash,

        Skill1,
        Skill2,

        Dead,
    }
}
