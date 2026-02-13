using System;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Systems.Stats;

namespace LightCone.Data.StatusEffects
{
    /// <summary>
    /// ScriptableObject defining a status effect's behavior and modifiers.
    /// Examples: Exhaustion, Hunger Debuff, Torch Buff, Potion Effect.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "LightCone/Data/Status Effect")]
    public sealed class StatusEffectDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string effectId;
        [SerializeField] private string displayName;
        [SerializeField][TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Timing")]
        [SerializeField] private float duration = 10f;
        [SerializeField] private bool isPermanent;
        [SerializeField] private bool isStackable;
        [SerializeField] private int maxStacks = 1;

        [Header("Modifiers")]
        [SerializeField] private AttributeModifierEntry[] modifiers;

        public string EffectId => effectId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float Duration => duration;
        public bool IsPermanent => isPermanent;
        public bool IsStackable => isStackable;
        public int MaxStacks => maxStacks;
        public AttributeModifierEntry[] Modifiers => modifiers;

        /// <summary>
        /// Serializable entry linking an attribute to a modifier.
        /// </summary>
        [Serializable]
        public struct AttributeModifierEntry
        {
            public AttributeType attribute;
            public float value;
            public ModifierType modifierType;
        }
    }
}