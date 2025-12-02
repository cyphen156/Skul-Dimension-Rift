using System;

namespace Assets.Scripts.Interface
{
    public interface IStateMachine
    {
    }

    public interface IStateMachine<TState> : IStateMachine where TState : struct, Enum
    {
        TState State
        {
            get;
        }

        bool ChangeState(TState next);
    }
}
