namespace De.Hochstaetter.HomeAutomationServerTests.Logging;

/// <summary>
/// Routes everything the production code writes through <see cref="ILogger"/> into the output of the test that is
/// running, so a failing test shows what the server logged while it failed.
/// </summary>
/// <remarks>
/// The output helper is looked up per log call rather than injected, because a host created once for a whole test
/// class outlives the individual tests and has to write into whichever one is currently running.
/// </remarks>
public sealed class TestOutputLoggerProvider(LogLevel minimumLevel = LogLevel.Debug) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TestOutputLogger(categoryName, minimumLevel);

    public void Dispose()
    {
    }
}

public sealed class TestOutputLogger(string categoryName, LogLevel minimumLevel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel && logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || TestContext.Current.TestOutputHelper is not { } output)
        {
            return;
        }

        var line = $"{logLevel,-11} {categoryName}: {formatter(state, exception)}";

        if (exception != null)
        {
            line += Environment.NewLine + exception;
        }

        try
        {
            output.WriteLine(line);
        }
        catch (InvalidOperationException)
        {
            // The test has already finished and its output is closed. Nothing left to write to.
        }
    }
}

public static class TestOutputLoggingExtensions
{
    public static ILoggingBuilder AddTestOutput(this ILoggingBuilder builder, LogLevel minimumLevel = LogLevel.Debug)
    {
        builder.ClearProviders();
        builder.AddProvider(new TestOutputLoggerProvider(minimumLevel));
        builder.SetMinimumLevel(minimumLevel);
        return builder;
    }
}
