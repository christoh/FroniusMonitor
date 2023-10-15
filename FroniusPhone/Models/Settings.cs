using De.Hochstaetter.Fronius.Extensions;

namespace FroniusPhone.Models
{
    public class Settings : SettingsShared
    {
        public void Load(string? fileName = null)
        {
            var settings = Load<Settings>(fileName ?? App.SettingsFileName);
            typeof(Settings).GetProperties().Apply(propertyInfo => propertyInfo.SetValue(this, propertyInfo.GetValue(settings)));
        }

        public async ValueTask LoadAsync(string? fileName = null)
        {
            await Task.Run(() => Load(fileName)).ConfigureAwait(false);
        }

        public void Save(string? fileName = null)
        {
            if (fileName == null)
            {
                Directory.CreateDirectory(App.PerUserDataDir);
            }
            Save(this, fileName ?? App.SettingsFileName);
        }

        public async ValueTask SaveAsync(string? fileName = null)
        {
            await Task.Run(() => Save(fileName)).ConfigureAwait(false);
        }
    }
}
