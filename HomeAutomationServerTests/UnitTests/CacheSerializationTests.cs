using System.Text.Json;
using System.Text.Json.Nodes;
using De.Hochstaetter.HomeAutomationClient;
using De.Hochstaetter.HomeAutomationClient.Misc;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What an <see cref="ICache"/> writes, and that it can read back what it wrote.
/// </summary>
/// <remarks>
/// <para>
/// The cache holds the credentials of the server, so what it leaves out matters as much as what it keeps:
/// <c>WebConnection.Password</c> is deliberately absent and only its encrypted form is stored. The browser head
/// used to serialize the same object with Newtonsoft, which does not honor the
/// <c>System.Text.Json.Serialization.JsonIgnore</c> on that property, and so wrote the password into local
/// storage in clear text. Both heads now go through <see cref="CacheJson.Options"/>.
/// </para>
/// <para>
/// The other half is symmetry. The options say things about reading as well as about writing, and for a while
/// only the writer was given them, which is invisible until a value needs the reader's permission too - a
/// <see cref="double.NaN"/> being the cheapest example.
/// </para>
/// </remarks>
public sealed class CacheSerializationTests : IDisposable
{
    private sealed class Measurement
    {
        public double Value { get; set; }
    }

    private readonly TempFileCache cache = new();

    public void Dispose() => cache.Dispose();

    [Fact]
    public void ConnectionIsWrittenWithoutSecretsOrFramework()
    {
        var connection = new HomeAutomationServerConnection { BaseUrl = "https://home.example.com/api/", UserName = "someone", Password = "s3cret" };
        cache.AddOrUpdate(CacheKeys.Connection, connection);

        var written = JsonSerializer.Serialize(connection, CacheJson.Options);
        var properties = JsonNode.Parse(written)!.AsObject().Select(p => p.Key).ToList();

        Assert.DoesNotContain("Password", properties);
        Assert.DoesNotContain("ClearTextPassword", properties);
        Assert.DoesNotContain("CalculatedChecksum", properties);
        Assert.DoesNotContain("DisplayName", properties);
        // Read-only, and every cached BindableBase carries it. This is what IgnoreReadOnlyProperties is on for.
        Assert.DoesNotContain("HasErrors", properties);
        Assert.DoesNotContain("s3cret", written);

        Assert.Contains("BaseUrl", properties);
        Assert.Contains("UserName", properties);
        Assert.Contains("EncryptedPassword", properties);
    }

    [Fact]
    public void ConnectionSurvivesTheCache()
    {
        cache.AddOrUpdate(CacheKeys.Connection, new HomeAutomationServerConnection { BaseUrl = "https://home.example.com/api/", UserName = "someone", Password = "s3cret" });

        var loaded = cache.Get<HomeAutomationServerConnection>(CacheKeys.Connection);

        Assert.NotNull(loaded);
        Assert.Equal("https://home.example.com/api/", loaded.BaseUrl);
        Assert.Equal("someone", loaded.UserName);
        // Through EncryptedPassword: the clear text never goes near the file.
        Assert.Equal("s3cret", loaded.Password);
    }

    [Fact]
    public async Task ConnectionSurvivesTheCacheAsynchronously()
    {
        await cache.AddOrUpdateAsync(CacheKeys.Connection, new HomeAutomationServerConnection { BaseUrl = "https://home.example.com/api/", UserName = "someone", Password = "s3cret" }, TestContext.Current.CancellationToken);

        var loaded = await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal("someone", loaded.UserName);
        Assert.Equal("s3cret", loaded.Password);
    }

    /// <summary>
    /// What a logout does to the cache: the credentials go, and nothing else does - the server address in
    /// particular has to be there for the next login.
    /// </summary>
    [Fact]
    public async Task RemovingTheConnectionKeepsTheRest()
    {
        await cache.AddOrUpdateAsync(CacheKeys.ApiUri, "https://home.example.com/api/", TestContext.Current.CancellationToken);
        await cache.AddOrUpdateAsync(CacheKeys.Connection, new HomeAutomationServerConnection { BaseUrl = "https://home.example.com/api/", UserName = "someone", Password = "s3cret" }, TestContext.Current.CancellationToken);

        await cache.RemoveAsync(CacheKeys.Connection, TestContext.Current.CancellationToken);

        Assert.Null(await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection, TestContext.Current.CancellationToken));
        Assert.Equal("https://home.example.com/api/", await cache.GetAsync<string>(CacheKeys.ApiUri, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("someone", await File.ReadAllTextAsync(Path.Combine(cache.Directory, "cache.json"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemovingAKeyThatIsNotThereIsNotAnError()
    {
        await cache.RemoveAsync(CacheKeys.Connection, TestContext.Current.CancellationToken);

        Assert.Null(await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void WhatTheWriterIsAllowedToWriteTheReaderIsAllowedToRead(double value)
    {
        // The writer turns these into "NaN" and friends. A reader that was not given the same options throws on
        // them, so this fails the moment the two sides stop sharing one JsonSerializerOptions.
        cache.AddOrUpdate("measurement", new Measurement { Value = value });

        Assert.Equal(value, cache.Get<Measurement>("measurement")!.Value);
    }

    [Fact]
    public void ReadingIsCaseInsensitive()
    {
        // Not for our own output, which always matches - for a cache file that was edited by hand.
        File.WriteAllText(Path.Combine(cache.Directory, "cache.json"), """measurement={"value": 42}""" + Environment.NewLine);

        Assert.Equal(42d, cache.Get<Measurement>("measurement")!.Value);
    }
}
