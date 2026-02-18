using UnityEngine;
using LightCone.Core.Events;
using LightCone.Core.Save;
using LightCone.Core.SceneManagement;
using LightCone.Core.Services;
using LightCone.Core.StateMachine;
using LightCone.Data.SceneManagement;
using LightCone.Systems.Audio;
using LightCone.Data.Audio;

namespace LightCone.Core
{
    /// <summary>
    /// Application entry point. Lives on a persistent GameObject in the Boot scene.
    /// Initializes core systems, registers services, and starts the state machine.
    /// This is the ONE justified singleton in the project.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SceneDefinitionSO sceneConfig;
        [SerializeField] private AudioConfigSO audioConfig;

        private static GameBootstrapper instance;
        private GameStateMachine stateMachine;
        private SaveManager saveManager;
        private SceneLoader sceneLoader;
            

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        private void Update()
        {
            stateMachine?.Tick(UnityEngine.Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            ShutdownSystems();
        }

        private void InitializeSystems()
        {
            // Scene loader — lives on this persistent GameObject
            sceneLoader = gameObject.AddComponent<SceneLoader>();
            ServiceLocator.Register<SceneLoader>(sceneLoader);

            // State machine
            stateMachine = new GameStateMachine();
            ServiceLocator.Register<GameStateMachine>(stateMachine);

            // Save system
            var serializer = new JsonSaveSerializer();
            saveManager = new SaveManager(serializer);
            ServiceLocator.Register<SaveManager>(saveManager);

            //audio system
            var audioGo = new GameObject("AudioService");
            audioGo.transform.SetParent(transform);
            var audioService = audioGo.AddComponent<AudioService>();
            audioService.Initialize(audioConfig);
            ServiceLocator.Register<IAudioService>(audioService);

            // Register game states
            RegisterStates();

            // Start the machine
            stateMachine.TransitionTo(GameStateType.Boot);

            Debug.Log("[GameBootstrapper] Core systems initialized.");
        }

        private void RegisterStates()
        {
            stateMachine.RegisterState(new BootState(stateMachine));
            stateMachine.RegisterState(new MainMenuState(sceneLoader, sceneConfig));
            stateMachine.RegisterState(new LoadingState());
            stateMachine.RegisterState(new GameplayState(sceneLoader, sceneConfig));
            stateMachine.RegisterState(new PausedState(stateMachine));
            stateMachine.RegisterState(new GameOverState(sceneLoader, sceneConfig));
        }

        private void ShutdownSystems()
        {
            EventBus.Clear();
            ServiceLocator.Clear();
            Debug.Log("[GameBootstrapper] Core systems shut down.");
        }
    }
}