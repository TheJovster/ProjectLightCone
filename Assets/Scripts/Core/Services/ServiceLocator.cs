using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightCone.Core.Services
{
    /// <summary>
    /// Lightweight service locator for decoupled system access.
    /// Services register by interface, consumers resolve by interface.
    /// Not a god-object — each service owns its own logic.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new();

        /// <summary>
        /// Register a service instance against its interface type.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);

            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service for {type.Name}.");
            }

            services[type] = service;
        }

        /// <summary>
        /// Resolve a registered service by interface type.
        /// Returns null and logs error if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);

            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service not found: {type.Name}. Was it registered?");
            return null;
        }

        /// <summary>
        /// Try to resolve a service. Returns false if not registered.
        /// Use this when the service is optional.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            if (services.TryGetValue(type, out var found))
            {
                service = found as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Remove a registered service.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all services. Call on application quit.
        /// </summary>
        public static void Clear()
        {
            services.Clear();
        }
    }
}