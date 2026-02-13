using System;
using UnityEngine;

namespace LightCone.Data.Rest
{
    /// <summary>
    /// ScriptableObject defining rest behavior and costs.
    /// Controls what resources are consumed and restored during rest.
    /// </summary>
    [CreateAssetMenu(fileName = "Rest Settings", menuName = "LightCone/Data/Rest Settings")]
    public sealed class RestDefinitionSO : ScriptableObject
    {
        [Header("Costs")]
        [Tooltip("Resources consumed per in-game hour of rest.")]
        [SerializeField] private RestResourceCost[] costsPerHour;

        [Header("Recovery")]
        [Tooltip("Resources restored per in-game hour of rest.")]
        [SerializeField] private RestResourceRecovery[] recoveryPerHour;

        [Header("Constraints")]
        [Tooltip("Minimum rest duration in in-game hours.")]
        [SerializeField] private float minimumHours = 1f;
        [Tooltip("Maximum rest duration in in-game hours.")]
        [SerializeField] private float maximumHours = 8f;
        [Tooltip("If true, rest is aborted if any cost resource is fully depleted.")]
        [SerializeField] private bool abortOnResourceDepleted = true;

        public RestResourceCost[] CostsPerHour => costsPerHour;
        public RestResourceRecovery[] RecoveryPerHour => recoveryPerHour;
        public float MinimumHours => minimumHours;
        public float MaximumHours => maximumHours;
        public bool AbortOnResourceDepleted => abortOnResourceDepleted;

        /// <summary>
        /// A resource consumed during rest (e.g., rations, torch light).
        /// </summary>
        [Serializable]
        public struct RestResourceCost
        {
            [Tooltip("Resource ID to consume.")]
            public string resourceId;
            [Tooltip("Amount consumed per in-game hour of rest.")]
            public float amountPerHour;
        }

        /// <summary>
        /// A resource restored during rest (e.g., stamina, health).
        /// </summary>
        [Serializable]
        public struct RestResourceRecovery
        {
            [Tooltip("Resource ID to restore.")]
            public string resourceId;
            [Tooltip("Flat amount restored per in-game hour of rest.")]
            public float amountPerHour;
            [Tooltip("If true, amountPerHour is a percentage of max (0-1) instead of flat.")]
            public bool isPercentOfMax;
        }
    }
}