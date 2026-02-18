using UnityEngine;

namespace LightCone.Systems.Audio
{
    /// <summary>
    /// Service contract for the audio system.
    /// Consumers resolve this through the ServiceLocator.
    /// </summary>
    public interface IAudioService
    {
        float MasterVolume { get; }
        float MusicVolume { get; }
        float AmbientVolume { get; }
        float SfxVolume { get; }
        bool IsMusicPlaying { get; }

        void PlaySFX(AudioClip clip, Vector3 position, float volumeScale = 1f, float spatialBlend = 1f);
        void PlayMusic(AudioClip clip, float fadeInDuration = 0f);
        void PlayAmbient(AudioClip clip, float fadeInDuration = 0f);
        void StopMusic();
        void StopAmbient();

        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetAmbientVolume(float volume);
        void SetSfxVolume(float volume);
    }
}