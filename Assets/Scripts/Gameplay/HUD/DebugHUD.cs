using LightCone.Core.Events;
using UnityEditor.Rendering;
using UnityEngine;

namespace LightCone.Gameplay.UI
{
    /// <summary>
    /// Temporary debug HUD. Displays resource and attribute values on screen.
    /// Listens to EventBus — no player reference needed.
    /// Delete this when you build the real HUD.
    /// </summary>
    public sealed class DebugHUD : MonoBehaviour
    {
        private float stamina;
        private float staminaMax;
        private float health;
        private float healthMax;

        private GUIStyle debugStyle;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<AttributeChangedEvent>(OnAttributeChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<AttributeChangedEvent>(OnAttributeChanged);
        }

        private void OnResourceChanged(ResourceChangedEvent e)
        {
            switch (e.ResourceId)
            {
                case "stamina":
                    stamina = e.NewValue;
                    staminaMax = e.MaxValue;
                    break;
            }
        }

        private void OnAttributeChanged(AttributeChangedEvent e)
        {
            switch (e.Attribute)
            {
                case AttributeType.Health:
                    health = e.NewValue;
                    break;
                case AttributeType.MaxHealth:
                    healthMax = e.NewValue;
                    break;
            }
        }

        private void OnGUI()
        {
            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22
                };
            }

            float y = 10f;
            float lineHeight = 30f;

            GUI.color = Color.white;
            DrawLabel(ref y, lineHeight, $"Stamina: {stamina:F0} / {staminaMax:F0}");
            DrawLabel(ref y, lineHeight, $"Health: {health:F0} / {healthMax:F0}");
        }

        private void DrawLabel(ref float y, float height, string text)
        {
            GUI.Label(new Rect(10f, y, 400f, height), text, debugStyle);
            y += height;
        }
    }
}
