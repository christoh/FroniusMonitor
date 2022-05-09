using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models.Gen24
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class Gen24Restrictions:Gen24DeviceBase
    {
        private double? maxStorageChargeFromGridPower;
        [FroniusProprietaryImport("BAT_MODE_POWERRESTRICTION_CHARGE_FROM_AC_U16")]
        public double? MaxStorageChargeFromGridPower
        {
            get => maxStorageChargeFromGridPower;
            set => Set(ref maxStorageChargeFromGridPower, value);
        }

        private double maxStateOfCharge;
        [FroniusProprietaryImport("BAT_PERCENT_POWERRESTRICTION_SOC_MAX_F64", Unit.Percent)]
        public double MaxStateOfCharge
        {
            get => maxStateOfCharge;
            set => Set(ref maxStateOfCharge, value);
        }

        private double minStateOfCharge;
        [FroniusProprietaryImport("BAT_PERCENT_POWERRESTRICTION_SOC_MIN_F64", Unit.Percent)]
        public double MinStateOfCharge
        {
            get => minStateOfCharge;
            set => Set(ref minStateOfCharge, value);
        }

        private double? storageLimitDischarge;
        [FroniusProprietaryImport("DCLINK_POWERACTIVE_LIMIT_DISCHARGE_F64")]
        public double? StorageLimitDischarge
        {
            get => storageLimitDischarge;
            set => Set(ref storageLimitDischarge, value);
        }

        private double? storageLimitCharge;
        [FroniusProprietaryImport("DCLINK_POWERACTIVE_MAX_F32")]
        public double? StorageLimitCharge
        {
            get => storageLimitCharge;
            set => Set(ref storageLimitCharge, value);
        }
    }
}
