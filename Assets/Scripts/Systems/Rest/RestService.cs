using UnityEngine;
using LightCone.Core.Events;
using LightCone.Core.Services;
using LightCone.Data.Rest;
using LightCone.Systems.Resources;
using LightCone.Systems.Time;

namespace LightCone.Systems.Rest
{
    /// <summary>
    /// Handles rest logic: previewing costs, executing rest, advancing time.
    /// Stateless — call Preview to see what a rest will cost, Execute to do it.
    /// The gameplay layer decides when and where to rest; this service handles what happens.
    /// </summary>
    public sealed class RestService
    {
        private readonly RestDefinitionSO definition;

        public float MinimumHours => definition.MinimumHours;
        public float MaximumHours => definition.MaximumHours;

        public RestService(RestDefinitionSO definition)
        {
            this.definition = definition;
        }

        /// <summary>
        /// Preview the outcome of resting for a given duration.
        /// Does NOT modify any state. Use this for UI display.
        /// </summary>
        public RestPreview Preview(ResourceController resources, float hours)
        {
            float clampedHours = Mathf.Clamp(hours, definition.MinimumHours, definition.MaximumHours);
            var preview = new RestPreview(clampedHours);

            // Calculate costs
            var costs = definition.CostsPerHour;

            if (costs != null)
            {
                for (int i = 0; i < costs.Length; i++)
                {
                    var cost = costs[i];
                    float totalCost = cost.amountPerHour * clampedHours;
                    float available = resources.GetValue(cost.resourceId);

                    if (available < 0f)
                    {
                        continue;
                    }

                    float actualCost = Mathf.Min(totalCost, available);
                    float affordableHours = cost.amountPerHour > 0f
                        ? available / cost.amountPerHour
                        : clampedHours;

                    preview.AddCost(cost.resourceId, totalCost, actualCost, affordableHours);
                }
            }

            // Calculate recovery
            var recovery = definition.RecoveryPerHour;

            if (recovery != null)
            {
                for (int i = 0; i < recovery.Length; i++)
                {
                    var rec = recovery[i];
                    var resource = resources.GetResource(rec.resourceId);

                    if (resource == null)
                    {
                        continue;
                    }

                    float perHour = rec.isPercentOfMax
                        ? resource.MaxValue * rec.amountPerHour
                        : rec.amountPerHour;

                    float totalRecovery = perHour * clampedHours;
                    float headroom = resource.MaxValue - resource.CurrentValue;
                    float actualRecovery = Mathf.Min(totalRecovery, headroom);

                    preview.AddRecovery(rec.resourceId, totalRecovery, actualRecovery);
                }
            }

            // Determine effective rest duration (limited by most constrained cost resource)
            preview.FinalizeEffectiveHours(definition.AbortOnResourceDepleted);

            return preview;
        }

        /// <summary>
        /// Execute a rest. Consumes resources, applies recovery, advances time.
        /// Returns the result with actual hours rested.
        /// </summary>
        public RestResult Execute(ResourceController resources, float hours)
        {
            var preview = Preview(resources, hours);
            float effectiveHours = preview.EffectiveHours;

            if (effectiveHours <= 0f)
            {
                return new RestResult(0f, false, "Insufficient resources to rest.");
            }

            // Apply costs
            var costs = definition.CostsPerHour;

            if (costs != null)
            {
                for (int i = 0; i < costs.Length; i++)
                {
                    float totalCost = costs[i].amountPerHour * effectiveHours;
                    resources.Consume(costs[i].resourceId, totalCost);
                }
            }

            // Apply recovery
            var recovery = definition.RecoveryPerHour;

            if (recovery != null)
            {
                for (int i = 0; i < recovery.Length; i++)
                {
                    var rec = recovery[i];
                    var resource = resources.GetResource(rec.resourceId);

                    if (resource == null)
                    {
                        continue;
                    }

                    float perHour = rec.isPercentOfMax
                        ? resource.MaxValue * rec.amountPerHour
                        : rec.amountPerHour;

                    float totalRecovery = perHour * effectiveHours;
                    resources.Restore(rec.resourceId, totalRecovery);
                }
            }

            // Advance time
            if (ServiceLocator.TryGet<TimeSystem>(out var timeSystem))
            {
                timeSystem.AdvanceHours(effectiveHours);
            }

            bool wasShortened = effectiveHours < hours;

            EventBus.Publish(new RestCompletedEvent
            {
                HoursRested = effectiveHours,
                WasInterrupted = false,
                WasShortened = wasShortened
            });

            string reason = wasShortened ? "Rest shortened — a cost resource was depleted." : null;
            return new RestResult(effectiveHours, true, reason);
        }
    }

