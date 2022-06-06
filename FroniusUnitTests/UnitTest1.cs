using System;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Extensions;

namespace FroniusUnitTests;

public class Tests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public async Task TestWattPilotHash()
    {
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(5000);
        var clientWebSocket = new ClientWebSocket();
        await clientWebSocket.ConnectAsync(new Uri("ws://192.168.44.114/ws"), cancellationSource.Token);
        var buffer = new byte[4096];

        var result = await clientWebSocket.ReceiveAsync(buffer, cancellationSource.Token);
        var hello = Encoding.UTF8.GetString(buffer, 0, result.Count);
        result = await clientWebSocket.ReceiveAsync(buffer, cancellationSource.Token);
        var auth = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var serial = JsonNode.Parse(hello)?["serial"]?.GetValue<string>();

        var token1 = "u7IJNrrodRLKBCcchlymQj4cVKPDxqa7";
        var token2 = "9M4ZsZQFlNFcvI26QkyEmAd1M8dWjGsJ";
        var token3 = "779585582aaaae09e5dd669cde5a0251";

        using var deriveBytes = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes("XXXXXXXXX"), Encoding.UTF8.GetBytes(serial??string.Empty), 100000, HashAlgorithmName.SHA512);
        var hash0 = deriveBytes.GetBytes(256);
        var hashedPassword = Convert.ToBase64String(hash0)[..32];
        Assert.AreEqual("YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY",hashedPassword);
        using var sha256 = SHA256.Create();
        var hash1Input = Encoding.UTF8.GetBytes(token1+hashedPassword);
        var hash1=string.Join(string.Empty,sha256.ComputeHash(hash1Input,0,hash1Input.Length).Select(b=>b.ToString("x2")));
        Assert.AreEqual("c6312eefa1b2ae0723a30e580d36c4e56caf9c9999441a1958e38ae1292fdbfa",hash1);
        var hashInput = Encoding.UTF8.GetBytes(token3+token2+hash1);
        var hash=string.Join(string.Empty,sha256.ComputeHash(hashInput,0,hashInput.Length).Select(b=>b.ToString("x2")));
        Assert.AreEqual("4452d701f83e5e2d5db0404431d51795b2c69e9a4c12f3bc8459f8fc731c2fb2",hash);
    }
}
