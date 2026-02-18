using UnityEngine;
using LightCone.Data.Audio;

namespace LightCone.Systems.Audio
{
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        // ── Configuration ──────────────────────────────────────────
        [SerializeField] private AudioConfigSO audioConfig;

        // ── Dedicated channel sources ──────────────────────────────
        private AudioSource musicSource;
        private AudioSource ambientSource;

        // ── Pooled one-shot sources ────────────────────────────────
        private AudioSource[] sfxPool;
        private int nextPoolIndex;

        // ── Volume state ───────────────────────────────────────────
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float ambientVolume = 1f;
        private float sfxVolume = 1f;

        // ── Public API ─────────────────────────────────────────────
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float AmbientVolume => ambientVolume;
        public float SfxVolume => sfxVolume;
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        /// <summary>
        /// Create audio sources and apply configuration.
        /// Called by the bootstrapper after instantiation.
        /// </summary>
        public void Initialize(AudioConfigSO config)
        {
            audioConfig = config;

            // Dedicated music source — 2D, looping
            musicSource = CreateSource("Music", loop: true, spatialBlend: 0f);

            // Dedicated ambient source — 2D, looping
            ambientSource = CreateSource("Ambient", loop: true, spatialBlend: 0f);

            // SFX pool — individual sources on child GameObjects
            sfxPool = new AudioSource[audioConfig.PoolSize];
            nextPoolIndex = 0;

            for (int i = 0; i < sfxPool.Length; i++)
            {
                sfxPool[i] = CreateSource($"SFX_{i}", loop: false, spatialBlend: 1f);
            }

            // Apply volume defaults from config
            masterVolume = audioConfig.DefaultMasterVolume;
            musicVolume = audioConfig.DefaultMusicVolume;
            ambientVolume = audioConfig.DefaultAmbientVolume;
            sfxVolume = audioConfig.DefaultSfxVolume;

            ApplyVolumeToSource(musicSource, musicVolume);
            ApplyVolumeToSource(ambientSource, ambientVolume);
        }

        // ── HELPERS ───────────────────────────────────────────────
        private AudioSource CreateSource(string label, bool loop, float spatialBlend)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;

            return source;
        }

        private void ApplyVolumeToSource(AudioSource source, float channelVolume)
        {
            source.volume = channelVolume * masterVolume;
        }

        /// <summary>
        /// Play a one-shot sound effect. Positional by default (3D).
        /// Pass Vector3.zero with spatialBlend 0 for 2D sounds (UI, etc).
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volumeScale = 1f, float spatialBlend = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioService] Null clip passed to PlaySFX.");
                return;
            }

            var source = sfxPool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % sfxPool.Length;

            source.Stop();
            source.clip = clip;
            source.transform.position = position;
            source.spatialBlend = spatialBlend;
            source.volume = sfxVolume * masterVolume * volumeScale;
            source.Play();
        }

        /// <summary>
        /// Play a music track. Replaces any currently playing music.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeInDuration = 0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioService] Null clip passed to PlayMusic.");
                return;
            }

            musicSource.clip = clip;
            ApplyVolumeToSource(musicSource, musicVolume);
            musicSource.Play();
        }

        /// <summary>
        /// Play an ambient track. Replaces any currently playing ambient.
        /// </summary>
        public void PlayAmbient(AudioClip clip, float fadeInDuration = 0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioService] Null clip passed to PlayAmbient.");
                return;
            }

            ambientSource.clip = clip;
            ApplyVolumeToSource(ambientSource, ambientVolume);
            ambientSource.Play();
        }

        /// <summary>
        /// Stop the current music track.
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        /// <summary>
        /// Stop the current ambient track.
        /// </summary>
        public void StopAmbient()
        {
            ambientSource.Stop();
            ambientSource.clip = null;
        }

        /// <summary>
        /// Set master volume. Updates music and ambient immediately.
        /// SFX pool applies on next play.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource(musicSource, musicVolume);
            ApplyVolumeToSource(ambientSource, ambientVolume);
        }

        /// <summary>
        /// Set music channel volume. Updates immediately.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource(musicSource, musicVolume);
        }

        /// <summary>
        /// Set ambient channel volume. Updates immediately.
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ApplyVolumeToSource(ambientSource, ambientVolume);
        }

        /// <summary>
        /// Set SFX channel volume. Applies to future sounds only.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        private void OnDestroy()
        {
            // Sources are on child GameObjects — they die with us.
            // Just null references for safety.
            musicSource = null;
            ambientSource = null;
            sfxPool = null;
        }
    }
}

