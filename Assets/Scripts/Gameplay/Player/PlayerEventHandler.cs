using UnityEngine;
using UnityEngine.InputSystem;
using LightCone.Core.Events;
using LightCone.Systems.Stats;
using LightCone.Data.Player;
using LightCone.Core.Services;
using LightCone.Systems.Resources;

namespace LightCone.Gameplay.Player
{
    /// <summary>
    /// EventHandler instance for the player.
    /// Lives on the Player GameObject. Manages the player's event stack.
    /// All player gameplay events (movement, menus, interaction) run through this.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AttributeSet))]
    public sealed class PlayerEventHandler : EventHandler
    {
        [Header("Configuration")]
        [SerializeField] private PlayerDefinitionSO playerDefinition;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private ResourceController resourceController;

        [Header("References")]
        [SerializeField] private Transform cameraHolder;

        private void Start()
        {
            var movement = new PlayerMovementEvent(
                GetComponent<CharacterController>(),
                GetComponent<AttributeSet>(),
                playerDefinition,
                transform,
                cameraHolder,
                inputActions,
                resourceController
            );

            PushEvent(movement);

            ServiceLocator.Register<PlayerEventHandler>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerEventHandler>();
        }
    }
}