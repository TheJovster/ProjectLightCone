using System;
using System.Collections.Generic;

namespace LightCone.Systems.Stats
{
    /// <summary>
    /// A single modifiable attribute (e.g., Strength, MaxHealth, AttackSpeed).
    /// Holds a base value and a sorted list of modifiers.
    /// Caches the final value and recalculates only when dirty.
    /// </summary>
    public sealed class ModifiableAttribute
    {
        private float baseValue;
        private float cachedValue;
        private bool isDirty = true;
        private readonly List<StatModifier> modifiers = new();

        /// <summary>
        /// Fires when the effective value changes. Passes (oldValue, newValue).
        /// </summary>
        public event Action<float, float> OnValueChanged;

        public float BaseValue
        {
            get => baseValue;
            set
            {
                if (Math.Abs(baseValue - value) < float.Epsilon)
                {
                    return;
                }

                baseValue = value;
                SetDirty();
            }
        }

        /// <summary>
        /// The final calculated value after all modifiers.
        /// </summary>
        public float Value
        {
            get
            {
                if (isDirty)
                {
                    Recalculate();
                }

                return cachedValue;
            }
        }

        public ModifiableAttribute(float baseValue = 0f)
        {
            this.baseValue = baseValue;
            cachedValue = baseValue;
        }

        /// <summary>
        /// Add a modifier. The modifier list is re-sorted by Type then Order.
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            modifiers.Add(modifier);
            modifiers.Sort(CompareModifiers);
            SetDirty();
        }

        /// <summary>
        /// Remove a specific modifier instance.
        /// </summary>
        public bool RemoveModifier(StatModifier modifier)
        {
            // Struct comparison — find by matching all fields.
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var m = modifiers[i];
                if (Math.Abs(m.Value - modifier.Value) < float.Epsilon
                    && m.Type == modifier.Type
                    && m.Source == modifier.Source)
                {
                    modifiers.RemoveAt(i);
                    SetDirty();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove all modifiers from a given source.
        /// Use when unequipping gear or removing a status effect.
        /// </summary>
        public int RemoveModifiersFromSource(object source)
        {
            int removed = 0;

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].Source == source)
                {
                    modifiers.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                SetDirty();
            }

            return removed;
        }

        /// <summary>
        /// Remove all modifiers. Does not change base value.
        /// </summary>
        public void ClearModifiers()
        {
            if (modifiers.Count == 0)
            {
                return;
            }

            modifiers.Clear();
            SetDirty();
        }

        private void SetDirty()
        {
            float oldValue = cachedValue;
            isDirty = true;

            // Force recalculation to fire event if value actually changed.
            float newValue = Value;

            if (Math.Abs(oldValue - newValue) > float.Epsilon)
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }

        /// <summary>
        /// Recalculates the final value using the modifier stack.
        /// Order: Base + Flat → × (1 + sum of PercentAdditive) → × each PercentMultiplicative
        /// </summary>
        private void Recalculate()
        {
            float finalValue = baseValue;
            float percentAdditiveSum = 0f;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];

                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        finalValue += mod.Value;
                        break;

                    case ModifierType.PercentAdditive:
                        percentAdditiveSum += mod.Value;
                        break;

                    case ModifierType.PercentMultiplicative:
                        // Apply accumulated additive first when transitioning
                        if (percentAdditiveSum != 0f)
                        {
                            finalValue *= 1f + percentAdditiveSum;
                            percentAdditiveSum = 0f;
                        }
                        finalValue *= 1f + mod.Value;
                        break;
                }
            }

            // Apply any remaining additive percent
            if (percentAdditiveSum != 0f)
            {
                finalValue *= 1f + percentAdditiveSum;
            }

            cachedValue = finalValue;
            isDirty = false;
        }

        private static int CompareModifiers(StatModifier a, StatModifier b)
        {
            int typeComparison = a.Type.CompareTo(b.Type);
            return typeComparison != 0 ? typeComparison : a.Order.CompareTo(b.Order);
        }
    }
}
