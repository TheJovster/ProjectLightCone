using UnityEngine;
using UnityEngine.InputSystem;
using LightCone.Gameplay.Interaction;

namespace LightCone.Gameplay.Player
{
    /// <summary>
    /// Single owner of all player input bindings.
    /// Subscribes to Input System actions and routes them to consumer components.
    /// Input pipeline: InputSystem → PlayerInputBinder → Consumers.
    /// All input gating (menus, cutscenes, death) happens here.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerLook))]
    public sealed class PlayerInputBinder : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        private PlayerController playerController;
        private PlayerLook playerLook;
        private InteractionSystem interactionSystem;

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction jumpAction;
        private InputAction interactAction;

        private bool inputEnabled = true;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerLook = GetComponent<PlayerLook>();
            interactionSystem = GetComponent<InteractionSystem>();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerInputBinder] No InputActionAsset assigned.");
                return;
            }

            playerMap = inputActions.FindActionMap("Player");

            if (playerMap == null)
            {
                Debug.LogError("[PlayerInputBinder] 'Player' action map not found in InputActionAsset.");
                return;
            }

            CacheActions();
            Subscribe();
            playerMap.Enable();
        }

        private void OnDisable()
        {
            Unsubscribe();
            playerMap?.Disable();
        }

        /// <summary>
        /// Enable all player input. Call when resuming gameplay.
        /// </summary>
        public void EnableInput()
        {
            inputEnabled = true;
            playerMap?.Enable();
            playerLook.LockCursor();
        }

        /// <summary>
        /// Disable all player input. Call for menus, cutscenes, death.
        /// </summary>
        public void DisableInput()
        {
            inputEnabled = false;
            playerMap?.Disable();
            playerLook.UnlockCursor();
        }

        private void CacheActions()
        {
            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            sprintAction = playerMap.FindAction("Sprint");
            crouchAction = playerMap.FindAction("Crouch");
            jumpAction = playerMap.FindAction("Jump");
            interactAction = playerMap.FindAction("Interact");
        }

        private void Subscribe()
        {
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }

            if (lookAction != null)
            {
                lookAction.performed += OnLook;
                lookAction.canceled += OnLook;
            }

            if (sprintAction != null)
            {
                sprintAction.performed += OnSprint;
                sprintAction.canceled += OnSprint;
            }

            if (crouchAction != null)
            {
                crouchAction.performed += OnCrouch;
                crouchAction.canceled += OnCrouch;
            }

            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteract;
            }
        }

        private void Unsubscribe()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }

            if (lookAction != null)
            {
                lookAction.performed -= OnLook;
                lookAction.canceled -= OnLook;
            }

            if (sprintAction != null)
            {
                sprintAction.performed -= OnSprint;
                sprintAction.canceled -= OnSprint;
            }

            if (crouchAction != null)
            {
                crouchAction.performed -= OnCrouch;
                crouchAction.canceled -= OnCrouch;
            }

            if (jumpAction != null)
            {
                jumpAction.performed -= OnJump;
            }

            if (interactAction != null)
            {
                interactAction.performed -= OnInteract;
            }
        }

        // ── Input Routing ───────────────────────────────────────────
        // Each method is the single point where input can be intercepted.

        private void OnMove(InputAction.CallbackContext context)
        {
            playerController.OnMove(context);
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            playerLook.OnLook(context);
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            playerController.OnSprint(context);
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            playerController.OnCrouch(context);
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            playerController.OnJump(context);
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (interactionSystem != null)
            {
                interactionSystem.OnInteract(context);
            }
        }
    }
}