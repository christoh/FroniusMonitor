using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FroniusUnitTests;

public class Tests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public async Task Test1()
    {
        var cancellationSource= new CancellationTokenSource();
        cancellationSource.CancelAfter(5000);
        var clientWebSocket = new ClientWebSocket();
        await clientWebSocket.ConnectAsync(new Uri("ws://192.168.44.114/ws"), cancellationSource.Token);
        var buffer=new byte[4096];

        var result=await clientWebSocket.ReceiveAsync(buffer, cancellationSource.Token);
        var hello = Encoding.UTF8.GetString(buffer, 0, result.Count);
        result=await clientWebSocket.ReceiveAsync(buffer, cancellationSource.Token);
        var auth = Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}