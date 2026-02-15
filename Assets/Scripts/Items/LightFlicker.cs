using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Intensity")]
    [SerializeField] private float baseIntensity = 1.2f;
    [SerializeField] [Range(0.0f, 2.0f)] private float flickerAmount = 0.15f;

    [Header("Timing")]
    [SerializeField] private float flickerSpeed = 3f;

    [Header("Optional")]
    [SerializeField] private float rangePulse = 0.1f;

    private Light pointLight;
    private float baseRange;
    private float seed;

    private void Awake()
    {
        pointLight = GetComponentInChildren<Light>();
        baseRange = pointLight.range;
        seed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        float sharp = Mathf.PerlinNoise(seed + 100f, Time.time * flickerSpeed * 3f);

        float combined = Mathf.Lerp(noise, sharp, 0.3f);

        pointLight.intensity = baseIntensity + (combined - 0.5f) * flickerAmount * 2f;
        pointLight.range = baseRange + (combined - 0.5f) * rangePulse * 2f;
    }
}