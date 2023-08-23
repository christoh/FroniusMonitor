using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.Gen24.Settings
{
    public class Gen24Common : BindableBase, ICloneable, IHaveDisplayName
    {
        public object Clone() => MemberwiseClone();

        private string? displayName;

        [FroniusProprietaryImport("systemName", FroniusDataType.Root)]
        public string DisplayName
        {
            get => displayName ?? "---";
            set => Set(ref displayName, value);
        }

        private string? timeZone;
        [FroniusProprietaryImport("timezone", FroniusDataType.Root)]
        public string? TimeZone
        {
            get => timeZone;
            set => Set(ref timeZone, value);
        }

        private bool? timeSync;
        // ReSharper disable once StringLiteralTypo
        [FroniusProprietaryImport("timesync", FroniusDataType.Root)]
        public bool? TimeSync
        {
            get => timeSync;
            set => Set(ref timeSync, value);
        }

        public override string ToString() => DisplayName;

        public static Gen24Common Parse(JToken token)
        {
            var gen24JsonService = IoC.Get<IGen24JsonService>();
            return gen24JsonService.ReadFroniusData<Gen24Common>(token);
        }
    }
}
