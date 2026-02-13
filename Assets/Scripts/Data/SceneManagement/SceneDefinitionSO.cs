using System;
using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Data.SceneManagement
{
    /// <summary>
    /// ScriptableObject mapping game states to scene names.
    /// Single source of truth for which scene belongs to which state.
    /// Change scene assignments in the inspector, never in code.
    /// </summary>
    [CreateAssetMenu(fileName = "Scene Config", menuName = "LightCone/Data/Scene Config")]
    public sealed class SceneDefinitionSO : ScriptableObject
    {
        [Header("Scene Assignments")]
        [SerializeField] private SceneEntry[] sceneEntries;

        [Header("Defaults")]
        [Tooltip("Scene shown during async loading transitions.")]
        [SerializeField] private string loadingSceneName = "Loading";

        public string LoadingSceneName => loadingSceneName;

        /// <summary>
        /// Get the scene name associated with a game state.
        /// Returns null if no scene is mapped.
        /// </summary>
        public string GetSceneName(GameStateType stateType)
        {
            if (sceneEntries == null)
            {
                return null;
            }

            for (int i = 0; i < sceneEntries.Length; i++)
            {
                if (sceneEntries[i].stateType == stateType)
                {
                    return sceneEntries[i].sceneName;
                }
            }

            return null;
        }

        /// <summary>
        /// Links a game state to a scene name.
        /// </summary>
        [Serializable]
        public struct SceneEntry
        {
            public GameStateType stateType;
            public string sceneName;
        }
    }
}