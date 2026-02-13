using System.Collections.Generic;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Data.StatusEffects;
using LightCone.Systems.Stats;

namespace LightCone.Systems.StatusEffects
{
    /// <summary>
    /// Manages all active status effects on an entity.
    /// Requires an AttributeSet on the same GameObject.
    /// Ticks effects each frame and handles application, stacking, and removal.
    /// </summary>
    [RequireComponent(typeof(AttributeSet))]
    public sealed class StatusEffectController : MonoBehaviour
    {
        private AttributeSet attributeSet;
        private readonly List<StatusEffectInstance> activeEffects = new();
        private readonly List<StatusEffectInstance> expiredBuffer = new();

        private void Awake()
        {
            attributeSet = GetComponent<AttributeSet>();
        }

        private void Update()
        {
            TickEffects(UnityEngine.Time.deltaTime);
        }

        /// <summary>
        /// Apply a status effect to this entity.
        /// Handles refresh and stacking logic automatically.
        /// </summary>
        public void ApplyEffect(StatusEffectDefinitionSO definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("[StatusEffectController] Null effect definition.");
                return;
            }

            var existing = FindEffect(definition.EffectId);

            if (existing != null && !existing.IsExpired)
            {
                if (definition.IsStackable)
                {
                    existing.AddStack();
                }

                existing.Refresh();
                return;
            }

            var instance = new StatusEffectInstance(definition, attributeSet);
            activeEffects.Add(instance);

            EventBus.Publish(new StatusEffectAppliedEvent
            {
                EntityId = attributeSet.EntityId,
                EffectId = definition.EffectId
            });
        }

        /// <summary>
        /// Remove a status effect by ID.
        /// </summary>
        public bool RemoveEffect(string effectId)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].EffectId != effectId)
                {
                    continue;
                }

                var effect = activeEffects[i];
                effect.Expire();
                activeEffects.RemoveAt(i);

                EventBus.Publish(new StatusEffectRemovedEvent
                {
                    EntityId = attributeSet.EntityId,
                    EffectId = effectId
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove all active status effects.
        /// </summary>
        public void RemoveAllEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].Expire();

                EventBus.Publish(new StatusEffectRemovedEvent
                {
                    EntityId = attributeSet.EntityId,
                    EffectId = activeEffects[i].EffectId
                });
            }

            activeEffects.Clear();
        }

        /// <summary>
        /// Check if a specific effect is currently active.
        /// </summary>
        public bool HasEffect(string effectId)
        {
            return FindEffect(effectId) != null;
        }

        /// <summary>
        /// Get the remaining duration of an effect. Returns -1 if not found.
        /// </summary>
        public float GetRemainingDuration(string effectId)
        {
            var effect = FindEffect(effectId);
            return effect?.RemainingDuration ?? -1f;
        }

        /// <summary>
        /// Get count of all active effects.
        /// </summary>
        public int ActiveEffectCount => activeEffects.Count;

        private void TickEffects(float deltaTime)
        {
            expiredBuffer.Clear();

            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (!activeEffects[i].Tick(deltaTime))
                {
                    expiredBuffer.Add(activeEffects[i]);
                }
            }

            for (int i = 0; i < expiredBuffer.Count; i++)
            {
                var expired = expiredBuffer[i];
                activeEffects.Remove(expired);

                EventBus.Publish(new StatusEffectRemovedEvent
                {
                    EntityId = attributeSet.EntityId,
                    EffectId = expired.EffectId
                });
            }
        }

        private StatusEffectInstance FindEffect(string effectId)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].EffectId == effectId && !activeEffects[i].IsExpired)
                {
                    return activeEffects[i];
                }
            }

            return null;
        }
    }
}