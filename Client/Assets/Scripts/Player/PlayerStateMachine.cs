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
        private Locker<IStateMachine> locker = new Locker<IStateMachine>();

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

        public StateMachine<GroundState> Ground
        {
            get
            {
                return (StateMachine<GroundState>)_machines[typeof(GroundState)];
            }
        }

        public PlayerStateMachine()
        {
            _machines = new Dictionary<Type, object>();

            _machines[typeof(LifeState)] = new StateMachine<LifeState>(LifeState.None);
            _machines[typeof(MovementState)] = new StateMachine<MovementState>(MovementState.None);
            _machines[typeof(GroundState)] = new StateMachine<GroundState>(GroundState.None);
        }

        public bool ChangeState<TState>(TState next) where TState : struct, Enum
        {
            StateMachine<TState> machine = GetMachine<TState>();

            // 먼저 락커에 있는지 확인
            if (locker.IsLocked(machine))
            {
                return false;
            }

            return machine.ChangeState(next);
        }

        public bool ForceChangeState<TState>(TState next) where TState : struct, Enum
        {
            StateMachine<TState> machine = GetMachine<TState>();

            // 상태가 잠겨있다면 해제
            if (locker.IsLocked(machine))
            {
                UnLockState<TState>();
            }

            return machine.ForceChageState(next);
        }

        // Get the current state of the specified state machine
        public TState GetState<TState>() where TState : struct, Enum
        {
            StateMachine<TState> machine = GetMachine<TState>();
            return machine.State;
        }

        private StateMachine<TState> GetMachine<TState>() where TState : struct, Enum
        {
            object machineObject;

            if (_machines.TryGetValue(typeof(TState), out machineObject) == false)
            {
                throw new ArgumentException("Unsupported state type: " + typeof(TState).Name);
            }

            return (StateMachine<TState>)machineObject;
        }

        public void LockStateMachine<TState>(float duration = 0f) where TState : struct, Enum
        {
            StateMachine<TState> machine = GetMachine<TState>();
            locker.Lock(machine, duration);
        }

        public void UnLockState<TState>() where TState : struct, Enum
        {
            StateMachine<TState> machine = GetMachine<TState>();
            locker.ForceUnlock(machine);
        }

#if UNITY_EDITOR
        // For debugging: Get all current states
        public Dictionary<string, string> GetAllCurrentStates()
        {
            Dictionary<string, string> states = new Dictionary<string, string>();

            states["LifeState"] = Life.State.ToString();
            states["MovementState"] = Movement.State.ToString();
            states["GroundState"] = Ground.State.ToString();
            return states;
        }
#endif
    }
}
