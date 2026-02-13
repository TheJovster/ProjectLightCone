using UnityEngine;
using LightCone.Core.Events;
using LightCone.Core.SceneManagement;
using LightCone.Core.Services;
using LightCone.Data.SceneManagement;

namespace LightCone.Core.StateMachine
{
    /// <summary>
    /// Initial state. Runs once on application start.
    /// Validates systems, then transitions to MainMenu.
    /// </summary>
    public sealed class BootState : IGameState
    {
        private readonly GameStateMachine stateMachine;

        public GameStateType StateType => GameStateType.Boot;

        public BootState(GameStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[BootState] Entering. Validating systems...");

            // Future: validate required services, check save data integrity, etc.

            stateMachine.TransitionTo(GameStateType.MainMenu);
        }

        public void Exit() { }
        public void Tick(float deltaTime) { }
    }

    /// <summary>
    /// Main menu state. Loads the menu scene.
    /// Gameplay time is paused. Player can start new game, load, or quit.
    /// </summary>
    public sealed class MainMenuState : IGameState
    {
        private readonly SceneLoader sceneLoader;
        private readonly SceneDefinitionSO sceneConfig;

        public GameStateType StateType => GameStateType.MainMenu;

        public MainMenuState(SceneLoader sceneLoader, SceneDefinitionSO sceneConfig)
        {
            this.sceneLoader = sceneLoader;
            this.sceneConfig = sceneConfig;
        }

        public void Enter()
        {
            Debug.Log("[MainMenuState] Entering.");

            string sceneName = sceneConfig.GetSceneName(GameStateType.MainMenu);

            if (!string.IsNullOrEmpty(sceneName))
            {
                sceneLoader.LoadSceneDirect(sceneName);
            }
        }

        public void Exit()
        {
            Debug.Log("[MainMenuState] Exiting.");
        }

        public void Tick(float deltaTime) { }
    }

    /// <summary>
    /// Transitional loading state. Shown during heavy async loads.
    /// The SceneLoader handles the loading screen automatically,
    /// so this state primarily exists as a logical marker.
    /// </summary>
    public sealed class LoadingState : IGameState
    {
        public GameStateType StateType => GameStateType.Loading;

        public void Enter()
        {
            Debug.Log("[LoadingState] Entering.");
        }

        public void Exit()
        {
            Debug.Log("[LoadingState] Exiting.");
        }

        public void Tick(float deltaTime) { }
    }

    /// <summary>
    /// Core gameplay state. The dungeon is active, time ticks, systems run.
    /// Loads the gameplay scene through the loading screen.
    /// </summary>
    public sealed class GameplayState : IGameState
    {
        private readonly SceneLoader sceneLoader;
        private readonly SceneDefinitionSO sceneConfig;

        public GameStateType StateType => GameStateType.Gameplay;

        public GameplayState(SceneLoader sceneLoader, SceneDefinitionSO sceneConfig)
        {
            this.sceneLoader = sceneLoader;
            this.sceneConfig = sceneConfig;
        }

        public void Enter()
        {
            Debug.Log("[GameplayState] Entering.");

            string sceneName = sceneConfig.GetSceneName(GameStateType.Gameplay);
            string loadingScene = sceneConfig.LoadingSceneName;

            if (!string.IsNullOrEmpty(sceneName))
            {
                sceneLoader.LoadScene(sceneName, loadingScene, OnSceneLoaded);
            }
        }

        public void Exit()
        {
            Debug.Log("[GameplayState] Exiting.");
        }

        public void Tick(float deltaTime)
        {
            // Gameplay-specific per-frame logic that doesn't belong to a system.
            // Most logic lives in systems (TimeSystem, ResourceController, etc.)
            // which tick themselves. This is for orchestration if needed.
        }

        private void OnSceneLoaded()
        {
            Debug.Log("[GameplayState] Gameplay scene loaded and ready.");
        }
    }

    /// <summary>
    /// Paused state. Overlays on top of gameplay — does NOT unload the dungeon scene.
    /// Time stops. Player can save, adjust settings, or resume.
    /// </summary>
    public sealed class PausedState : IGameState
    {
        private readonly GameStateMachine stateMachine;

        public GameStateType StateType => GameStateType.Paused;

        public PausedState(GameStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[PausedState] Entering.");
            UnityEngine.Time.timeScale = 0f;
        }

        public void Exit()
        {
            Debug.Log("[PausedState] Exiting.");
            UnityEngine.Time.timeScale = 1f;
        }

        public void Tick(float deltaTime)
        {
            // Paused state still ticks (with deltaTime = 0 due to timeScale).
            // Use unscaledDeltaTime for pause menu animations if needed.
        }
    }

    /// <summary>
    /// Game over state. Player died or extracted.
    /// Shows results, then transitions back to MainMenu.
    /// </summary>
    public sealed class GameOverState : IGameState
    {
        private readonly SceneLoader sceneLoader;
        private readonly SceneDefinitionSO sceneConfig;

        public GameStateType StateType => GameStateType.GameOver;

        public GameOverState(SceneLoader sceneLoader, SceneDefinitionSO sceneConfig)
        {
            this.sceneLoader = sceneLoader;
            this.sceneConfig = sceneConfig;
        }

        public void Enter()
        {
            Debug.Log("[GameOverState] Entering.");

            string sceneName = sceneConfig.GetSceneName(GameStateType.GameOver);

            if (!string.IsNullOrEmpty(sceneName))
            {
                sceneLoader.LoadSceneDirect(sceneName);
            }
        }

        public void Exit()
        {
            Debug.Log("[GameOverState] Exiting.");
        }

        public void Tick(float deltaTime) { }
    }
}