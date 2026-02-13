using System;
using System.Collections.Generic;
using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Core.StateMachine
{
    /// <summary>
    /// Interface for a game state. Implement for each top-level state.
    /// </summary>
    public interface IGameState
    {
        GameStateType StateType { get; }
        void Enter();
        void Exit();
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Manages top-level game state transitions (Boot, MainMenu, Loading, Gameplay, Paused, GameOver).
    /// Publishes GameStateChangedEvent on transition.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly Dictionary<GameStateType, IGameState> states = new();
        private IGameState currentState;

        public GameStateType CurrentStateType => currentState?.StateType ?? GameStateType.Boot;

        /// <summary>
        /// Register a state implementation. Call during initialization.
        /// </summary>
        public void RegisterState(IGameState state)
        {
            if (state == null)
            {
                Debug.LogError("[GameStateMachine] Cannot register null state.");
                return;
            }

            if (states.ContainsKey(state.StateType))
            {
                Debug.LogWarning($"[GameStateMachine] Overwriting state: {state.StateType}");
            }

            states[state.StateType] = state;
        }

        /// <summary>
        /// Transition to a new state. Calls Exit on current, Enter on new.
        /// </summary>
        public void TransitionTo(GameStateType newStateType)
        {
            if (!states.TryGetValue(newStateType, out var newState))
            {
                Debug.LogError($"[GameStateMachine] State not registered: {newStateType}");
                return;
            }

            var previousType = CurrentStateType;

            currentState?.Exit();
            currentState = newState;
            currentState.Enter();

            EventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = previousType,
                NewState = newStateType
            });
        }

        /// <summary>
        /// Call from a MonoBehaviour's Update. Ticks the current state.
        /// </summary>
        public void Tick(float deltaTime)
        {
            currentState?.Tick(deltaTime);
        }
    }
}