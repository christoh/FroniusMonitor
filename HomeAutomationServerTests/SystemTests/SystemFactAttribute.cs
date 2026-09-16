namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// A test that talks to the world outside the process - the inverter on the LAN, Awattar, the home server, the
/// multicast network. It needs the communication environment described in <c>CLAUDE.md</c> and fails without it,
/// so it is explicit: a plain <c>dotnet test</c> leaves it alone.
/// </summary>
/// <remarks>
/// Run them with <c>dotnet test --project HomeAutomationServerTests/HomeAutomationServerTests.csproj -c Debug --
/// --explicit only</c>, or one of them by adding <c>--filter-class</c>.
/// </remarks>
public sealed class SystemFactAttribute : FactAttribute
{
    public SystemFactAttribute() => Explicit = true;
}
