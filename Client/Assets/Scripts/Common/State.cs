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
        Waiting,
        Moving,
    }

    [Serializable]
    public enum ActionState
    {
        Idle,       // No action
        Movement,   
        Combat, 
    }
}
