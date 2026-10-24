using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.SmartHome;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the FritzBox library does with a certificate the operating system does not trust, as the box's own
/// self-signed one is: without a pinned fingerprint it is accepted and named once in the log, with one only the
/// pinned certificate is.
/// </summary>
public sealed class FritzBoxCertificateTests : IDisposable
{
    private static readonly Uri box = new("https://192.168.44.11/");

    private readonly X509Certificate2 boxCertificate = CreateCertificate("CN=fritz.box");
    private readonly X509Certificate2 otherCertificate = CreateCertificate("CN=somebody.else");
    private readonly RecordingLogger logger = new();

    public void Dispose()
    {
        boxCertificate.Dispose();
        otherCertificate.Dispose();
    }

    [Fact]
    public void Without_a_pin_any_certificate_is_accepted_and_named_once_in_the_log()
    {
        var policy = new UntrustedCertificatePolicy(box, null, logger);

        Assert.True(policy.Accept(boxCertificate));
        Assert.True(policy.Accept(boxCertificate));
        Assert.True(policy.Accept(otherCertificate));

        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains(Fingerprint(boxCertificate), warning.Message);
        Assert.Contains("CertificateFingerprint", warning.Message);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void With_a_pin_the_pinned_certificate_is_accepted_however_the_fingerprint_is_written(bool lowerCase, bool withColons)
    {
        var pin = Fingerprint(boxCertificate);
        pin = lowerCase ? pin.ToLowerInvariant() : pin;
        pin = withColons ? string.Join(':', pin.Chunk(2).Select(pair => new string(pair))) : pin;
        var policy = new UntrustedCertificatePolicy(box, pin, logger);

        Assert.True(policy.Accept(boxCertificate));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void With_a_pin_another_certificate_is_refused_with_an_error_that_names_both()
    {
        var policy = new UntrustedCertificatePolicy(box, Fingerprint(boxCertificate), logger);

        Assert.False(policy.Accept(otherCertificate));

        var error = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Contains(Fingerprint(otherCertificate), error.Message);
        Assert.Contains(Fingerprint(boxCertificate), error.Message);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("ab:cd ef", "ABCDEF")]
    public void A_fingerprint_is_compared_as_upper_case_hex_without_separators(string? written, string? normalized)
    {
        Assert.Equal(normalized, UntrustedCertificatePolicy.Normalize(written));
    }

    [Fact]
    public async Task The_service_checks_the_pin_of_its_connection_and_makes_a_new_client_when_the_pin_changes()
    {
        var created = new List<FritzBoxHttpClientOptions>();
        var connection = new WebConnection { BaseUrl = box.ToString(), UserName = "user", Password = "secret", CertificateFingerprint = Fingerprint(boxCertificate) };

        using var service = new FritzBoxSmartHomeService(NullLoggerFactory.Instance, options =>
        {
            created.Add(options);
            return new FritzBoxHttpClient(options, null, new NoBox());
        });

        service.Connection = connection;
        await Assert.ThrowsAsync<HttpRequestException>(() => service.FritzBoxLogin(TestContext.Current.CancellationToken).AsTask());
        connection.CertificateFingerprint = Fingerprint(otherCertificate);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.FritzBoxLogin(TestContext.Current.CancellationToken).AsTask());

        Assert.Equal(2, created.Count);
        Assert.True(created[0].AcceptUntrustedCertificate!(boxCertificate));
        Assert.False(created[0].AcceptUntrustedCertificate!(otherCertificate));
        Assert.True(created[1].AcceptUntrustedCertificate!(otherCertificate));
    }

    private static string Fingerprint(X509Certificate2 certificate) => certificate.GetCertHashString(HashAlgorithmName.SHA256);

    private static X509Certificate2 CreateCertificate(string subject)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    /// <summary>A box that is not there: the service only has to make its client, not to reach anything.</summary>
    private sealed class NoBox : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("No box in a unit test");
    }
}
