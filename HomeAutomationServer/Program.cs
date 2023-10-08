namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static void Main(string[] _)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
            .Verbose()
            .WriteTo
            .Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddScoped<IGen24Service, Gen24Service>()
            .AddSingleton<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IDataCollectionService, DataCollectionService>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddTransient<ISunSpecMeterClient, SunSpecMeterClient>()
            .AddLogging(builder => builder.AddSerilog())
            .BuildServiceProvider()
            ;

        var serviceProvider = serviceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);

        var logger = IoC.Get<ILogger<Program>>();

        //var server = new ModbusTcpServer(logger, true) { EnableRaisingEvents = true, ConnectionTimeout = TimeSpan.FromSeconds(20) };
        //server.AddUnit(2);
        //server.Start();

        //while (true)
        //{
        //    var registers = server.GetHoldingRegisters(2);

        //    lock (server.Lock)
        //    {
        //        registers.SetBigEndian<int>(40000, 1400204883);
        //    }

        //    Thread.Sleep(1000);
        //}

        var client = IoC.Get<ISunSpecMeterClient>();
        client.ConnectAsync("192.168.44.10", 502, 200).GetAwaiter().GetResult();
        var meter = client.GetDataAsync().GetAwaiter().GetResult();
    }
}
