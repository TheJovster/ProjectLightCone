namespace LightCone.Core.Save
{
    /// <summary>
    /// Implement on any MonoBehaviour or system that has state worth saving.
    /// Each saveable provides a unique key and handles its own serialization shape.
    /// The SaveManager orchestrates collection and distribution — it never interprets the data.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique identifier for this saveable's data block.
        /// Must be consistent across sessions (do NOT use InstanceID).
        /// Convention: use the system name, e.g., "TimeSystem", "PlayerAttributes".
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Capture current state into a serializable object.
        /// Called by SaveManager when saving.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restore state from a previously captured object.
        /// Called by SaveManager when loading.
        /// The object type will match what CaptureState returned.
        /// </summary>
        void RestoreState(object state);
    }
}