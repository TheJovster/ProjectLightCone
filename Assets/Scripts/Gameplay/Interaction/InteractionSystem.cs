using UnityEngine;
using UnityEngine.InputSystem;
using LightCone.Core.Events;
using LightCone.Data.Player;

namespace LightCone.Gameplay.Interaction
{
    /// <summary>
    /// Handles player interaction with world objects.
    /// Raycasts from the camera to find IInteractable targets.
    /// Publishes events for UI prompt display.
    /// </summary>
    public sealed class InteractionSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerDefinitionSO playerDefinition;

        [Header("References")]
        [Tooltip("The camera to raycast from.")]
        [SerializeField] private Transform cameraTransform;

        private IInteractable currentTarget;
        private bool interactRequested;

        /// <summary>
        /// The currently targeted interactable, or null if none.
        /// </summary>
        public IInteractable CurrentTarget => currentTarget;

        /// <summary>
        /// Whether the player is currently looking at a valid interactable.
        /// </summary>
        public bool HasTarget => currentTarget != null && currentTarget.CanInteract;

        private void Update()
        {
            UpdateRaycast();
            ProcessInteraction();
        }

        /// <summary>
        /// Called by the Input System for the interact action (E / X button).
        /// </summary>
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                interactRequested = true;
            }
        }

        private void UpdateRaycast()
        {
            if (cameraTransform == null)
            {
                return;
            }

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            IInteractable newTarget = null;

            if (Physics.Raycast(ray, out var hit, playerDefinition.InteractionRange, playerDefinition.InteractionMask))
            {
                newTarget = hit.collider.GetComponentInParent<IInteractable>();
            }

            // Check if target changed
            if (newTarget == currentTarget)
            {
                return;
            }

            var previousTarget = currentTarget;
            currentTarget = newTarget;

            // Notify UI about target change
            if (previousTarget != null)
            {
                EventBus.Publish(new InteractionTargetLostEvent());
            }

            if (currentTarget != null && currentTarget.CanInteract)
            {
                EventBus.Publish(new InteractionTargetFoundEvent
                {
                    Prompt = currentTarget.InteractionPrompt
                });
            }
        }

        private void ProcessInteraction()
        {
            if (!interactRequested)
            {
                return;
            }

            interactRequested = false;

            if (currentTarget == null || !currentTarget.CanInteract)
            {
                return;
            }

            currentTarget.Interact();
        }
    }
}