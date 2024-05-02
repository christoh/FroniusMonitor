namespace De.Hochstaetter.Fronius.Models.Gen24.Settings
{
    public class Gen24PowerLimitSettings : BindableBase, ICloneable
    {
        private Gen24PowerLimits exportLimits = new();

        public Gen24PowerLimits ExportLimits
        {
            get => exportLimits;
            set => Set(ref exportLimits, value);
        }

        private Gen24PowerLimitsVisualization visualization = new();

        public Gen24PowerLimitsVisualization Visualization
        {
            get => visualization;
            set => Set(ref visualization, value);
        }

        public static Gen24PowerLimitSettings ParseFromConfig(JToken? configToken)
        {
            var token = configToken?["powerLimits"]?.Value<JToken>();
            return Parse(token);
        }

        public static Gen24PowerLimitSettings Parse(JToken? token)
        {
            var gen24PowerLimitSettings = new Gen24PowerLimitSettings
            {
                ExportLimits = Gen24PowerLimits.Parse(token?["exportLimits"]),
                Visualization = Gen24PowerLimitsVisualization.Parse(token?["visualization"]),
            };

            return gen24PowerLimitSettings;
        }

        public object Clone()
        {
            return new Gen24PowerLimitSettings
            {
                ExportLimits = (Gen24PowerLimits)ExportLimits.Clone(),
                Visualization = (Gen24PowerLimitsVisualization)Visualization.Clone(),
            };
        }
    }
}
