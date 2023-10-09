namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static void Main(string[] args)
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
            .AddSingleton<SunSpecMeterService>()
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

        logger.LogInformation($"Arguments: {string.Join(" ", args)}");
        var client = IoC.Get<ISunSpecMeterClient>();
        
        logger.LogInformation("Starting server");
        var server = IoC.Get<SunSpecMeterService>();
        server.StartAsync().GetAwaiter().GetResult();
        
        logger.LogInformation("Connecting to Modbus/TCP of smart meter");
        client.ConnectAsync("127.0.0.1", 1502, 2).GetAwaiter().GetResult();
        
        logger.LogInformation("Retrieving data");
        var meter = client.GetDataAsync().GetAwaiter().GetResult();
        
        logger.LogInformation($"L1 Voltage: {meter.PhaseVoltageL1:N1} V");
        logger.LogInformation($"L2 Voltage: {meter.PhaseVoltageL2:N1} V");
        logger.LogInformation($"L3 Voltage: {meter.PhaseVoltageL3:N1} V");

        while (true)
        {
            Thread.Sleep(500);
        }
    }
}
