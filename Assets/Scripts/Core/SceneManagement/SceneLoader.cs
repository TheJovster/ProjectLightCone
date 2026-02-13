using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightCone.Core.Events;

namespace LightCone.Core.SceneManagement
{
    /// <summary>
    /// Handles async scene loading and unloading.
    /// Publishes progress events for loading screen UI.
    /// Must be attached to a persistent GameObject (the bootstrapper).
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private bool isLoading;
        private string currentLoadedScene;

        public bool IsLoading => isLoading;
        public string CurrentLoadedScene => currentLoadedScene;

        /// <summary>
        /// Load a scene additively with a loading screen in between.
        /// Unloads the previous scene, shows loading scene, loads target, unloads loading scene.
        /// </summary>
        public void LoadScene(string targetScene, string loadingScene, Action onComplete = null)
        {
            if (isLoading)
            {
                Debug.LogWarning("[SceneLoader] Already loading a scene. Ignoring request.");
                return;
            }

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError("[SceneLoader] Target scene name is null or empty.");
                return;
            }

            StartCoroutine(LoadSceneSequence(targetScene, loadingScene, onComplete));
        }

        /// <summary>
        /// Load a scene additively without a loading screen.
        /// Use for lightweight scenes (menus, overlays).
        /// </summary>
        public void LoadSceneDirect(string targetScene, Action onComplete = null)
        {
            if (isLoading)
            {
                Debug.LogWarning("[SceneLoader] Already loading a scene. Ignoring request.");
                return;
            }

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError("[SceneLoader] Target scene name is null or empty.");
                return;
            }

            StartCoroutine(LoadSceneDirectSequence(targetScene, onComplete));
        }

        /// <summary>
        /// Unload the current loaded scene. Does not load anything in its place.
        /// </summary>
        public void UnloadCurrentScene(Action onComplete = null)
        {
            if (string.IsNullOrEmpty(currentLoadedScene))
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(UnloadSceneSequence(currentLoadedScene, onComplete));
        }

        private IEnumerator LoadSceneSequence(string targetScene, string loadingScene, Action onComplete)
        {
            isLoading = true;

            EventBus.Publish(new SceneLoadStartedEvent
            {
                TargetScene = targetScene
            });

            // Unload current scene if one is loaded
            if (!string.IsNullOrEmpty(currentLoadedScene))
            {
                yield return UnloadSceneAsync(currentLoadedScene);
                currentLoadedScene = null;
            }

            // Load the loading screen if specified
            bool hasLoadingScreen = !string.IsNullOrEmpty(loadingScene);

            if (hasLoadingScreen)
            {
                yield return LoadSceneAsync(loadingScene);
            }

            // Load the target scene with progress reporting
            var loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

            if (loadOp == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene: {targetScene}");
                isLoading = false;
                yield break;
            }

            loadOp.allowSceneActivation = false;

            // Report progress (Unity goes to 0.9 then waits for allowSceneActivation)
            while (loadOp.progress < 0.9f)
            {
                EventBus.Publish(new SceneLoadProgressEvent
                {
                    TargetScene = targetScene,
                    Progress = loadOp.progress / 0.9f
                });

                yield return null;
            }

            // Allow activation
            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
            {
                yield return null;
            }

            currentLoadedScene = targetScene;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

            // Unload the loading screen
            if (hasLoadingScreen)
            {
                yield return UnloadSceneAsync(loadingScene);
            }

            isLoading = false;

            EventBus.Publish(new SceneLoadCompletedEvent
            {
                LoadedScene = targetScene
            });

            onComplete?.Invoke();
        }

        private IEnumerator LoadSceneDirectSequence(string targetScene, Action onComplete)
        {
            isLoading = true;

            EventBus.Publish(new SceneLoadStartedEvent
            {
                TargetScene = targetScene
            });

            // Unload current scene if one is loaded
            if (!string.IsNullOrEmpty(currentLoadedScene))
            {
                yield return UnloadSceneAsync(currentLoadedScene);
                currentLoadedScene = null;
            }

            // Load target directly
            var loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

            if (loadOp == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene: {targetScene}");
                isLoading = false;
                yield break;
            }

            while (!loadOp.isDone)
            {
                yield return null;
            }

            currentLoadedScene = targetScene;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

            isLoading = false;

            EventBus.Publish(new SceneLoadCompletedEvent
            {
                LoadedScene = targetScene
            });

            onComplete?.Invoke();
        }

        private IEnumerator UnloadSceneSequence(string sceneName, Action onComplete)
        {
            yield return UnloadSceneAsync(sceneName);

            if (currentLoadedScene == sceneName)
            {
                currentLoadedScene = null;
            }

            onComplete?.Invoke();
        }

        private AsyncOperation LoadSceneAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private AsyncOperation UnloadSceneAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.isLoaded)
            {
                return null;
            }

            return SceneManager.UnloadSceneAsync(scene);
        }
    }
}