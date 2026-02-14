using UnityEngine;
using UnityEngine.InputSystem;
using LightCone.Core.Events;
using LightCone.Data.Player;
using LightCone.Systems.Resources;
using LightCone.Systems.Stats;

namespace LightCone.Gameplay.Player
{
    /// <summary>
    /// First-person player movement controller.
    /// Handles walk, sprint, crouch, and jump using CharacterController.
    /// Reads movement speed from AttributeSet and stamina from ResourceController.
    /// Requires the new Input System.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AttributeSet))]
    [RequireComponent(typeof(ResourceController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerDefinitionSO playerDefinition;

        [Header("Crouch Settings")]
        [SerializeField] private bool toggleCrouch;

        [Header("References")]
        [Tooltip("The camera transform (child of this GameObject).")]
        [SerializeField] private Transform cameraHolder;

        private CharacterController characterController;
        private AttributeSet attributeSet;
        private ResourceController resourceController;

        // Input state
        private Vector2 moveInput;
        private bool sprintHeld;
        private bool crouchInput;
        private bool jumpRequested;

        // Movement state
        private Vector3 velocity;
        [SerializeField] private bool isCrouching;
        private bool isSprinting;
        private float currentHeight;
        private float standingCameraY;

        public bool IsGrounded => characterController.isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public Vector3 Velocity => velocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            attributeSet = GetComponent<AttributeSet>();
            resourceController = GetComponent<ResourceController>();
        }

        private void Start()
        {
            currentHeight = playerDefinition.StandingHeight;
            characterController.height = currentHeight;

            // Cache standing camera offset (camera Y relative to controller center)
            if (cameraHolder != null)
            {
                standingCameraY = cameraHolder.localPosition.y;
            }
        }

        private void Update()
        {
            float deltaTime = UnityEngine.Time.deltaTime;

            UpdateSprintState(deltaTime);
            UpdateCrouchState(deltaTime);
            UpdateGravityAndJump(deltaTime);
            UpdateMovement(deltaTime);
        }

        // ── Input System Callbacks ──────────────────────────────────
        // Wire these to a PlayerInput component or call them from an input wrapper.

        /// <summary>
        /// Called by the Input System for movement (WASD / left stick).
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Called by the Input System for sprint (Shift / left stick click).
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            sprintHeld = context.performed;

            if (context.canceled)
            {
                sprintHeld = false;
            }
        }

        /// <summary>
        /// Called by the Input System for crouch (Ctrl / B button).
        /// </summary>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.canceled)
            {
                if (!toggleCrouch)
                {
                    crouchInput = false;
                }

                return;
            }

            if (!context.performed)
            {
                return;
            }

            if (toggleCrouch)
            {
                crouchInput = !crouchInput;
            }
            else
            {
                crouchInput = true;
            }
        }

        /// <summary>
        /// Called by the Input System for jump (Space / A button).
        /// </summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpRequested = true;
            }
        }

        // ── Movement Logic ──────────────────────────────────────────

        private void UpdateSprintState(float deltaTime)
        {
            bool wantsToSprint = sprintHeld
                && moveInput.y > 0.1f
                && !isCrouching
                && characterController.isGrounded;

            if (wantsToSprint)
            {
                // Check stamina availability
                bool hasStamina = resourceController.CanAfford(
                    playerDefinition.StaminaResourceId,
                    playerDefinition.SprintStaminaThreshold
                );

                if (hasStamina)
                {
                    isSprinting = true;
                    resourceController.Consume(
                        playerDefinition.StaminaResourceId,
                        playerDefinition.SprintStaminaCost * deltaTime
                    );
                }
                else
                {
                    isSprinting = false;
                }
            }
            else
            {
                isSprinting = false;
            }
        }

        private void UpdateCrouchState(float deltaTime)
        {
            bool wantsCrouch = crouchInput;
            Debug.Log("wantsCrouch is " + wantsCrouch);

            // If crouching and want to stand, check for ceiling
            if (isCrouching && !wantsCrouch)
            {
                if (IsCeilingAbove())
                {
                    Debug.Log("Ceiling above " + IsCeilingAbove());
                    // Can't stand up — ceiling is too low
                    wantsCrouch = true;

                    // Keep toggle state consistent
                    if (toggleCrouch)
                    {
                        crouchInput = true;
                    }
                }
            }

            isCrouching = wantsCrouch;

            // Cancel sprint if crouching
            if (isCrouching)
            {
                isSprinting = false;
            }

            // Smoothly transition height
            float targetHeight = isCrouching
                ? playerDefinition.CrouchHeight
                : playerDefinition.StandingHeight;

            if (!Mathf.Approximately(currentHeight, targetHeight))
            {
                float previousHeight = currentHeight;
                currentHeight = Mathf.MoveTowards(
                    currentHeight,
                    targetHeight,
                    playerDefinition.CrouchTransitionSpeed * deltaTime
                );

                characterController.height = currentHeight;

                // Adjust center so the feet stay planted
                float centerY = currentHeight * 0.5f;
                characterController.center = new Vector3(0f, centerY, 0f);

                // Move the camera holder proportionally
                if (cameraHolder != null)
                {
                    float heightRatio = currentHeight / playerDefinition.StandingHeight;
                    cameraHolder.localPosition = new Vector3(
                        cameraHolder.localPosition.x,
                        standingCameraY * heightRatio,
                        cameraHolder.localPosition.z
                    );
                }
            }
        }

        private void UpdateGravityAndJump(float deltaTime)
        {
            if (characterController.isGrounded && velocity.y < 0f)
            {
                // Small downward force to keep grounded (CharacterController quirk)
                velocity.y = -2f;
            }

            // Handle jump
            if (jumpRequested)
            {
                jumpRequested = false;

                if (characterController.isGrounded && !isCrouching)
                {
                    bool canAffordJump = resourceController.CanAfford(
                        playerDefinition.StaminaResourceId,
                        playerDefinition.JumpStaminaCost
                    );

                    if (canAffordJump)
                    {
                        velocity.y = playerDefinition.JumpVelocity;
                        resourceController.Consume(
                            playerDefinition.StaminaResourceId,
                            playerDefinition.JumpStaminaCost
                        );
                    }
                }
            }

            // Apply gravity
            velocity.y -= playerDefinition.Gravity * deltaTime;
        }

        private void UpdateMovement(float deltaTime)
        {
            // Calculate speed
            float baseSpeed = playerDefinition.WalkSpeed;

            // MovementSpeed attribute acts as a multiplier (base 1.0 = normal).
            // Status effects like Exhaustion modify this via the AttributeSet.
            // If the attribute isn't initialized, default to 1.0 (no modification).
            float speedModifier = attributeSet.HasAttribute(AttributeType.MovementSpeed)
                ? attributeSet.GetValue(AttributeType.MovementSpeed)
                : 1f;

            baseSpeed *= Mathf.Max(speedModifier, 0f);

            if (isSprinting)
            {
                baseSpeed *= playerDefinition.SprintMultiplier;
            }
            else if (isCrouching)
            {
                baseSpeed *= playerDefinition.CrouchMultiplier;
            }

            // Build movement vector in local space
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

            // Apply horizontal movement + vertical velocity
            Vector3 finalMove = move * baseSpeed + Vector3.up * velocity.y;
            characterController.Move(finalMove * deltaTime);
        }

        private bool IsCeilingAbove()
        {
            float checkDistance = playerDefinition.StandingHeight - playerDefinition.CrouchHeight;
            Vector3 origin = transform.position + Vector3.up * (currentHeight + 0.05f);
            return Physics.Raycast(origin, Vector3.up, checkDistance);
        }
    }
}