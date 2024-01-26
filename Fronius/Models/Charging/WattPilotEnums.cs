// ReSharper disable UnusedMember.Global
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

public enum WifiScanStatus
{
    None = 0,
    Scanning = 1,
    Finished = 2,
    Failed = 3,
}

public enum WifiState
{
    None = 0,
    Scanning = 1,
    Connecting = 2,
    Connected = 3,
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

[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum AwattarCountry : int
{
    Austria = 0,
    GermanyLuxembourg = 1,
    Switzerland = 10035,
    Belgium = 10043,
    Denmark1 = 10024,
    Denmark2 = 10036,
    DenmarkTibber1 = 10055,
    DenmarkTibber2 = 10056,
    Estonia = 10020,
    Finland = 10004,
    FinlandTibber = 10049,
    France = 10015,
    Greece = 10006,
    ItalyCalabria = 10017,
    ItalyCenterNorth = 10031,
    ItalyCenterSouth = 10009,
    ItalyNorth = 10034,
    ItalySacoAc = 10023,
    ItalySacoDc = 10001,
    ItalySardinia = 10029,
    ItalySicily = 10005,
    ItalySouth = 10041,
    Croatia = 10003,
    Latvia = 10010,
    Lithuania = 10030,
    Montenegro = 10012,
    Netherlands = 10038,
    Poland = 10037,
    Portugal = 10027,
    Romania = 10044,
    Serbia = 10039,
    Slovakia = 10040,
    Slovenia = 10025,
    Spain = 10018,
    Czechia = 10019,
    Ukraine = 10002,
    Hungary = 10042,
    Sweden1 = 10062,
    Sweden2 = 10063,
    Sweden3 = 10064,
    Sweden4 = 10065,
    Sweden1Tibber = 10045,
    Sweden2Tibber = 10046,
    Sweden3Tibber = 10047,
    Sweden4Tibber = 10048,
    Norway1 = 10057,
    Norway2 = 10058,
    Norway3 = 10059,
    Norway4 = 10060,
    Norway5 = 10061,
    NorwayVirtualNo2Nsl = 10021,
    NorwayTibber1 = 10050,
    NorwayTibber2 = 10051,
    NorwayTibber3 = 10052,
    NorwayTibber4 = 10053,
    NorwayTibber5 = 10054,
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

//[Flags]
//public enum Phases : byte
//{
//    None = 0,
//    L1 = 1 << 0,
//    L2 = 1 << 1,
//    L3 = 1 << 2,
//    All = L1 | L2 | L3,
//}

public enum LoadBalancingPriority : byte
{
    Low = 60,
    Medium = 50,
    High = 40,
}
