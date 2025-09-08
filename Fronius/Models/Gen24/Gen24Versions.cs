namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
//TODO: This class is incomplete
public partial class Gen24Versions : BindableBase
{
    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "commandsApi")]
    public partial Version? CommandApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "ComponentsApi")]
    public partial Version? ComponentsApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "configApi")]
    public partial Version? ConfigApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "setupAppApi")]
    public partial Version? SetupAppApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "statusApi")]
    public partial Version? StatusApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("apiversions", "updateApi")]
    public partial Version? UpdateApi { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("devicename", FroniusDataType.Root)]
    public partial string? ModelName { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("articleNumber", FroniusDataType.Root)]
    public partial string? ArticleNumber { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("commonName", FroniusDataType.Root)]
    public partial string? CommonName { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("serialNumber", FroniusDataType.Root)]
    public partial string? SerialNumber { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("numberOfPhases", FroniusDataType.Root)]
    public partial byte? NumberOfPhases { get; set; }

    [ObservableProperty]
    public partial IDictionary<string, Version> SwVersions { get; set; } = new Dictionary<string, Version>();

    public static Gen24Versions Parse(JToken token)
    {
        var gen24Service = IoC.Get<IGen24JsonService>();
        var result = gen24Service.ReadFroniusData<Gen24Versions>(token);

        var swRevisions = token["swrevisions"];

        if (swRevisions != null)
        {
            foreach (var swToken in swRevisions.OfType<JProperty>())
            {
                Version version;

                try
                {
                    version = new Version(swToken.Value.ToString().Replace("-", "."));
                }
                catch
                {
                    continue;
                }

                result.SwVersions[swToken.Name] = version;
            }
        }

        return result;
    }
}
