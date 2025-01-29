namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24PowerLimitSettings : BindableBase, ICloneable
{
    public Gen24PowerLimits ExportLimits
    {
        get;
        set => Set(ref field, value);
    } = new();

    public Gen24PowerLimitsVisualization Visualization
    {
        get;
        set => Set(ref field, value);
    } = new();

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