using UnityEngine;

namespace LightCone.Core.Save
{
    /// <summary>
    /// Abstraction for save file serialization.
    /// Swap implementations to change format (JSON, binary, encrypted)
    /// without touching the SaveManager or any saveable system.
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        /// Serialize SaveData to a string for writing to disk.
        /// </summary>
        string Serialize(SaveData data);

        /// <summary>
        /// Deserialize a string back into SaveData.
        /// Returns null if deserialization fails.
        /// </summary>
        SaveData Deserialize(string raw);
    }

    /// <summary>
    /// JSON serializer using Unity's JsonUtility for metadata
    /// and a lightweight manual approach for the system state dictionary.
    /// Readable, debuggable, good for development.
    /// </summary>
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        public string Serialize(SaveData data)
        {
            return JsonUtility.ToJson(new JsonSaveWrapper(data), true);
        }

        public SaveData Deserialize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<JsonSaveWrapper>(raw);
                return wrapper?.ToSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JsonSaveSerializer] Failed to deserialize: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Unity's JsonUtility cannot serialize Dictionary directly.
        /// This wrapper converts to parallel arrays for serialization.
        /// </summary>
        [System.Serializable]
        private sealed class JsonSaveWrapper
        {
            public SaveMetadata metadata;
            public string[] stateKeys;
            public string[] stateValues;

            public JsonSaveWrapper() { }

            public JsonSaveWrapper(SaveData data)
            {
                metadata = data.metadata;

                int count = data.systemStates.Count;
                stateKeys = new string[count];
                stateValues = new string[count];

                int i = 0;

                foreach (var kvp in data.systemStates)
                {
                    stateKeys[i] = kvp.Key;
                    stateValues[i] = JsonUtility.ToJson(kvp.Value);
                    i++;
                }
            }

            public SaveData ToSaveData()
            {
                var data = new SaveData
                {
                    metadata = metadata
                };

                if (stateKeys != null)
                {
                    for (int i = 0; i < stateKeys.Length; i++)
                    {
                        // Store raw JSON strings — systems will deserialize their own data
                        data.systemStates[stateKeys[i]] = stateValues[i];
                    }
                }

                return data;
            }
        }
    }
}