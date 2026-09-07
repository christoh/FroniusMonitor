using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Components : BindableBase
{
    [ObservableProperty]
    public partial IDictionary<string, IList<string>> Groups { get; set; } = new Dictionary<string, IList<string>>();

    public static Gen24Components Parse(JsonNode token)
    {
            var result = new Gen24Components();

            if (token["Body"]?["Data"] is not JsonObject listToken)
            {
                return result;
            }

            listToken
                .Select(item => (item.Value?["Group"].AsString() ?? "NONE", item.Key))
                .GroupBy(r => r.Item1)
                .Apply(item => result.Groups[item.Key]=item.Select(i=>i.Key).ToArray());

            return result;
        }
}