    /// <summary>
    /// Preview data for a potential rest. Shows costs, recovery, and effective duration.
    /// </summary>
    public sealed class RestPreview
    {
        private readonly float requestedHours;
        private float effectiveHours;

        // Simple parallel arrays — rest will never have dozens of entries
        private string[] costIds;
        private float[] costRequested;
        private float[] costActual;
        private float[] costAffordableHours;
        private int costCount;

        private string[] recoveryIds;
        private float[] recoveryRequested;
        private float[] recoveryActual;
        private int recoveryCount;

        public float RequestedHours => requestedHours;
        public float EffectiveHours => effectiveHours;
        public int CostCount => costCount;
        public int RecoveryCount => recoveryCount;

        public RestPreview(float requestedHours)
        {
            this.requestedHours = requestedHours;
            effectiveHours = requestedHours;

            costIds = new string[4];
            costRequested = new float[4];
            costActual = new float[4];
            costAffordableHours = new float[4];

            recoveryIds = new string[4];
            recoveryRequested = new float[4];
            recoveryActual = new float[4];
        }

        public void AddCost(string resourceId, float requested, float actual, float affordableHours)
        {
            EnsureCostCapacity();
            costIds[costCount] = resourceId;
            costRequested[costCount] = requested;
            costActual[costCount] = actual;
            costAffordableHours[costCount] = affordableHours;
            costCount++;
        }

        public void AddRecovery(string resourceId, float requested, float actual)
        {
            EnsureRecoveryCapacity();
            recoveryIds[recoveryCount] = resourceId;
            recoveryRequested[recoveryCount] = requested;
            recoveryActual[recoveryCount] = actual;
            recoveryCount++;
        }

        /// <summary>
        /// Get cost info by index. Returns (resourceId, requestedAmount, actualAmount).
        /// </summary>
        public (string resourceId, float requested, float actual) GetCost(int index)
        {
            return (costIds[index], costRequested[index], costActual[index]);
        }

        /// <summary>
        /// Get recovery info by index. Returns (resourceId, requestedAmount, actualAmount).
        /// </summary>
        public (string resourceId, float requested, float actual) GetRecovery(int index)
        {
            return (recoveryIds[index], recoveryRequested[index], recoveryActual[index]);
        }

        /// <summary>
        /// Calculate effective hours based on the most constrained cost resource.
        /// </summary>
        public void FinalizeEffectiveHours(bool abortOnDepleted)
        {
            if (!abortOnDepleted)
            {
                effectiveHours = requestedHours;
                return;
            }

            float minAffordable = requestedHours;

            for (int i = 0; i < costCount; i++)
            {
                if (costAffordableHours[i] < minAffordable)
                {
                    minAffordable = costAffordableHours[i];
                }
            }

            effectiveHours = Mathf.Max(minAffordable, 0f);
        }

        private void EnsureCostCapacity()
        {
            if (costCount < costIds.Length)
            {
                return;
            }

            int newSize = costIds.Length * 2;
            System.Array.Resize(ref costIds, newSize);
            System.Array.Resize(ref costRequested, newSize);
            System.Array.Resize(ref costActual, newSize);
            System.Array.Resize(ref costAffordableHours, newSize);
        }

        private void EnsureRecoveryCapacity()
        {
            if (recoveryCount < recoveryIds.Length)
            {
                return;
            }

            int newSize = recoveryIds.Length * 2;
            System.Array.Resize(ref recoveryIds, newSize);
            System.Array.Resize(ref recoveryRequested, newSize);
            System.Array.Resize(ref recoveryActual, newSize);
        }
    }

    /// <summary>
    /// Result of executing a rest action.
    /// </summary>
    public readonly struct RestResult
    {
        public readonly float HoursRested;
        public readonly bool Success;
        public readonly string Message;

        public RestResult(float hoursRested, bool success, string message)
        {
            HoursRested = hoursRested;
            Success = success;
            Message = message;
        }
    }
}