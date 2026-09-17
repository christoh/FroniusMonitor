using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// The test classes that point <see cref="Settings.SettingsFileName"/> at a temp file of their own.
/// </summary>
/// <remarks>
/// That name is a static, and <c>Settings.SaveAsync</c> without an argument writes to it, so two such classes
/// running at once would save each other's users into each other's files. xUnit runs classes in parallel by
/// default, which is what <see cref="CollectionDefinitionAttribute"/> with
/// <see cref="CollectionDefinitionAttribute.DisableParallelization"/> is switching off here. Nothing else in the
/// assembly reads the static - the other settings tests pass their file names explicitly - so nothing else needs
/// to join.
/// </remarks>
[CollectionDefinition("Settings", DisableParallelization = true)]
public class SettingsCollection;
