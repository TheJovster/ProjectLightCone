namespace LightCone.Systems.Stats
{
    /// <summary>
    /// How a modifier stacks with other modifiers.
    /// Order matters: Flat is applied first, then PercentAdditive, then PercentMultiplicative.
    /// </summary>
    public enum ModifierType
    {
        /// <summary>Added directly to base value. Example: +10 Strength from equipment.</summary>
        Flat = 100,

        /// <summary>Additive percentage of base. Multiple stack additively. Example: +20% from buff A and +10% from buff B = +30% total.</summary>
        PercentAdditive = 200,

        /// <summary>Multiplicative percentage. Each compounds. Example: ×1.1 then ×0.9 = ×0.99.</summary>
        PercentMultiplicative = 300
    }

    /// <summary>
    /// A single stat modifier. Immutable after creation.
    /// The Source field allows removal of all modifiers from a given source (e.g., an equipment piece or status effect).
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly float Value;
        public readonly ModifierType Type;
        public readonly int Order;
        public readonly object Source;

        /// <param name="value">The modifier value. For Flat: raw amount. For Percent: 0.1 = +10%.</param>
        /// <param name="type">How this modifier stacks.</param>
        /// <param name="order">Sort order within the same ModifierType. Lower = applied first.</param>
        /// <param name="source">The object that applied this modifier (equipment, status effect, etc.).</param>
        public StatModifier(float value, ModifierType type, int order = 0, object source = null)
        {
            Value = value;
            Type = type;
            Order = order;
            Source = source;
        }

        /// <summary>Convenience constructor without explicit order (defaults to ModifierType's enum value).</summary>
        public StatModifier(float value, ModifierType type, object source)
            : this(value, type, (int)type, source) { }
    }
}