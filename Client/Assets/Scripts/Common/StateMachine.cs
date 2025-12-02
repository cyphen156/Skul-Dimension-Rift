using System;
using Assets.Scripts.Interface;

namespace Assets.Scripts.Common
{
    public class StateMachine<TState> : IStateMachine<TState>
        where TState : struct, Enum
    {
        public TState State { get; private set; }

        public StateMachine(TState initialState)
        {
            State = initialState;
        }

        public bool ChangeState(TState next)
        {
            if (State.Equals(next))
            {
                return false;
            }

            State = next;
            return true;
        }

        public bool ForceChageState(TState nextState)
        {
            State = nextState;
            return true;
        }
    }
}
