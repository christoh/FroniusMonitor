namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum ModelStatus
{
    NotChargingBecauseNoChargeCtrlData = 0,
    NotChargingBecauseTemperatureLimit = 1,
    NotChargingBecauseAccessControlWait = 2,
    ChargingBecauseForceStateOn = 3,
    NotChargingBecauseForceStateOff = 4,
    NotChargingBecauseScheduler = 5,
    NotChargingBecauseEnergyLimit = 6,
    ChargingBecauseAwattarPriceLow = 7,
    ChargingBecauseAutomaticStopTestCharging = 8,
    ChargingBecauseAutomaticStopNotEnoughTime = 9,
    ChargingBecauseAutomaticStop = 10,
    ChargingBecauseAutomaticStopNoClock = 11,
    ChargingBecausePvSurplus = 12,
    ChargingBecauseFallbackGoEDefault = 13,
    ChargingBecauseFallbackGoEScheduler = 14,
    ChargingBecauseFallbackDefault = 15,
    NotChargingBecauseFallbackGoEAwattar = 16,
    NotChargingBecauseFallbackAwattar = 17,
    NotChargingBecauseFallbackAutomaticStop = 18,
    ChargingBecauseCarCompatibilityKeepAlive = 19,
    ChargingBecauseChargePauseNotAllowed = 20,
    NotChargingBecauseSimulateUnplugging = 22,
    NotChargingBecausePhaseSwitch = 23,
    NotChargingBecauseMinPauseDuration = 24
}

public enum CableLockBehavior
{
    Normal = 0,
    AutoUnlock = 1,
    AlwaysLock = 2,
}

public enum CarStatus : byte
{
    Unknown = 0,
    Idle = 1,
    Charging = 2,
    WaitCar = 3,
    Complete = 4,
    Error = 5
}

public enum PhaseSwitchMode : byte
{
    Auto = 0,
    Phase1 = 1,
    Phases3 = 2,
}

public enum AccessMode : byte
{
    Everyone = 0,
    RequireAuth = 1,
}

public enum ChargingLogic : byte
{
    /// <summary>
    /// Immediately charge the car until the car stops charging
    /// </summary>
    None = 3,

    /// <summary>
    /// Use enabled eco settings (PV surplus, awattar, etc.)
    /// </summary>
    Eco = 4,

    /// <summary>
    /// Ensure the car receives a certain amount of energy at a specific time.
    /// Make use of eco settings
    /// </summary>
    NextTrip = 5,
}

public enum ForcedCharge
{
    Default = 0,
    ForcedOff = 1,
    ForcedOn = 2,
}

public enum AwattarCountry : byte
{
    Austria = 0,
    Germany = 1,
}

public enum EcoRoundingMode : byte
{
    PreferFromGrid = 0,
    NoPreference = 1,
    PreferToGrid = 2,
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum CarType : byte
{
    @default,
    kiaSoul,
    ecorsa,
    renaultZoe,
    MitsubishiImiev,
    citroenCZero,
    peugeotIon,
    vwID3_4,
    Eqc2019,
}
