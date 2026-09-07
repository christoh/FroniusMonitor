using System.Runtime.CompilerServices;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// Sets up the static <see cref="IoC"/> once for the whole assembly, before any test runs.
/// </summary>
/// <remarks>
/// <para>
/// The models of <c>Fronius</c> reach for services through that static injector rather than being given them -
/// <c>Gen24ChargingRule.ParseList</c> wants an <see cref="IGen24JsonService"/>, and the static constructor of
/// <c>WebConnection</c> wants an <see cref="IAesKeyProvider"/> the first time anything touches that type. It is
/// one injector for the process, so a test class that sets up its own takes it away from every other class, and
/// with xUnit running classes in parallel it is not knowable which one wins.
/// </para>
/// <para>
/// That went wrong as soon as a second class needed something: whichever class touched <c>WebConnection</c> first
/// found an injector that had no <see cref="IAesKeyProvider"/> in it, and
/// <c>Cannot dynamically create an instance of type IAesKeyProvider</c> came out of a static constructor, which
/// then stays broken for the rest of the run. So the registrations live here, once, and no test class sets up its
/// own.
/// </para>
/// </remarks>
internal static class TestInjector
{
    [ModuleInitializer]
    internal static void Initialize() => IoC.Update
    (
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<IAesKeyProvider, TestAesKeyProvider>()
            // Gen24DataCollector asks the injector for one of these per inverter it polls.
            .AddTransient<IGen24Service, Gen24Service>()
            .BuildServiceProvider()
    );
}
