using System;

[Serializable]
public class State
{
    [Serializable]
    public enum LifeState
    {
        Alive,
        Dead,
        Invincible
    }

    [Serializable]
    public enum MovementState
    {
        Idle,
        Wait,
        Move,
    }

    [Serializable]
    public enum GroundState
    {
        Ground,
        Jump,
        Fall,
    }

    [Serializable]
    public enum ActionState
    {
        Idle,       // No action
        Movement,   
        Combat, 
    }
}
