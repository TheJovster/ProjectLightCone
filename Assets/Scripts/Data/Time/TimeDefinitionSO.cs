using System;
using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Data.Time
{
    /// <summary>
    /// ScriptableObject defining time system configuration.
    /// Controls day length, time periods, and their thresholds.
    /// All tuning lives here — the TimeSystem just reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "Time Settings", menuName = "LightCone/Data/Time Settings")]
    public sealed class TimeDefinitionSO : ScriptableObject
    {
        [Header("Day Cycle")]
        [Tooltip("Real-time seconds per in-game hour.")]
        [SerializeField] private float secondsPerHour = 60f;
        [Tooltip("Starting hour when a new game begins (0-23).")]
        [SerializeField][Range(0f, 23.99f)] private float startingHour = 8f;

        [Header("Time Periods")]
        [Tooltip("Define time periods in order. Each starts at its threshold hour.")]
        [SerializeField] private TimePeriodEntry[] periods;

        public float SecondsPerHour => secondsPerHour;
        public float StartingHour => startingHour;
        public float HoursPerDay => 24f;
        public TimePeriodEntry[] Periods => periods;

        /// <summary>
        /// Defines a named time period and the hour it begins.
        /// </summary>
        [Serializable]
        public struct TimePeriodEntry
        {
            [Tooltip("Identifier for this period.")]
            public TimePeriod period;
            [Tooltip("The hour this period starts (0-23.99).")]
            [Range(0f, 23.99f)]
            public float startHour;
            [Tooltip("Display name for UI.")]
            public string displayName;
        }
    }
}