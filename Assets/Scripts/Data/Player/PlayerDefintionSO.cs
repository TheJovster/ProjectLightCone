using UnityEngine;

namespace LightCone.Data.Player
{
    /// <summary>
    /// ScriptableObject defining player movement and interaction tuning.
    /// All player constants live here — the controller just reads them.
    /// </summary>
    [CreateAssetMenu(fileName = "Player Settings", menuName = "LightCone/Data/Player Settings")]
    public sealed class PlayerDefinitionSO : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Base walk speed in units/second.")]
        [SerializeField] private float walkSpeed = 4f;
        [Tooltip("Sprint speed multiplier applied to walk speed.")]
        [SerializeField] private float sprintMultiplier = 1.6f;
        [Tooltip("Crouch speed multiplier applied to walk speed.")]
        [SerializeField] private float crouchMultiplier = 0.5f;

        [Header("Jump")]
        [Tooltip("Jump height in units.")]
        [SerializeField] private float jumpHeight = 1.2f;
        [Tooltip("Gravity magnitude (positive value, applied downward).")]
        [SerializeField] private float gravity = 20f;

        [Header("Crouch")]
        [Tooltip("Standing CharacterController height.")]
        [SerializeField] private float standingHeight = 2f;
        [Tooltip("Crouching CharacterController height.")]
        [SerializeField] private float crouchHeight = 1.2f;
        [Tooltip("Speed of height transition (units/second).")]
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Stamina")]
        [Tooltip("Resource ID for stamina on the player's ResourceController.")]
        [SerializeField] private string staminaResourceId = "stamina";
        [Tooltip("Stamina consumed per second while sprinting.")]
        [SerializeField] private float sprintStaminaCost = 10f;
        [Tooltip("Stamina consumed per jump.")]
        [SerializeField] private float jumpStaminaCost = 8f;
        [Tooltip("Minimum stamina required to start sprinting.")]
        [SerializeField] private float sprintStaminaThreshold = 1f;

        [Header("Interaction")]
        [Tooltip("Maximum raycast distance for interaction.")]
        [SerializeField] private float interactionRange = 2.5f;
        [Tooltip("Layer mask for interaction raycasts.")]
        [SerializeField] private LayerMask interactionMask = ~0;

        [Header("Camera")]
        [Tooltip("Mouse sensitivity.")]
        [SerializeField] private float lookSensitivity = 2f;
        [Tooltip("Maximum vertical look angle (degrees).")]
        [SerializeField] private float maxLookAngle = 85f;
        [Tooltip("Look smoothing weight (0 = raw/no smoothing, higher = smoother). 10-15 is a good starting range.")]
        [Range(0f, 25f)]
        [SerializeField] private float lookSmoothing = 12f;

        // ── Public API ──────────────────────────────────────────────

        public float WalkSpeed => walkSpeed;
        public float SprintMultiplier => sprintMultiplier;
        public float CrouchMultiplier => crouchMultiplier;

        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;

        public float StandingHeight => standingHeight;
        public float CrouchHeight => crouchHeight;
        public float CrouchTransitionSpeed => crouchTransitionSpeed;

        public string StaminaResourceId => staminaResourceId;
        public float SprintStaminaCost => sprintStaminaCost;
        public float JumpStaminaCost => jumpStaminaCost;
        public float SprintStaminaThreshold => sprintStaminaThreshold;

        public float InteractionRange => interactionRange;
        public LayerMask InteractionMask => interactionMask;

        public float LookSensitivity => lookSensitivity;
        public float MaxLookAngle => maxLookAngle;
        public float LookSmoothing => lookSmoothing;

        /// <summary>
        /// Calculate jump velocity from desired jump height and gravity.
        /// v = sqrt(2 * g * h)
        /// </summary>
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);
    }
}