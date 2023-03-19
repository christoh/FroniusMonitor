using System.Text;
using System.Xml;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Settings;

namespace FroniusPhone.Models
{
    public class Settings : SettingsBase
    {
        public void Load(string? fileName = null)
        {
            fileName ??= App.SettingsFileName;
            var serializer = new XmlSerializer(typeof(Settings));
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var settings = serializer.Deserialize(stream) as Settings ?? new Settings();
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

            fileName ??= App.SettingsFileName;
            UpdateChecksum(WattPilotConnection, FritzBoxConnection, FroniusConnection, ToshibaAcConnection);
            var serializer = new XmlSerializer(typeof(Settings));
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = new string(' ', 3),
                NewLineChars = Environment.NewLine,
            });

            serializer.Serialize(writer, this);
        }

        public async ValueTask SaveAsync(string? fileName = null)
        {
            await Task.Run(() => Save(fileName)).ConfigureAwait(false);
        }
    }
}
