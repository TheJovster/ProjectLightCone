using UnityEngine;
using LightCone.Core.Events;

namespace LightCone.Data.Attributes
{
    /// <summary>
    /// ScriptableObject defining a single attribute's metadata.
    /// Use for tooltip data, UI display, and editor tooling.
    /// Does NOT hold runtime values — those live on AttributeSet.
    /// </summary>
    [CreateAssetMenu(fileName = "New Attribute", menuName = "LightCone/Data/Attribute Definition")]
    public sealed class AttributeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private AttributeType attributeType;
        [SerializeField] private string displayName;
        [SerializeField][TextArea(2, 4)] private string description;

        [Header("Constraints")]
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 999f;
        [SerializeField] private bool hasMaximum = true;

        [Header("Display")]
        [SerializeField] private Sprite icon;
        [SerializeField] private bool showDecimal;

        public AttributeType AttributeType => attributeType;
        public string DisplayName => displayName;
        public string Description => description;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public bool HasMaximum => hasMaximum;
        public Sprite Icon => icon;
        public bool ShowDecimal => showDecimal;

        /// <summary>
        /// Clamp a value to this attribute's defined range.
        /// </summary>
        public float Clamp(float value)
        {
            float clamped = Mathf.Max(value, minValue);
            return hasMaximum ? Mathf.Min(clamped, maxValue) : clamped;
        }
    }
}