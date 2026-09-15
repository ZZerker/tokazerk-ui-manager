using System.Text.RegularExpressions;

namespace TokaZerkUIConfig.Domain;

public readonly partial record struct SemVer(int Major, int Minor, int Patch, int Build = 0) : IComparable<SemVer>
{
    public int CompareTo(SemVer other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0)
        {
            return major;
        }

        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0)
        {
            return minor;
        }

        var patch = Patch.CompareTo(other.Patch);
        return patch != 0 ? patch : Build.CompareTo(other.Build);
    }

    public override string ToString() => Build > 0 ? $"{Major}.{Minor}.{Patch}.{Build}" : $"{Major}.{Minor}.{Patch}";

    public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
    public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
    public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;

    public static bool TryParse(string? text, out SemVer result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = ParseRegex().Match(text.Trim());
        if (!match.Success)
        {
            return false;
        }

        var major = int.Parse(match.Groups["major"].Value);
        var minor = int.Parse(match.Groups["minor"].Value);
        var patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
        var build = match.Groups["build"].Success ? int.Parse(match.Groups["build"].Value) : 0;
        result = new SemVer(major, minor, patch, build);
        return true;
    }

    public static SemVer? FindInText(string text)
    {
        var match = FindRegex().Match(text);
        return match.Success && TryParse(match.Value, out var version) ? version : null;
    }

    [GeneratedRegex(@"^v?(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?(\.(?<build>\d+))?")]
    private static partial Regex ParseRegex();

    [GeneratedRegex(@"\d+\.\d+(\.\d+){0,2}")]
    private static partial Regex FindRegex();
}
