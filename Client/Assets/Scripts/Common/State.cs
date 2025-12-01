using System;

[Serializable]
public class State
{
    [Serializable]
    public enum LifeState
    {
        None,
        Alive,
        Dead,
        Invincible
    }

    [Serializable]
    public enum MovementState
    {
        None,
        Idle,
        Wait,
        Move,
    }

    [Serializable]
    public enum GroundState
    {
        None,
        Ground,
        Jump,
        Fall,
    }

    [Serializable]
    public enum ActionState
    {
        None,
        Idle,
        Movement,   
        Combat, 
    }
}
