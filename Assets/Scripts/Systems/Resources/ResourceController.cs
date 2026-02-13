using System.Collections.Generic;
using System;
using UnityEngine;
using LightCone.Core.Events;
using LightCone.Data.Resources;
using LightCone.Systems.Stats;
using LightCone.Systems.StatusEffects;

namespace LightCone.Systems.Resources
{
    /// <summary>
    /// Manages all depletable resources on an entity.
    /// Requires AttributeSet and StatusEffectController on the same GameObject.
    /// Ticks resources each frame, applies depletion effects via the status effect system.
    /// </summary>
    [RequireComponent(typeof(AttributeSet))]
    [RequireComponent(typeof(StatusEffectController))]
    public sealed class ResourceController : MonoBehaviour
    {
        [SerializeField] private ResourceDefinitionSO[] resourceDefinitions;

        private AttributeSet attributeSet;
        private StatusEffectController statusEffectController;
        private readonly Dictionary<string, ResourceInstance> resources = new();

        private void Awake()
        {
            attributeSet = GetComponent<AttributeSet>();
            statusEffectController = GetComponent<StatusEffectController>();
            InitializeResources();
        }

        private void Update()
        {
            TickResources(UnityEngine.Time.deltaTime);
        }

        private void OnDestroy()
        {
            foreach (var kvp in resources)
            {
                kvp.Value.OnValueChanged -= HandleResourceValueChanged;
                kvp.Value.OnDepletionChanged -= HandleDepletionChanged;
            }
        }

        /// <summary>
        /// Get a resource instance by ID. Returns null if not found.
        /// </summary>
        public ResourceInstance GetResource(string resourceId)
        {
            return resources.TryGetValue(resourceId, out var resource) ? resource : null;
        }

        /// <summary>
        /// Get the current value of a resource. Returns -1 if not found.
        /// </summary>
        public float GetValue(string resourceId)
        {
            return resources.TryGetValue(resourceId, out var resource) ? resource.CurrentValue : -1f;
        }

        /// <summary>
        /// Get the normalized (0-1) value of a resource. Returns -1 if not found.
        /// </summary>
        public float GetNormalizedValue(string resourceId)
        {
            return resources.TryGetValue(resourceId, out var resource) ? resource.NormalizedValue : -1f;
        }

        /// <summary>
        /// Consume a flat amount of a resource. Returns the amount actually consumed.
        /// </summary>
        public float Consume(string resourceId, float amount)
        {
            if (!resources.TryGetValue(resourceId, out var resource))
            {
                Debug.LogWarning($"[ResourceController] Resource not found: {resourceId}");
                return 0f;
            }

            return resource.Consume(amount);
        }

        /// <summary>
        /// Restore a flat amount of a resource. Returns the amount actually restored.
        /// </summary>
        public float Restore(string resourceId, float amount)
        {
            if (!resources.TryGetValue(resourceId, out var resource))
            {
                Debug.LogWarning($"[ResourceController] Resource not found: {resourceId}");
                return 0f;
            }

            return resource.Restore(amount);
        }

        /// <summary>
        /// Check if a resource can afford a cost.
        /// </summary>
        public bool CanAfford(string resourceId, float cost)
        {
            return resources.TryGetValue(resourceId, out var resource) && resource.CanAfford(cost);
        }

        /// <summary>
        /// Check if a resource is depleted.
        /// </summary>
        public bool IsDepleted(string resourceId)
        {
            return resources.TryGetValue(resourceId, out var resource) && resource.IsDepleted;
        }

        /// <summary>
        /// Set a resource's active state (for ConditionalContinuous drain, e.g., torch lit/unlit).
        /// </summary>
        public void SetResourceActive(string resourceId, bool active)
        {
            if (resources.TryGetValue(resourceId, out var resource))
            {
                resource.IsActive = active;
            }
        }

        /// <summary>
        /// Check if a resource exists on this entity.
        /// </summary>
        public bool HasResource(string resourceId)
        {
            return resources.ContainsKey(resourceId);
        }

        private void InitializeResources()
        {
            if (resourceDefinitions == null)
            {
                return;
            }

            foreach (var definition in resourceDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (resources.ContainsKey(definition.ResourceId))
                {
                    Debug.LogWarning($"[ResourceController] Duplicate resource ID: {definition.ResourceId}");
                    continue;
                }

                var instance = new ResourceInstance(definition, attributeSet);
                instance.OnValueChanged += HandleResourceValueChanged;
                instance.OnDepletionChanged += HandleDepletionChanged;
                resources[definition.ResourceId] = instance;
            }
        }

        private void TickResources(float deltaTime)
        {
            foreach (var kvp in resources)
            {
                kvp.Value.Tick(deltaTime);
            }
        }

        private void HandleResourceValueChanged(string resourceId, float oldValue, float newValue, float maxValue)
        {
            EventBus.Publish(new ResourceChangedEvent
            {
                EntityId = attributeSet.EntityId,
                ResourceId = resourceId,
                OldValue = oldValue,
                NewValue = newValue,
                MaxValue = maxValue
            });
        }

        private void HandleDepletionChanged(string resourceId, bool depleted)
        {
            EventBus.Publish(new ResourceDepletedEvent
            {
                EntityId = attributeSet.EntityId,
                ResourceId = resourceId,
                IsDepleted = depleted
            });

            ApplyDepletionEffects(resourceId, depleted);
        }

        private void ApplyDepletionEffects(string resourceId, bool depleted)
        {
            // Find the definition to get depletion effects
            ResourceDefinitionSO definition = null;

            foreach (var def in resourceDefinitions)
            {
                if (def != null && def.ResourceId == resourceId)
                {
                    definition = def;
                    break;
                }
            }

            if (definition?.DepletionEffects == null)
            {
                return;
            }

            foreach (var depletionEffect in definition.DepletionEffects)
            {
                if (depletionEffect.statusEffect == null)
                {
                    continue;
                }

                if (depleted)
                {
                    statusEffectController.ApplyEffect(depletionEffect.statusEffect);
                }
                else if (depletionEffect.removeOnRecovery)
                {
                    statusEffectController.RemoveEffect(depletionEffect.statusEffect.EffectId);
                }
            }
        }
    }
}