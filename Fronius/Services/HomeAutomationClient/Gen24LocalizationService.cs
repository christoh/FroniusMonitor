using System.Text.Json;

namespace De.Hochstaetter.Fronius.Services.HomeAutomationClient;

public class Gen24LocalizationService(IWebClientService webClient) : IGen24LocalizationService
{
    public IDictionary<Gen24LocalizationSection, JsonElement> Sections { get; } = new ConcurrentDictionary<Gen24LocalizationSection, JsonElement>();

    public IDictionary<Gen24LocalizationSection, JsonElement> InvariantSections { get; } = new ConcurrentDictionary<Gen24LocalizationSection, JsonElement>();

    public async Task<bool> Initialize()
    {
        var gen24Devices = await webClient.GetGen24Devices().ConfigureAwait(false);

        if (gen24Devices.HasErrors || gen24Devices.Payload == null || gen24Devices.Payload.Count < 1)
        {
            return false;
        }

        var language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        var key = gen24Devices.Payload.Keys.First();
        var taskList = new List<Task<bool>>();

        foreach (var section in Enum.GetValues<Gen24LocalizationSection>())
        {
            taskList.Add(ReadFile(language, Sections));

            if (language.ToLowerInvariant() != "en")
            {
                taskList.Add(ReadFile("en", InvariantSections));
            }
            
            continue;

            async Task<bool> ReadFile(string twoLetterISOLanguageName, IDictionary<Gen24LocalizationSection, JsonElement> dictionary)
            {
                var result = await webClient.GetGen24Localization(key, twoLetterISOLanguageName, section.ToString()).ConfigureAwait(false);

                if (result.HasErrors)
                {
                    return false;
                }

                dictionary[section] = result.Payload;
                return true;
            }
            
        }

        foreach (var task in taskList)
        {
            if (!await task.ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    public string GetLocalizedString(Gen24LocalizationSection section, string key)
    {
        var result = GetValue(Sections) ?? GetValue(InvariantSections) ?? key;
        return result;

        string? GetValue(IDictionary<Gen24LocalizationSection, JsonElement> dictionary)
        {
            var keyParts = key.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (!dictionary.TryGetValue(section, out var currentDictionary))
            {
                return null;
            }

            if (keyParts.Any(keyPart => !currentDictionary.TryGetProperty(keyPart, out currentDictionary)))
            {
                return null;
            }
            
            return currentDictionary.ValueKind == JsonValueKind.String ? currentDictionary.GetString() : null;
        }
    }

    public string GetEventDisplayName(string key) => GetLocalizedString(Gen24LocalizationSection.Events, $"StateCodes.{key}");
}
