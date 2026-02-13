using System;
using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Data.Resources
{
    /// <summary>
    /// How this resource drains over time.
    /// </summary>
    public enum DrainMode
    {
        /// <summary>No automatic drain. Only consumed by explicit actions (e.g., stamina on attack).</summary>
        Manual,

        /// <summary>Drains continuously while active (e.g., torch burning, rations over time).</summary>
        Continuous,

        /// <summary>Drains continuously but only when an activation condition is met (e.g., torch only when lit).</summary>
        ConditionalContinuous
    }

    /// <summary>
    /// How this resource recovers.
    /// </summary>
    public enum RegenMode
    {
        /// <summary>No natural regeneration. Recovery only through explicit actions (e.g., rations).</summary>
        None,

        /// <summary>Regenerates continuously over time (e.g., stamina).</summary>
        Continuous,

        /// <summary>Regenerates only when a condition is met (e.g., stamina only when not exhausted).</summary>
        Conditional
    }

    /// <summary>
    /// ScriptableObject defining a depletable resource's behavior.
    /// Examples: Stamina, Rations, Torch Light.
    /// All tuning lives here — code just reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "New Resource", menuName = "LightCone/Data/Resource Definition")]
    public sealed class ResourceDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string resourceId;
        [SerializeField] private string displayName;
        [SerializeField][TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Capacity")]
        [Tooltip("The attribute that determines this resource's maximum value.")]
        [SerializeField] private AttributeType maxAttribute;
        [Tooltip("Fallback max if the entity has no maxAttribute defined.")]
        [SerializeField] private float defaultMax = 100f;
        [Tooltip("Normalized starting value (0-1). 1 = start full, 0.5 = start half.")]
        [SerializeField][Range(0f, 1f)] private float startingPercent = 1f;

        [Header("Drain")]
        [SerializeField] private DrainMode drainMode = DrainMode.Manual;
        [Tooltip("Base drain per second for Continuous/ConditionalContinuous modes.")]
        [SerializeField] private float baseDrainRate = 1f;
        [Tooltip("Optional attribute that modifies drain rate. Higher value = slower drain. Set to the same as maxAttribute if unused.")]
        [SerializeField] private AttributeType drainModifierAttribute;
        [Tooltip("If true, the drainModifierAttribute reduces drain rate. If false, it increases it.")]
        [SerializeField] private bool drainModifierReducesDrain = true;

        [Header("Regeneration")]
        [SerializeField] private RegenMode regenMode = RegenMode.None;
        [Tooltip("Base regen per second.")]
        [SerializeField] private float baseRegenRate;
        [Tooltip("Optional attribute that modifies regen rate (e.g., StaminaRegenRate).")]
        [SerializeField] private AttributeType regenModifierAttribute;
        [Tooltip("Delay in seconds after consumption before regen resumes.")]
        [SerializeField] private float regenDelay;

        [Header("Depletion")]
        [Tooltip("Status effects to apply when this resource hits zero.")]
        [SerializeField] private DepletionEffect[] depletionEffects;

        // ── Public API ──────────────────────────────────────────────

        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public AttributeType MaxAttribute => maxAttribute;
        public float DefaultMax => defaultMax;
        public float StartingPercent => startingPercent;

        public DrainMode DrainMode => drainMode;
        public float BaseDrainRate => baseDrainRate;
        public AttributeType DrainModifierAttribute => drainModifierAttribute;
        public bool DrainModifierReducesDrain => drainModifierReducesDrain;

        public RegenMode RegenMode => regenMode;
        public float BaseRegenRate => baseRegenRate;
        public AttributeType RegenModifierAttribute => regenModifierAttribute;
        public float RegenDelay => regenDelay;

        public DepletionEffect[] DepletionEffects => depletionEffects;

        /// <summary>
        /// Links a status effect to apply/remove based on depletion state.
        /// </summary>
        [Serializable]
        public struct DepletionEffect
        {
            [Tooltip("Reference to the StatusEffectDefinitionSO to apply on depletion.")]
            public StatusEffects.StatusEffectDefinitionSO statusEffect;
            [Tooltip("If true, effect is removed when resource recovers above zero.")]
            public bool removeOnRecovery;
        }
    }
}