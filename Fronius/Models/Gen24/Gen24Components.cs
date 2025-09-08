using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Components : BindableBase
{
    [ObservableProperty]
    public partial IDictionary<string, IList<string>> Groups { get; set; } = new Dictionary<string, IList<string>>();

    public static Gen24Components Parse(JToken token)
    {
            var result = new Gen24Components();

            if (token["Body"]?["Data"] is not JObject listToken)
            {
                return result;
            }

            (listToken as IDictionary<string, JToken?>)
                .Select(item => (item.Value?["Group"]?.Value<string>() ?? "NONE", item.Key))
                .GroupBy(r => r.Item1)
                .Apply(item => result.Groups[item.Key]=item.Select(i=>i.Key).ToArray());

            return result;
        }
}