using System;
using System.Collections.Generic;
using Assets.Scripts.Common;
using Assets.Scripts.Interface;
using static State;

namespace Assets.Scripts.Player
{
    public class PlayerStateMachine : IContainer
    {
        private readonly Dictionary<Type, object> _machines;

        public StateMachine<LifeState> Life
        {
            get
            {
                return (StateMachine<LifeState>)_machines[typeof(LifeState)];
            }
        }

        public StateMachine<MovementState> Movement
        {
            get
            {
                return (StateMachine<MovementState>)_machines[typeof(MovementState)];
            }
        }

        public PlayerStateMachine()
        {
            _machines = new Dictionary<Type, object>();

            _machines[typeof(LifeState)] = new StateMachine<LifeState>(LifeState.Alive);
            _machines[typeof(MovementState)] = new StateMachine<MovementState>(MovementState.Idle);
        }

        public bool ChangeState<TState>(TState next) where TState : struct, Enum
        {
            object machineObject;

            if (_machines.TryGetValue(typeof(TState), out machineObject) == false)
            {
                throw new ArgumentException("Unsupported state type: " + typeof(TState).Name);
            }

            StateMachine<TState> machine = (StateMachine<TState>)machineObject;
            return machine.ChangeState(next);
        }

        // Get the current state of the specified state machine
        public TState GetState<TState>() where TState : struct, Enum
        {
            object machineObject;

            if (_machines.TryGetValue(typeof(TState), out machineObject) == false)
            {
                throw new ArgumentException("Unsupported state type: " + typeof(TState).Name);
            }

            StateMachine<TState> machine = (StateMachine<TState>)machineObject;
            return machine.State;
        }

#if UNITY_EDITOR
        // For debugging: Get all current states
        public Dictionary<string, string> GetAllCurrentStates()
        {
            Dictionary<string, string> states = new Dictionary<string, string>();

            states["LifeState"] = Life.State.ToString();
            states["MovementState"] = Movement.State.ToString();

            return states;
        }
#endif
    }
}
