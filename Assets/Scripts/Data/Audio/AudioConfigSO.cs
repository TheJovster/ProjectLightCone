using UnityEngine;

namespace LightCone.Data.Audio
{
    [CreateAssetMenu(fileName = "Audio Config", menuName = "LightCone/Data/Audio Config")]
    public sealed class AudioConfigSO : ScriptableObject
    {
        [SerializeField] private int poolSize = 10;

        [SerializeField] private float defaultMasterVolume = 1f;
        [SerializeField] private float defaultMusicVolume = 1f;
        [SerializeField] private float defaultAmbientVolume = 1f;
        [SerializeField] private float defaultSfxVolume = 1f;

        // Properties
        public int PoolSize => poolSize;
        public float DefaultMasterVolume => defaultMasterVolume;
        public float DefaultMusicVolume => defaultMusicVolume;
        public float DefaultAmbientVolume => defaultAmbientVolume;
        public float DefaultSfxVolume => defaultSfxVolume;
    }
}


