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

public enum CarStatus
{
    Unknown = 0,
    Idle = 1,
    Charging = 2,
    WaitCar = 3,
    Complete = 4,
    Error = 5
}
