namespace De.Hochstaetter.Fronius.Extensions;

public static class VersionExtensions
{
    public static string ToLinuxString(this Version version)
    {
            var result = version.Major.ToString(CultureInfo.InvariantCulture);
            result += version.Minor >= 0 ? '.' + version.Minor.ToString(CultureInfo.InvariantCulture) : null;
            result += version.Build >= 0 ? '.' + version.Build.ToString(CultureInfo.InvariantCulture) : null;
            result += version.Revision >= 0 ? '-' + version.Revision.ToString(CultureInfo.InvariantCulture) : null;
            return result;
        }
}