namespace De.Hochstaetter.HomeAutomationServer.Models;

internal static class GitInfo
{
    public static Version Version { get; } = new(FormattableString.Invariant($"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}"));

    public static string VersionString { get; } = FormattableString.Invariant($"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}{ThisAssembly.Git.SemVer.DashLabel}");

    public static DateTimeOffset CommitTime { get; } = DateTimeOffset.Parse(ThisAssembly.Git.CommitDate, CultureInfo.InvariantCulture);

    public static bool IsDirty => ThisAssembly.Git.IsDirty; // True, if it was build with uncommited changes

    public static string CommitId => ThisAssembly.Git.Sha;
}