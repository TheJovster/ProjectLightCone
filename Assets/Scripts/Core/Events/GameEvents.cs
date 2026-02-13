namespace LightCone.Core.Events
{
    // ── Stat System Events ──────────────────────────────────────────

    /// <summary>
    /// Fired when any attribute's effective value changes (base or modified).
    /// </summary>
    public struct AttributeChangedEvent : IGameEvent
    {
        public int EntityId;
        public AttributeType Attribute;
        public float OldValue;
        public float NewValue;
    }

    /// <summary>
    /// Fired when a modifier is added to or removed from an entity.
    /// </summary>
    public struct ModifierChangedEvent : IGameEvent
    {
        public int EntityId;
        public AttributeType Attribute;
        public bool WasAdded;
    }

    // ── Status Effect Events ────────────────────────────────────────

    /// <summary>
    /// Fired when a status effect is applied to an entity.
    /// </summary>
    public struct StatusEffectAppliedEvent : IGameEvent
    {
        public int EntityId;
        public string EffectId;
    }

    /// <summary>
    /// Fired when a status effect expires or is removed from an entity.
    /// </summary>
    public struct StatusEffectRemovedEvent : IGameEvent
    {
        public int EntityId;
        public string EffectId;
    }

    // ── Game State Events ───────────────────────────────────────────

    /// <summary>
    /// Fired when the game transitions between top-level states.
    /// </summary>
    public struct GameStateChangedEvent : IGameEvent
    {
        public GameStateType PreviousState;
        public GameStateType NewState;
    }

    // ── Resource System Events ──────────────────────────────────────

    /// <summary>
    /// Fired when a resource's current value changes (drain, consume, restore, regen).
    /// </summary>
    public struct ResourceChangedEvent : IGameEvent
    {
        public int EntityId;
        public string ResourceId;
        public float OldValue;
        public float NewValue;
        public float MaxValue;
    }

    /// <summary>
    /// Fired when a resource transitions to or from depleted state.
    /// </summary>
    public struct ResourceDepletedEvent : IGameEvent
    {
        public int EntityId;
        public string ResourceId;
        public bool IsDepleted;
    }

    // ── Time System Events ────────────────────────────────────────

    /// <summary>
    /// Fired every in-game hour (or per hour-step during AdvanceHours).
    /// </summary>
    public struct TimeTickEvent : IGameEvent
    {
        public float CurrentHour;
        public int CurrentDay;
        public float DeltaHours;
    }

    /// <summary>
    /// Fired when the time of day transitions between periods (e.g., Day to Dusk).
    /// </summary>
    public struct TimePeriodChangedEvent : IGameEvent
    {
        public TimePeriod PreviousPeriod;
        public TimePeriod NewPeriod;
        public float CurrentHour;
        public int CurrentDay;
    }

    // ── Rest System Events ──────────────────────────────────────────

    /// <summary>
    /// Fired when a rest action completes (successfully or shortened).
    /// </summary>
    public struct RestCompletedEvent : IGameEvent
    {
        public float HoursRested;
        public bool WasInterrupted;
        public bool WasShortened;
    }

    // ── Save System Events ──────────────────────────────────────────

    /// <summary>
    /// Fired after a successful save operation.
    /// </summary>
    public struct GameSavedEvent : IGameEvent
    {
        public int SlotIndex;
        public string SaveName;
    }

    /// <summary>
    /// Fired after a successful load operation.
    /// </summary>
    public struct GameLoadedEvent : IGameEvent
    {
        public int SlotIndex;
        public string SaveName;
    }

    // ── Scene System Events ─────────────────────────────────────────

    /// <summary>
    /// Fired when a scene load operation begins.
    /// </summary>
    public struct SceneLoadStartedEvent : IGameEvent
    {
        public string TargetScene;
    }

    /// <summary>
    /// Fired during async scene loading with normalized progress (0-1).
    /// </summary>
    public struct SceneLoadProgressEvent : IGameEvent
    {
        public string TargetScene;
        public float Progress;
    }

    /// <summary>
    /// Fired when a scene has finished loading and is active.
    /// </summary>
    public struct SceneLoadCompletedEvent : IGameEvent
    {
        public string LoadedScene;
    }

    // ── Attribute and State Enums ───────────────────────────────────

    /// <summary>
    /// Core RPG attributes as defined in the GDD.
    /// </summary>
    public enum AttributeType
    {
        // Primary attributes
        Strength,
        Endurance,
        Agility,
        Perception,
        Intelligence,

        // Derived / resource attributes
        Health,
        MaxHealth,
        Stamina,
        MaxStamina,
        StaminaRegenRate,
        MovementSpeed,
        CarryCapacity,

        // Combat modifiers
        MeleeDamage,
        BlockEfficiency,
        DodgeWindow,
        AttackSpeed,

        // Survival
        LightEfficiency,
        DetectionRange
    }

    /// <summary>
    /// Top-level game states for the state machine.
    /// </summary>
    public enum GameStateType
    {
        Boot,
        MainMenu,
        Loading,
        Gameplay,
        Paused,
        GameOver
    }

    /// <summary>
    /// Periods of the in-game day cycle.
    /// Thresholds defined in TimeDefinitionSO.
    /// </summary>
    public enum TimePeriod
    {
        Dawn,
        Day,
        Dusk,
        Night
    }
}