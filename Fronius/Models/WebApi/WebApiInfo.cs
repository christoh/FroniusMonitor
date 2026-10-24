namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What a build of the home automation software says about itself: which product and version it is, which commit it
/// was built from, and what it runs on. The server answers <c>GET /api</c> with one, anonymously; the client's About
/// dialog makes one for itself with <see cref="ForThisProcess"/> and shows it above the one the server sent, so both
/// are described the same way. In the shared library for that reason.
/// </summary>
public partial class WebApiInfo : BindableBase
{
    /// <summary>What <see cref="BuildInfo"/> says instead of the commit, for a build made with uncommitted changes.</summary>
    public const string DeveloperBuild = "(Developer Build)";

    [ObservableProperty]
    public partial string? ProductName { get; set; }

    [ObservableProperty]
    public partial string? Version { get; set; }

    [ObservableProperty]
    public partial string? Manufacturer { get; set; }

    [ObservableProperty]
    public partial string? OsArch { get; set; }

    [ObservableProperty]
    public partial string? OsVersion { get; set; }

    [ObservableProperty]
    public partial string? DotNetVersion { get; set; }

    /// <summary>When the commit the build was made from was made, in UTC.</summary>
    [ObservableProperty]
    public partial DateTimeOffset? CommitTime { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuildInfo))]
    public partial string? Branch { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuildInfo))]
    public partial string? CommitId { get; set; }

    /// <summary>
    /// Whether the build was made with uncommitted changes, so that <see cref="CommitId"/> does not tell what is in it.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuildInfo))]
    public partial bool IsDirty { get; set; }

    /// <summary><c>Debug</c> or <c>Release</c>.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuildInfo))]
    public partial string? Configuration { get; set; }

    /// <summary>
    /// Configuration, branch and commit in one line, the way the About box of the WPF app has always shown them, and
    /// the line to copy into a bug report.
    /// </summary>
    [JsonIgnore]
    public string BuildInfo => $"{Configuration}:{Branch}:{(IsDirty ? DeveloperBuild : CommitId)}";

    /// <summary>
    /// This process: the build from <see cref="FroniusGitInfo"/>, the platform from <see cref="RuntimeInformation"/>.
    /// The server and every client are built from one repository in one go, so what the shared library was built
    /// from is what the whole of it was built from.
    /// </summary>
    /// <param name="productName">What the caller calls itself; the only part that differs between the two.</param>
    public static WebApiInfo ForThisProcess(string productName) => new()
    {
        ProductName = productName,
        Version = FroniusGitInfo.VersionString,
        Manufacturer = "Christoph Hochstätter",
        OsArch = $"{RuntimeInformation.OSArchitecture}",
        OsVersion = RuntimeInformation.OSDescription,
        DotNetVersion = RuntimeInformation.FrameworkDescription,
        CommitTime = FroniusGitInfo.CommitTime.ToUniversalTime(),
        Branch = FroniusGitInfo.Branch,
        CommitId = FroniusGitInfo.CommitId,
        IsDirty = FroniusGitInfo.IsDirty,
        Configuration = FroniusGitInfo.Configuration,
    };
}
