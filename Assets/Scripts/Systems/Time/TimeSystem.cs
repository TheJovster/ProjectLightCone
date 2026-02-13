using System;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Core.Save;
using LightCone.Core.Services;
using LightCone.Data.Time;

namespace LightCone.Systems.Time
{
    /// <summary>
    /// Manages in-game time. Tracks current hour, day count, and time period.
    /// Publishes TimeTickEvent every in-game hour and TimePeriodChangedEvent on transitions.
    /// Supports pausing and advancing time (e.g., during rest).
    /// Implements ISaveable to persist time state across save/load.
    /// </summary>
    public sealed class TimeSystem : MonoBehaviour, ISaveable
    {
        [SerializeField] private TimeDefinitionSO timeDefinition;

        private float currentHour;
        private int currentDay;
        private TimePeriod currentPeriod;
        private float secondsAccumulator;
        private bool isPaused;

        public float CurrentHour => currentHour;
        public int CurrentDay => currentDay;
        public TimePeriod CurrentPeriod => currentPeriod;
        public bool IsPaused { get => isPaused; set => isPaused = value; }

        /// <summary>
        /// Normalized time of day (0-1). 0 = midnight, 0.5 = noon, 1 = midnight.
        /// Useful for lighting/shader interpolation.
        /// </summary>
        public float NormalizedTimeOfDay => currentHour / timeDefinition.HoursPerDay;

        /// <summary>
        /// Total elapsed in-game hours since the start.
        /// </summary>
        public float TotalElapsedHours => currentDay * timeDefinition.HoursPerDay + currentHour;

        // ── ISaveable ───────────────────────────────────────────────

        public string SaveKey => "TimeSystem";

        public object CaptureState()
        {
            return new TimeState
            {
                currentHour = currentHour,
                currentDay = currentDay
            };
        }

        public void RestoreState(object state)
        {
            // State arrives as raw JSON string from the serializer
            var timeState = ParseState<TimeState>(state);

            if (timeState == null)
            {
                return;
            }

            currentHour = timeState.currentHour;
            currentDay = timeState.currentDay;
            currentPeriod = EvaluatePeriod(currentHour);
            secondsAccumulator = 0f;
        }

        /// <summary>
        /// Serializable time state. Only what's needed to reconstruct.
        /// </summary>
        [Serializable]
        private sealed class TimeState
        {
            public float currentHour;
            public int currentDay;
        }

        private void Awake()
        {
            ServiceLocator.Register<TimeSystem>(this);
        }

        private void Start()
        {
            currentHour = timeDefinition.StartingHour;
            currentDay = 1;
            currentPeriod = EvaluatePeriod(currentHour);

            // Register with save system if available
            if (ServiceLocator.TryGet<SaveManager>(out var saveManager))
            {
                saveManager.Register(this);
            }
        }

        private void Update()
        {
            if (isPaused)
            {
                return;
            }

            TickTime(UnityEngine.Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<SaveManager>(out var saveManager))
            {
                saveManager.Unregister(this);
            }

            ServiceLocator.Unregister<TimeSystem>();
        }

        /// <summary>
        /// Advance time by a number of in-game hours instantly.
        /// Use for rest, fast travel, or narrative time skips.
        /// Fires all appropriate tick and period events.
        /// </summary>
        public void AdvanceHours(float hours)
        {
            if (hours <= 0f)
            {
                return;
            }

            float remainingHours = hours;

            while (remainingHours > 0f)
            {
                // Advance in 1-hour increments to fire per-hour events correctly
                float step = Mathf.Min(remainingHours, 1f);
                ApplyHourAdvance(step);
                remainingHours -= step;
            }
        }

        /// <summary>
        /// Get a formatted time string (e.g., "14:30" or "Day 3, 14:30").
        /// </summary>
        public string GetFormattedTime(bool includeDay = false)
        {
            int displayHour = Mathf.FloorToInt(currentHour);
            int displayMinute = Mathf.FloorToInt((currentHour - displayHour) * 60f);

            string time = $"{displayHour:D2}:{displayMinute:D2}";
            return includeDay ? $"Day {currentDay}, {time}" : time;
        }

