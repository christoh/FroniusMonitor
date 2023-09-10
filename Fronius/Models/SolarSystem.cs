namespace De.Hochstaetter.Fronius.Models;

public class SolarSystem : BindableBase
{
    private Gen24System? gen24System;

    public Gen24System? Gen24System
    {
        get => gen24System;
        set => Set(ref gen24System, value);
    }

    private Gen24System? gen24System2;

    public Gen24System? Gen24System2
    {
        get => gen24System2;
        set => Set(ref gen24System2, value);
    }

    private Gen24Config? gen24Config;

    public Gen24Config? Gen24Config
    {
        get => gen24Config;
        set => Set(ref gen24Config, value);
    }

    private Gen24Config? gen24Config2;

    public Gen24Config? Gen24Config2
    {
        get => gen24Config2;
        set => Set(ref gen24Config2, value);
    }

    private FritzBoxDeviceList? fritzBox;

    public FritzBoxDeviceList? FritzBox
    {
        get => fritzBox;
        set => Set(ref fritzBox, value);
    }

    private Gen24PowerFlow? sitePowerFlow;

    public Gen24PowerFlow? SitePowerFlow
    {
        get => sitePowerFlow;
        set => Set(ref sitePowerFlow, value);
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        set => Set(ref wattPilot, value);
    }
}

