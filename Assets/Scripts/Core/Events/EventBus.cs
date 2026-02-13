using System;
using System.Collections.Generic;

namespace LightCone.Core.Events
{
    /// <summary>
    /// Marker interface for all game events.
    /// Implement this on structs to avoid allocation.
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Type-safe event bus for decoupled system communication.
    /// Uses structs as event types to minimize GC pressure.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> listeners = new();

        /// <summary>
        /// Subscribe to an event type. Returns an unsubscribe action for cleanup.
        /// </summary>
        public static Action Subscribe<T>(Action<T> callback) where T : struct, IGameEvent
        {
            var type = typeof(T);

            if (listeners.TryGetValue(type, out var existing))
            {
                listeners[type] = Delegate.Combine(existing, callback);
            }
            else
            {
                listeners[type] = callback;
            }

            return () => Unsubscribe(callback);
        }

        /// <summary>
        /// Unsubscribe a specific callback from an event type.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : struct, IGameEvent
        {
            var type = typeof(T);

            if (!listeners.TryGetValue(type, out var existing))
            {
                return;
            }

            var remaining = Delegate.Remove(existing, callback);

            if (remaining == null)
            {
                listeners.Remove(type);
            }
            else
            {
                listeners[type] = remaining;
            }
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        public static void Publish<T>(T gameEvent) where T : struct, IGameEvent
        {
            var type = typeof(T);

            if (!listeners.TryGetValue(type, out var existing))
            {
                return;
            }

            // Cast once, invoke. This avoids GetInvocationList allocation
            // for the common case of a single subscriber.
            if (existing is Action<T> typedCallback)
            {
                typedCallback.Invoke(gameEvent);
            }
        }

        /// <summary>
        /// Remove all listeners. Call on scene teardown or application quit.
        /// </summary>
        public static void Clear()
        {
            listeners.Clear();
        }

        /// <summary>
        /// Remove all listeners for a specific event type.
        /// </summary>
        public static void Clear<T>() where T : struct, IGameEvent
        {
            listeners.Remove(typeof(T));
        }
    }
}