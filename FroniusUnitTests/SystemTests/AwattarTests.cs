using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Services;

namespace FroniusUnitTests.SystemTests;

[TestFixture]
public class AwattarTests
{
    private readonly IElectricityPriceService service = new AwattarService();

    [Test]
    public async Task DoTest()
    {
        service.PriceRegion = AwattarCountry.GermanyLuxembourg;
        var enumerable = await service.GetElectricityPricesAsync(17.879m, 1.2257m);
        Assert.That(enumerable != null);
        Debug.Assert(enumerable != null);
        var result = enumerable as IReadOnlyList<IElectricityPrice> ?? enumerable.ToList();
        Assert.That(result.Count == 24);
        Assert.That(result.All(r => r.StartTime.Kind == DateTimeKind.Utc));
        Assert.That(result.All(r => r.EndTime.Kind == DateTimeKind.Utc));
        Assert.That(result.All(r => r.EndTime.AddHours(-1).Equals(r.StartTime)));
    }
}