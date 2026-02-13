using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Core.Services;

namespace LightCone.Core.Save
{
    /// <summary>
    /// Orchestrates save/load operations across all registered ISaveable systems.
    /// Supports multiple save slots. Writes to Application.persistentDataPath.
    /// Does NOT interpret system data — each system owns its own serialization shape.
    /// </summary>
    public sealed class SaveManager
    {
        private const string SaveFolder = "Saves";
        private const string FileExtension = ".lcsave";
        private const int MaxSlots = 5;

        private readonly ISaveSerializer serializer;
        private readonly List<ISaveable> saveables = new();
        private readonly string saveFolderPath;
        private float sessionStartTime;

        public int SlotCount => MaxSlots;

        public SaveManager(ISaveSerializer serializer)
        {
            this.serializer = serializer;
            saveFolderPath = Path.Combine(Application.persistentDataPath, SaveFolder);
            sessionStartTime = UnityEngine.Time.realtimeSinceStartup;

            EnsureSaveFolder();
        }

        /// <summary>
        /// Register a saveable system. Call during initialization.
        /// Order of registration determines restoration order.
        /// </summary>
        public void Register(ISaveable saveable)
        {
            if (saveable == null)
            {
                Debug.LogWarning("[SaveManager] Cannot register null saveable.");
                return;
            }

            if (saveables.Contains(saveable))
            {
                Debug.LogWarning($"[SaveManager] Saveable already registered: {saveable.SaveKey}");
                return;
            }

            saveables.Add(saveable);
        }

        /// <summary>
        /// Unregister a saveable system. Call on system destruction.
        /// </summary>
        public void Unregister(ISaveable saveable)
        {
            saveables.Remove(saveable);
        }

        /// <summary>
        /// Save the current game state to a slot.
        /// </summary>
        public bool Save(int slotIndex, string saveName = null)
        {
            if (!ValidateSlot(slotIndex))
            {
                return false;
            }

            var data = new SaveData();
            data.metadata.slotIndex = slotIndex;
            data.metadata.saveName = saveName ?? $"Save {slotIndex + 1}";

            // Let the time system provide day/hour if available
            StampMetadata(data.metadata);

            // Collect state from all saveables
            for (int i = 0; i < saveables.Count; i++)
            {
                var saveable = saveables[i];
                var state = saveable.CaptureState();

                if (state != null)
                {
                    data.systemStates[saveable.SaveKey] = state;
                }
            }

            // Serialize and write
            string json = serializer.Serialize(data);
            string filePath = GetSlotPath(slotIndex);

            try
            {
                File.WriteAllText(filePath, json);
            }
            catch (IOException e)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {e.Message}");
                return false;
            }

            EventBus.Publish(new GameSavedEvent
            {
                SlotIndex = slotIndex,
                SaveName = data.metadata.saveName
            });

            Debug.Log($"[SaveManager] Saved to slot {slotIndex}: {filePath}");
            return true;
        }

        /// <summary>
        /// Load game state from a slot. Returns false if slot is empty or corrupt.
        /// </summary>
        public bool Load(int slotIndex)
        {
            if (!ValidateSlot(slotIndex))
            {
                return false;
            }

            string filePath = GetSlotPath(slotIndex);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] No save file in slot {slotIndex}.");
                return false;
            }

            string raw;

            try
            {
                raw = File.ReadAllText(filePath);
            }
            catch (IOException e)
            {
                Debug.LogError($"[SaveManager] Failed to read save file: {e.Message}");
                return false;
            }

            var data = serializer.Deserialize(raw);

            if (data == null)
            {
                Debug.LogError($"[SaveManager] Failed to deserialize slot {slotIndex}.");
                return false;
            }

            // Distribute state to all saveables
            for (int i = 0; i < saveables.Count; i++)
            {
                var saveable = saveables[i];

                if (data.systemStates.TryGetValue(saveable.SaveKey, out var state))
                {
                    saveable.RestoreState(state);
                }
            }

            // Reset session timer
            sessionStartTime = UnityEngine.Time.realtimeSinceStartup;

            EventBus.Publish(new GameLoadedEvent
            {
                SlotIndex = slotIndex,
                SaveName = data.metadata.saveName
            });

            Debug.Log($"[SaveManager] Loaded slot {slotIndex}.");
            return true;
        }

        /// <summary>
        /// Delete a save slot.
        /// </summary>
        public bool DeleteSlot(int slotIndex)
        {
            if (!ValidateSlot(slotIndex))
            {
                return false;
            }

            string filePath = GetSlotPath(slotIndex);

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogError($"[SaveManager] Failed to delete save file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a save slot has data.
        /// </summary>
        public bool SlotExists(int slotIndex)
        {
            return ValidateSlot(slotIndex) && File.Exists(GetSlotPath(slotIndex));
        }

        /// <summary>
        /// Get metadata for a save slot without loading the full state.
        /// Returns null if slot is empty.
        /// </summary>
        public SaveMetadata GetSlotMetadata(int slotIndex)
        {
            if (!ValidateSlot(slotIndex))
            {
                return null;
            }

            string filePath = GetSlotPath(slotIndex);

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string raw = File.ReadAllText(filePath);
                var data = serializer.Deserialize(raw);
                return data?.metadata;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get metadata for all slots. Empty slots return null.
        /// Useful for populating a save/load UI.
        /// </summary>
        public SaveMetadata[] GetAllSlotMetadata()
        {
            var result = new SaveMetadata[MaxSlots];

            for (int i = 0; i < MaxSlots; i++)
            {
                result[i] = GetSlotMetadata(i);
            }

            return result;
        }

        /// <summary>
        /// Get the elapsed session play time in seconds.
        /// </summary>
        public float GetSessionPlayTime()
        {
            return UnityEngine.Time.realtimeSinceStartup - sessionStartTime;
        }

        private void StampMetadata(SaveMetadata metadata)
        {
            float playTime = GetSessionPlayTime();
            int day = 1;
            float hour = 0f;

            // Pull time data if available
            if (ServiceLocator.TryGet<Systems.Time.TimeSystem>(out var timeSystem))
            {
                day = timeSystem.CurrentDay;
                hour = timeSystem.CurrentHour;
            }

            // Add previous play time from loaded save if applicable
            metadata.Stamp(playTime, day, hour);
        }

        private string GetSlotPath(int slotIndex)
        {
            return Path.Combine(saveFolderPath, $"slot_{slotIndex}{FileExtension}");
        }

        private bool ValidateSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < MaxSlots)
            {
                return true;
            }

            Debug.LogError($"[SaveManager] Invalid slot index: {slotIndex}. Must be 0-{MaxSlots - 1}.");
            return false;
        }

        private void EnsureSaveFolder()
        {
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }
        }
    }
}