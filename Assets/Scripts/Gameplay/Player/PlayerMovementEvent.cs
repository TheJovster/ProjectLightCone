using UnityEngine;
using LightCone.Core.Events;
using LightCone.Data.Player;
using LightCone.Systems.Stats;
using UnityEngine.InputSystem;
using LightCone.Systems.Rest;
using LightCone.Systems.Resources;

namespace LightCone.Gameplay.Player 
{
    /// <summary>
    /// Core movement event. Lives on the player's event stack permanently.
    /// Handles input reading, movement, and camera look.
    /// When this event is on top, the player can move. When preempted, they can't.
    /// </summary>
    public sealed class PlayerMovementEvent : EventHandler.GameEvent
    {
        private readonly CharacterController characterController;
        private readonly AttributeSet attributeSet;
        private readonly PlayerDefinitionSO playerDefinition;
        private readonly ResourceController resourceController;
        private readonly Transform playerTransform;
        private readonly Transform cameraHolder;
        private readonly InputActionMap playerMap;

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector3 velocity;
        private float verticalRotation;

        //actions
        private bool sprintHeld;

        public PlayerMovementEvent(
            CharacterController characterController,
            AttributeSet attributeSet,
            PlayerDefinitionSO playerDefinition,
            Transform playerTransform,
            Transform cameraHolder,
            InputActionAsset inputActions,
            ResourceController resourceController)
        {
            this.characterController = characterController;
            this.attributeSet = attributeSet;
            this.playerDefinition = playerDefinition;
            this.playerTransform = playerTransform;
            this.cameraHolder = cameraHolder;
            this.resourceController = resourceController;

            playerMap = inputActions.FindActionMap("Player");
            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            sprintAction = playerMap.FindAction("Sprint");
        }

        public override void OnBegin(bool bFirstTime)
        {
            playerMap.Enable();
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
            sprintAction.performed += OnSprint;
            sprintAction.canceled += OnSprint;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;

            ApplyLook();
            ApplyGravity(deltaTime);
            ApplyMovement(deltaTime);

            Debug.Log($"Stamina: {resourceController.GetValue(playerDefinition.StaminaResourceId):F1}");
        }

        public override void OnEnd()
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
            sprintAction.performed -= OnSprint;
            sprintAction.canceled -= OnSprint;
            playerMap.Disable();
        }

        public override bool IsDone()
        {
            return false;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            sprintHeld = context.performed;
            Debug.Log($"Sprint held: {sprintHeld}");

            if (context.canceled)
            {
                sprintHeld = false;
                Debug.Log("Sprint released");
            }
        }

        private void ApplyLook()
        {
            float sensitivity = playerDefinition.LookSensitivity;

            playerTransform.Rotate(Vector3.up, lookInput.x * sensitivity);

            verticalRotation -= lookInput.y * sensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -playerDefinition.MaxLookAngle, playerDefinition.MaxLookAngle);
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void ApplyGravity(float deltaTime)
        {
            if (characterController.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            velocity.y -= playerDefinition.Gravity * deltaTime;
        }

        private void ApplyMovement(float deltaTime)
        {
            float speed =  sprintHeld ? playerDefinition.WalkSpeed * playerDefinition.SprintMultiplier : playerDefinition.WalkSpeed;

            float speedModifier = attributeSet.HasAttribute(AttributeType.MovementSpeed)
                    ? attributeSet.GetValue(AttributeType.MovementSpeed)
                    : 1f;

            speed *= Mathf.Max(speedModifier, 0f);

            Vector3 move = playerTransform.right * moveInput.x + playerTransform.forward * moveInput.y;
            Vector3 finalMove = move * speed + Vector3.up * velocity.y;
            characterController.Move(finalMove * deltaTime);

            if (sprintHeld && moveInput.y > 0.1f && characterController.isGrounded)
            {
                bool hasStamina = resourceController.CanAfford(
                    playerDefinition.StaminaResourceId,
                    playerDefinition.SprintStaminaThreshold
                );

                speed *= hasStamina ? playerDefinition.SprintMultiplier : 1f;

                if (hasStamina)
                {
                    resourceController.Consume(
                        playerDefinition.StaminaResourceId,
                        playerDefinition.SprintStaminaCost * deltaTime
                    );
                }
            }
        }
    }
}
