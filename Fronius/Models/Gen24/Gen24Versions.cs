namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
//TODO: This class is incomplete
public class Gen24Versions : BindableBase
{
    [FroniusProprietaryImport("apiversions", "commandsApi")]
    public Version? CommandApi
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("apiversions", "configApi")]
    public Version? ConfigApi
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("apiversions", "setupAppApi")]
    public Version? SetupAppApi
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("apiversions", "statusApi")]
    public Version? StatusApi
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("apiversions", "updateApi")]
    public Version? UpdateApi
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("devicename", FroniusDataType.Root)]
    public string? ModelName
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("articleNumber", FroniusDataType.Root)]
    public string? ArticleNumber
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("commonName", FroniusDataType.Root)]
    public string? CommonName
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("serialNumber", FroniusDataType.Root)]
    public string? SerialNumber
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("numberOfPhases", FroniusDataType.Root)]
    public byte? NumberOfPhases
    {
        get;
        set => Set(ref field, value);
    }

    public IDictionary<string, Version> SwVersions
    {
        get;
        set => Set(ref field, value);
    } = new Dictionary<string, Version>();

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
