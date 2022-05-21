namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
//TODO: This class is incomplete
public class Gen24Versions : BindableBase
{
    private Version? commandApi;
    [FroniusProprietaryImport("apiversions", "commandsApi")]
    public Version? CommandApi
    {
        get => commandApi;
        set => Set(ref commandApi, value);
    }

    private Version? configApi;
    [FroniusProprietaryImport("apiversions", "configApi")]
    public Version? ConfigApi
    {
        get => configApi;
        set => Set(ref configApi, value);
    }

    private Version? setupAppApi;
    [FroniusProprietaryImport("apiversions", "setupAppApi")]
    public Version? SetupAppApi
    {
        get => setupAppApi;
        set => Set(ref setupAppApi, value);
    }

    private Version? statusApi;
    [FroniusProprietaryImport("apiversions", "statusApi")]
    public Version? StatusApi
    {
        get => statusApi;
        set => Set(ref statusApi, value);
    }

    private Version? updateApi;
    [FroniusProprietaryImport("apiversions", "updateApi")]
    public Version? UpdateApi
    {
        get => updateApi;
        set => Set(ref updateApi, value);
    }

    private string? modelName;
    [FroniusProprietaryImport("devicename", FroniusDataType.Root)]
    public string? ModelName
    {
        get => modelName;
        set => Set(ref modelName, value);
    }

    private string? articleNumber;
    [FroniusProprietaryImport("articleNumber", FroniusDataType.Root)]
    public string? ArticleNumber
    {
        get => articleNumber;
        set => Set(ref articleNumber, value);
    }

    private string? commonName;
    [FroniusProprietaryImport("commonName", FroniusDataType.Root)]
    public string? CommonName
    {
        get => commonName;
        set => Set(ref commonName, value);
    }

    private string? serialNumber;
    [FroniusProprietaryImport("serialNumber", FroniusDataType.Root)]
    public string? SerialNumber
    {
        get => serialNumber;
        set => Set(ref serialNumber, value);
    }

    private byte? numberOfPhases;
    [FroniusProprietaryImport("numberOfPhases", FroniusDataType.Root)]
    public byte? NumberOfPhases
    {
        get => numberOfPhases;
        set => Set(ref numberOfPhases, value);
    }

    private IDictionary<string, Version> swVersions = new Dictionary<string, Version>();

    public IDictionary<string, Version> SwVersions
    {
        get => swVersions;
        set => Set(ref swVersions, value);
    }

    public static Gen24Versions Parse(string jsonText)
    {
        var token = JObject.Parse(jsonText);
        var gen24Service = IoC.Get<IGen24JsonService>();
        var result = gen24Service.ReadFroniusData<Gen24Versions>(token);

        var swRevisions = token?["swrevisions"];

        if (swRevisions != null)
        {
            foreach (var swToken in swRevisions.OfType<JProperty>())
            {
                Version version;

                try
                {
                    version = new Version(swToken.Value.ToString()!.Replace("-", "."));
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
