namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public enum Gen24LocalizationSection:sbyte
{
    Channels,
    Ui,
    Config,
    Events
}

public interface IGen24LocalizationService
{
    Task<bool> Initialize();

    string GetLocalizedString(Gen24LocalizationSection section, string key);

    string GetEventDisplayName(string key);
}