        /// <summary>
        /// Get the display name of the current time period.
        /// </summary>
        public string GetPeriodDisplayName()
        {
            var periods = timeDefinition.Periods;

            if (periods == null || periods.Length == 0)
            {
                return currentPeriod.ToString();
            }

            for (int i = 0; i < periods.Length; i++)
            {
                if (periods[i].period == currentPeriod)
                {
                    return string.IsNullOrEmpty(periods[i].displayName)
                        ? currentPeriod.ToString()
                        : periods[i].displayName;
                }
            }

            return currentPeriod.ToString();
        }

        private void TickTime(float realDeltaTime)
        {
            secondsAccumulator += realDeltaTime;
            float secondsPerHour = timeDefinition.SecondsPerHour;

            if (secondsAccumulator < secondsPerHour)
            {
                // Sub-hour update — advance the fractional hour for smooth interpolation
                float fractionalHours = secondsAccumulator / secondsPerHour;
                float previousHour = currentHour;
                currentHour += fractionalHours;
                secondsAccumulator = 0f;

                // Wrap day
                if (currentHour >= timeDefinition.HoursPerDay)
                {
                    currentHour -= timeDefinition.HoursPerDay;
                    currentDay++;
                }

                // Check period transition
                CheckPeriodTransition();
                return;
            }

            // Full hour(s) elapsed — fire tick events
            while (secondsAccumulator >= secondsPerHour)
            {
                secondsAccumulator -= secondsPerHour;
                ApplyHourAdvance(1f);
            }

            // Apply remaining fractional time
            if (secondsAccumulator > 0f)
            {
                float fractionalHours = secondsAccumulator / secondsPerHour;
                currentHour += fractionalHours;
                secondsAccumulator = 0f;

                if (currentHour >= timeDefinition.HoursPerDay)
                {
                    currentHour -= timeDefinition.HoursPerDay;
                    currentDay++;
                }
            }
        }

        private void ApplyHourAdvance(float hours)
        {
            float previousHour = currentHour;
            currentHour += hours;

            // Wrap day
            if (currentHour >= timeDefinition.HoursPerDay)
            {
                currentHour -= timeDefinition.HoursPerDay;
                currentDay++;
            }

            EventBus.Publish(new TimeTickEvent
            {
                CurrentHour = currentHour,
                CurrentDay = currentDay,
                DeltaHours = hours
            });

            CheckPeriodTransition();
        }

        private void CheckPeriodTransition()
        {
            var newPeriod = EvaluatePeriod(currentHour);

            if (newPeriod == currentPeriod)
            {
                return;
            }

            var previousPeriod = currentPeriod;
            currentPeriod = newPeriod;

            EventBus.Publish(new TimePeriodChangedEvent
            {
                PreviousPeriod = previousPeriod,
                NewPeriod = currentPeriod,
                CurrentHour = currentHour,
                CurrentDay = currentDay
            });
        }

        private TimePeriod EvaluatePeriod(float hour)
        {
            var periods = timeDefinition.Periods;

            if (periods == null || periods.Length == 0)
            {
                return TimePeriod.Day;
            }

            // Periods are defined by their start hour.
            // Find the last period whose startHour <= current hour.
            TimePeriod result = periods[0].period;

            for (int i = 0; i < periods.Length; i++)
            {
                if (hour >= periods[i].startHour)
                {
                    result = periods[i].period;
                }
            }

            return result;
        }

        /// <summary>
        /// Helper to deserialize state from either a typed object or raw JSON string.
        /// Handles both cases: direct object (in-memory) and deserialized JSON (from disk).
        /// </summary>
        private static T ParseState<T>(object state) where T : class
        {
            if (state is T typed)
            {
                return typed;
            }

            if (state is string json)
            {
                try
                {
                    return JsonUtility.FromJson<T>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TimeSystem] Failed to parse save state: {e.Message}");
                }
            }

            return null;
        }
    }
}