using System.Net;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.Internet;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <see cref="Ipv6Client"/> against <see cref="FakeIpv6Box"/>, which answers <c>sync_groups/sg/sg1001</c> the way the
/// Swiss box did on 2026-10-27: the whole group on a GET, and a PUT that changes the fields it names and keeps the others.
/// </summary>
public sealed class FritzBoxIpv6ClientTests : IDisposable
{
    private const string GroupPath = FakeIpv6Box.GroupPath;

    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());

    public void Dispose() => loggers.Dispose();

    [Fact]
    public async Task The_mode_is_read_from_the_IPv6_group_with_the_session()
    {
        var box = new FakeIpv6Box();
        using var http = CreateClient(box);

        var mode = await new Ipv6Client(http).GetModeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Ipv6Client.Automatic, mode);
        var request = Assert.Single(box.Requests);
        Assert.Equal($"GET {GroupPath}", request.Line);
        Assert.Equal("AVM-SID " + FakeFritzBox.Sid(1), request.Authorization);
    }

    [Theory]
    [InlineData(Ipv6Client.Off)]
    [InlineData(Ipv6Client.Automatic)]
    public async Task Switching_puts_the_mode_alone_and_leaves_the_other_fields_of_the_group_as_they_were(string mode)
    {
        var box = new FakeIpv6Box { Mode = mode == Ipv6Client.Off ? Ipv6Client.Automatic : Ipv6Client.Off };
        var before = box.Group;
        using var http = CreateClient(box);
        var client = new Ipv6Client(http);

        await client.SetModeAsync(mode, TestContext.Current.CancellationToken);

        var put = Assert.Single(box.Requests);
        Assert.Equal($"PUT {GroupPath}", put.Line);
        Assert.Equal("application/json", put.ContentType);
        Assert.Equal($$"""{"ipv6_mode":"{{mode}}"}""", put.Body);
        Assert.Equal(mode, await client.GetModeAsync(TestContext.Current.CancellationToken));
        Assert.Equal(before.Where(f => f.Key != "ipv6_mode"), box.Group.Where(f => f.Key != "ipv6_mode"));
    }

    [Fact]
    public async Task A_mode_of_the_web_interface_other_than_off_and_automatic_is_returned_as_the_box_names_it()
    {
        var box = new FakeIpv6Box { Mode = "ipv6_native" };
        using var http = CreateClient(box);

        Assert.Equal("ipv6_native", await new Ipv6Client(http).GetModeAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("""{"ipv4_mode":"ipv4_normal"}""")]
    [InlineData("""{"ipv6_mode":""}""")]
    [InlineData("""{"ipv6_mode":null}""")]
    [InlineData("[]")]
    public async Task An_answer_without_a_mode_is_reported_with_what_the_box_said(string json)
    {
        var box = new FakeIpv6Box { Response = json };
        using var http = CreateClient(box);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new Ipv6Client(http).GetModeAsync(TestContext.Current.CancellationToken));

        Assert.Contains("ipv6_mode", exception.Message);
        Assert.Contains(json, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task An_empty_mode_is_not_sent(string mode)
    {
        var box = new FakeIpv6Box();
        using var http = CreateClient(box);

        await Assert.ThrowsAsync<ArgumentException>(() => new Ipv6Client(http).SetModeAsync(mode, TestContext.Current.CancellationToken));

        Assert.Empty(box.Calls);
    }

    [Fact]
    public async Task A_box_that_refuses_the_change_is_reported_with_its_answer()
    {
        var box = new FakeIpv6Box { RefusePut = true };
        using var http = CreateClient(box);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => new Ipv6Client(http).SetModeAsync(Ipv6Client.Off, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("invalid value", exception.Message);
    }

    private FritzBoxHttpClient CreateClient(FakeIpv6Box box) =>
        new(new FritzBoxHttpClientOptions { BaseUri = FakeFritzBox.BaseUri, Password = "secret" }, loggers.CreateLogger<FritzBoxHttpClient>(), box);
}
