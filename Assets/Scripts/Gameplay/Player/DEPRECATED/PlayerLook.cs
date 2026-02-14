using UnityEngine;
using UnityEngine.InputSystem;
using LightCone.Data.Player;

namespace LightCone.Gameplay.Player
{
    /// <summary>
    /// First-person mouse look controller.
    /// Rotates the player transform horizontally and the camera vertically.
    /// Uses LateUpdate to ensure camera moves after all movement/physics.
    /// Applies configurable input smoothing to eliminate frame-rate dependent jitter.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerDefinitionSO playerDefinition;

        [Header("References")]
        [Tooltip("The camera transform to rotate vertically.")]
        [SerializeField] private Transform cameraHolder;

        private Vector2 rawInput;
        private Vector2 smoothedInput;
        private float verticalRotation;
        private bool cursorLocked = true;

        private void Start()
        {
            LockCursor();
        }

        private void LateUpdate()
        {
            if (!cursorLocked)
            {
                return;
            }

            SmoothInput();
            ApplyRotation();
        }

        private void OnDestroy()
        {
            UnlockCursor();
        }

        /// <summary>
        /// Called by the Input System for look input (mouse delta / right stick).
        /// </summary>
        public void OnLook(InputAction.CallbackContext context)
        {
            rawInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Lock the cursor for gameplay.
        /// </summary>
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLocked = true;
        }

        /// <summary>
        /// Unlock the cursor for menus.
        /// </summary>
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorLocked = false;
        }

        private void SmoothInput()
        {
            float smoothing = playerDefinition.LookSmoothing;

            if (smoothing <= 0f)
            {
                // No smoothing — use raw input directly
                smoothedInput = rawInput;
                return;
            }

            // Exponential moving average using unscaledDeltaTime
            // so smoothing works identically during timeScale = 0 (pause menus).
            float t = smoothing * UnityEngine.Time.unscaledDeltaTime;
            smoothedInput = Vector2.Lerp(smoothedInput, rawInput, Mathf.Clamp01(t));
        }

        private void ApplyRotation()
        {
            float sensitivity = playerDefinition.LookSensitivity;
            float maxAngle = playerDefinition.MaxLookAngle;

            // Horizontal rotation — rotate the player body
            float horizontalRotation = smoothedInput.x * sensitivity;
            transform.Rotate(Vector3.up, horizontalRotation);

            // Vertical rotation — rotate the camera only
            verticalRotation -= smoothedInput.y * sensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxAngle, maxAngle);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }
    }
}