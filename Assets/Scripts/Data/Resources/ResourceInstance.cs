using System;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Data.Resources;
using LightCone.Systems.Stats;

namespace LightCone.Systems.Resources
{
    /// <summary>
    /// Runtime instance of a depletable resource.
    /// Manages current value, drain, regeneration, and depletion state.
    /// Reads max and modifier values from the entity's AttributeSet.
    /// </summary>
    public sealed class ResourceInstance
    {
        private readonly ResourceDefinitionSO definition;
        private readonly AttributeSet attributeSet;
        private float currentValue;
        private float regenCooldown;
        private bool isDepleted;
        private bool isActive = true;

        /// <summary>
        /// Fires when the current value changes. Passes (resourceId, oldValue, newValue, maxValue).
        /// </summary>
        public event Action<string, float, float, float> OnValueChanged;

        /// <summary>
        /// Fires when this resource becomes depleted or recovers from depletion.
        /// Passes (resourceId, isDepleted).
        /// </summary>
        public event Action<string, bool> OnDepletionChanged;

        public string ResourceId => definition.ResourceId;
        public string DisplayName => definition.DisplayName;
        public float CurrentValue => currentValue;
        public float MaxValue => GetMaxValue();
        public float NormalizedValue => MaxValue > 0f ? currentValue / MaxValue : 0f;
        public bool IsDepleted => isDepleted;
        public bool IsActive { get => isActive; set => isActive = value; }

        public ResourceInstance(ResourceDefinitionSO definition, AttributeSet attributeSet)
        {
            this.definition = definition;
            this.attributeSet = attributeSet;

            float max = GetMaxValue();
            currentValue = max * definition.StartingPercent;
        }

        /// <summary>
        /// Tick drain and regen logic. Call once per frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TickDrain(deltaTime);
            TickRegen(deltaTime);
        }

        /// <summary>
        /// Consume a flat amount of this resource (e.g., stamina cost for an attack).
        /// Returns the amount actually consumed.
        /// </summary>
        public float Consume(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float oldValue = currentValue;
            float consumed = Mathf.Min(amount, currentValue);
            SetValue(currentValue - consumed);

            // Reset regen cooldown on consumption
            regenCooldown = definition.RegenDelay;

            return consumed;
        }

        /// <summary>
        /// Restore a flat amount of this resource (e.g., potion, rest recovery).
        /// Returns the amount actually restored.
        /// </summary>
        public float Restore(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float max = GetMaxValue();
            float restored = Mathf.Min(amount, max - currentValue);
            SetValue(currentValue + restored);

            return restored;
        }

        /// <summary>
        /// Set the current value directly. Clamps to [0, max].
        /// Use sparingly — prefer Consume/Restore for gameplay actions.
        /// </summary>
        public void SetValueDirect(float value)
        {
            SetValue(value);
        }

        /// <summary>
        /// Reset to full capacity.
        /// </summary>
        public void Fill()
        {
            SetValue(GetMaxValue());
        }

        /// <summary>
        /// Check if the resource can afford a cost.
        /// </summary>
        public bool CanAfford(float cost)
        {
            return currentValue >= cost;
        }

        private void TickDrain(float deltaTime)
        {
            if (definition.DrainMode == DrainMode.Manual)
            {
                return;
            }

            if (definition.DrainMode == DrainMode.ConditionalContinuous && !isActive)
            {
                return;
            }

            float drainRate = CalculateDrainRate();
            float drainAmount = drainRate * deltaTime;

            if (drainAmount > 0f)
            {
                SetValue(currentValue - drainAmount);
                regenCooldown = definition.RegenDelay;
            }
        }

        private void TickRegen(float deltaTime)
        {
            if (definition.RegenMode == RegenMode.None)
            {
                return;
            }

            if (definition.RegenMode == RegenMode.Conditional && isDepleted)
            {
                return;
            }

            // Wait for regen cooldown
            if (regenCooldown > 0f)
            {
                regenCooldown -= deltaTime;
                return;
            }

            float max = GetMaxValue();

            if (currentValue >= max)
            {
                return;
            }

            float regenRate = CalculateRegenRate();
            float regenAmount = regenRate * deltaTime;

            if (regenAmount > 0f)
            {
                SetValue(currentValue + regenAmount);
            }
        }

        private float CalculateDrainRate()
        {
            float rate = definition.BaseDrainRate;

            float modifierValue = attributeSet.GetValue(definition.DrainModifierAttribute);

            if (modifierValue > 0f)
            {
                if (definition.DrainModifierReducesDrain)
                {
                    // Higher attribute = slower drain. e.g., Perception 20 = drain * (1 / (1 + 20*0.01)) = ~83% drain
                    rate /= 1f + modifierValue * 0.01f;
                }
                else
                {
                    // Higher attribute = faster drain
                    rate *= 1f + modifierValue * 0.01f;
                }
            }

            return rate;
        }

        private float CalculateRegenRate()
        {
            float rate = definition.BaseRegenRate;

            float modifierValue = attributeSet.GetValue(definition.RegenModifierAttribute);

            if (modifierValue > 0f)
            {
                // Higher attribute = faster regen
                rate *= 1f + modifierValue * 0.01f;
            }

            return rate;
        }

        private float GetMaxValue()
        {
            float attributeMax = attributeSet.GetValue(definition.MaxAttribute);
            return attributeMax > 0f ? attributeMax : definition.DefaultMax;
        }

        private void SetValue(float newValue)
        {
            float max = GetMaxValue();
            float clampedValue = Mathf.Clamp(newValue, 0f, max);

            if (Mathf.Approximately(clampedValue, currentValue))
            {
                return;
            }

            float oldValue = currentValue;
            currentValue = clampedValue;

            OnValueChanged?.Invoke(definition.ResourceId, oldValue, currentValue, max);

            // Check depletion transitions
            bool nowDepleted = currentValue <= 0f;

            if (nowDepleted != isDepleted)
            {
                isDepleted = nowDepleted;
                OnDepletionChanged?.Invoke(definition.ResourceId, isDepleted);
            }
        }
    }
}