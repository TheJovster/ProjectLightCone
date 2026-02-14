namespace LightCone.Gameplay.Interaction
{
    /// <summary>
    /// Implement on any MonoBehaviour that the player can interact with.
    /// Examples: doors, chests, levers, NPCs, rest points, extraction points.
    /// The InteractionSystem raycasts from the camera and calls these methods.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Display name shown in the interaction prompt (e.g., "Open Door", "Rest", "Loot Chest").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Whether this object can currently be interacted with.
        /// Return false if locked, already used, conditions not met, etc.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Called when the player confirms interaction.
        /// </summary>
        void Interact();
    }
}