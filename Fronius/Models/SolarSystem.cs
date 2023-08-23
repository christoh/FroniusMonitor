using De.Hochstaetter.Fronius.Models.Gen24.Settings;

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

    private Gen24Components? components;

    public Gen24Components? Components
    {
        get => components;
        set => Set(ref components, value);
    }

    private Gen24Components? components2;

    public Gen24Components? Components2
    {
        get => components2;
        set => Set(ref components2, value);
    }

    private Gen24Versions? versions;

    public Gen24Versions? Versions
    {
        get => versions;
        set => Set(ref versions, value);
    }

    private Gen24Versions? versions2;

    public Gen24Versions? Versions2
    {
        get => versions2;
        set => Set(ref versions2, value);
    }

    private Gen24Common? gen24Common;

    public Gen24Common? Gen24Common
    {
        get => gen24Common;
        set => Set(ref gen24Common, value);
    }

    private Gen24Common? gen24Common2;

    public Gen24Common? Gen24Common2
    {
        get => gen24Common2;
        set => Set(ref gen24Common2, value);
    }

    private FritzBoxDeviceList? fritzBox;

    public FritzBoxDeviceList? FritzBox
    {
        get => fritzBox;
        set => Set(ref fritzBox, value);
    }

    //private PowerFlow? powerFlow;

    //public PowerFlow? PowerFlow
    //{
    //    get => powerFlow;
    //    set => Set(ref powerFlow, value);
    //}

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

