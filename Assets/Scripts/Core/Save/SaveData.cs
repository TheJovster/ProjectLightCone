using System;
using System.Collections.Generic;

namespace LightCone.Core.Save
{
    /// <summary>
    /// Top-level save file container. This is what gets serialized to disk.
    /// Contains metadata for the save slot UI and a dictionary of system states.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public SaveMetadata metadata;
        public Dictionary<string, object> systemStates;

        public SaveData()
        {
            metadata = new SaveMetadata();
            systemStates = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Metadata displayed in save/load UI. Not gameplay state.
    /// </summary>
    [Serializable]
    public sealed class SaveMetadata
    {
        public int slotIndex;
        public string saveName;
        public string timestamp;
        public float playTimeSeconds;
        public int dayCount;
        public float currentHour;

        /// <summary>
        /// Stamp the metadata with current real time.
        /// </summary>
        public void Stamp(float playTime, int day, float hour)
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            playTimeSeconds = playTime;
            dayCount = day;
            currentHour = hour;
        }
    }
}