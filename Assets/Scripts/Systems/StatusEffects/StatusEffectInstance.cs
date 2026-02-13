using LightCone.Core.Events;
using LightCone.Data.StatusEffects;
using LightCone.Systems.Stats;

namespace LightCone.Systems.StatusEffects
{
    /// <summary>
    /// Runtime instance of a status effect applied to an entity.
    /// Manages its own lifetime and modifier application/removal.
    /// </summary>
    public sealed class StatusEffectInstance
    {
        private readonly StatusEffectDefinitionSO definition;
        private readonly AttributeSet target;
        private readonly StatModifier[] appliedModifiers;
        private float remainingDuration;
        private int currentStacks;
        private bool isExpired;

        public string EffectId => definition.EffectId;
        public string DisplayName => definition.DisplayName;
        public float RemainingDuration => remainingDuration;
        public float NormalizedDuration => definition.IsPermanent ? 1f : remainingDuration / definition.Duration;
        public int CurrentStacks => currentStacks;
        public bool IsExpired => isExpired;
        public bool IsPermanent => definition.IsPermanent;

        public StatusEffectInstance(StatusEffectDefinitionSO definition, AttributeSet target)
        {
            this.definition = definition;
            this.target = target;
            remainingDuration = definition.Duration;
            currentStacks = 1;

            appliedModifiers = new StatModifier[definition.Modifiers.Length];
            ApplyModifiers();
        }

        /// <summary>
        /// Tick the effect. Returns true if still active, false if expired.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (isExpired)
            {
                return false;
            }

            if (definition.IsPermanent)
            {
                return true;
            }

            remainingDuration -= deltaTime;

            if (remainingDuration <= 0f)
            {
                Expire();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Refresh the duration (e.g., when re-applying the same effect).
        /// </summary>
        public void Refresh()
        {
            remainingDuration = definition.Duration;
        }

        /// <summary>
        /// Add a stack if allowed. Returns true if stack was added.
        /// </summary>
        public bool AddStack()
        {
            if (!definition.IsStackable || currentStacks >= definition.MaxStacks)
            {
                return false;
            }

            currentStacks++;
            RemoveModifiers();
            ApplyModifiers();
            return true;
        }

        /// <summary>
        /// Force-remove this effect.
        /// </summary>
        public void Expire()
        {
            if (isExpired)
            {
                return;
            }

            isExpired = true;
            RemoveModifiers();
        }

        private void ApplyModifiers()
        {
            var entries = definition.Modifiers;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                float scaledValue = entry.value * currentStacks;

                var modifier = new StatModifier(scaledValue, entry.modifierType, source: this);
                appliedModifiers[i] = modifier;
                target.AddModifier(entry.attribute, modifier);
            }
        }

        private void RemoveModifiers()
        {
            target.RemoveAllModifiersFromSource(this);
        }
    }
}