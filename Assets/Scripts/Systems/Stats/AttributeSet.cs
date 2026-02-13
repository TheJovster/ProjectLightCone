using System;
using System.Collections.Generic;
using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Systems.Stats
{
    /// <summary>
    /// Holds all modifiable attributes for an entity (player or enemy).
    /// Attach to any GameObject that participates in the stat system.
    /// Publishes AttributeChangedEvent through the EventBus when values change.
    /// </summary>
    public sealed class AttributeSet : MonoBehaviour
    {
        [SerializeField] private AttributeInitializer[] initialAttributes;

        private readonly Dictionary<AttributeType, ModifiableAttribute> attributes = new();
        private int entityId;

        public int EntityId => entityId;

        private void Awake()
        {
            entityId = gameObject.GetInstanceID();
            InitializeAttributes();
        }

        private void OnDestroy()
        {
            foreach (var kvp in attributes)
            {
                kvp.Value.OnValueChanged -= CreateHandler(kvp.Key);
            }
        }

        /// <summary>
        /// Get the ModifiableAttribute for a given type. Returns null if not present.
        /// </summary>
        public ModifiableAttribute GetAttribute(AttributeType type)
        {
            return attributes.TryGetValue(type, out var attr) ? attr : null;
        }

        /// <summary>
        /// Get the current effective value of an attribute. Returns 0 if not present.
        /// </summary>
        public float GetValue(AttributeType type)
        {
            return attributes.TryGetValue(type, out var attr) ? attr.Value : 0f;
        }

        /// <summary>
        /// Get the base (unmodified) value of an attribute.
        /// </summary>
        public float GetBaseValue(AttributeType type)
        {
            return attributes.TryGetValue(type, out var attr) ? attr.BaseValue : 0f;
        }

        /// <summary>
        /// Set the base value of an attribute directly.
        /// </summary>
        public void SetBaseValue(AttributeType type, float value)
        {
            var attr = GetOrCreateAttribute(type);
            attr.BaseValue = value;
        }

        /// <summary>
        /// Add a modifier to an attribute.
        /// </summary>
        public void AddModifier(AttributeType type, StatModifier modifier)
        {
            var attr = GetOrCreateAttribute(type);
            attr.AddModifier(modifier);
        }

        /// <summary>
        /// Remove a specific modifier from an attribute.
        /// </summary>
        public bool RemoveModifier(AttributeType type, StatModifier modifier)
        {
            if (!attributes.TryGetValue(type, out var attr))
            {
                return false;
            }

            return attr.RemoveModifier(modifier);
        }

        /// <summary>
        /// Remove all modifiers from a given source across ALL attributes.
        /// Use when unequipping gear, removing a status effect, etc.
        /// </summary>
        public int RemoveAllModifiersFromSource(object source)
        {
            int totalRemoved = 0;

            foreach (var kvp in attributes)
            {
                totalRemoved += kvp.Value.RemoveModifiersFromSource(source);
            }

            return totalRemoved;
        }

        /// <summary>
        /// Check if an attribute exists on this entity.
        /// </summary>
        public bool HasAttribute(AttributeType type)
        {
            return attributes.ContainsKey(type);
        }

        private ModifiableAttribute GetOrCreateAttribute(AttributeType type)
        {
            if (attributes.TryGetValue(type, out var existing))
            {
                return existing;
            }

            var attr = new ModifiableAttribute(0f);
            attributes[type] = attr;
            attr.OnValueChanged += CreateHandler(type);
            return attr;
        }

        private void InitializeAttributes()
        {
            if (initialAttributes == null)
            {
                return;
            }

            foreach (var init in initialAttributes)
            {
                var attr = GetOrCreateAttribute(init.type);
                attr.BaseValue = init.baseValue;
            }
        }

        private Action<float, float> CreateHandler(AttributeType type)
        {
            return (oldValue, newValue) =>
            {
                EventBus.Publish(new AttributeChangedEvent
                {
                    EntityId = entityId,
                    Attribute = type,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            };
        }

        /// <summary>
        /// Serializable struct for inspector-based attribute initialization.
        /// </summary>
        [Serializable]
        public struct AttributeInitializer
        {
            public AttributeType type;
            public float baseValue;
        }
    }
}