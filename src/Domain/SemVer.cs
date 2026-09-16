using System.Globalization;
using System.Text.RegularExpressions;

namespace TokaZerkUIConfig.Domain;

public readonly partial record struct SemVer(int Major, int Minor, int Patch, int Build = 0, int? PreRelease = null) : IComparable<SemVer>
{
    public int CompareTo(SemVer other)
    {
        var major = this.Major.CompareTo(other.Major);
        if (major != 0)
        {
            return major;
        }

        var minor = this.Minor.CompareTo(other.Minor);
        if (minor != 0)
        {
            return minor;
        }

        var patch = this.Patch.CompareTo(other.Patch);
        if (patch != 0)
        {
            return patch;
        }

        var build = this.Build.CompareTo(other.Build);
        if (build != 0)
        {
            return build;
        }

        if (this.PreRelease is null && other.PreRelease is null)
        {
            return 0;
        }

        // A release (PreRelease == null) outranks any of its betas.
        if (this.PreRelease is null)
        {
            return 1;
        }

        if (other.PreRelease is null)
        {
            return -1;
        }

        return this.PreRelease.Value.CompareTo(other.PreRelease.Value);
    }

    public override string ToString()
    {
        var version = this.Build > 0 ? $"{this.Major}.{this.Minor}.{this.Patch}.{this.Build}" : $"{this.Major}.{this.Minor}.{this.Patch}";
        return this.PreRelease.HasValue ? $"{version}-beta.{this.PreRelease}" : version;
    }

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

        if (!int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major))
        {
            return false;
        }

        if (!int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        var patch = 0;
        if (match.Groups["patch"].Success && !int.TryParse(match.Groups["patch"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out patch))
        {
            return false;
        }

        var build = 0;
        if (match.Groups["build"].Success && !int.TryParse(match.Groups["build"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out build))
        {
            return false;
        }

        int? preRelease = null;
        if (match.Groups["pre"].Success)
        {
            if (!int.TryParse(match.Groups["pre"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var pre))
            {
                return false;
            }

            preRelease = pre;
        }

        result = new SemVer(major, minor, patch, build, preRelease);
        return true;
    }

    public static SemVer? FindInText(string text)
    {
        var match = FindRegex().Match(text);
        return match.Success && TryParse(match.Value, out var version) ? version : null;
    }

    [GeneratedRegex(@"^v?(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?(\.(?<build>\d+))?(-beta\.(?<pre>\d+))?$")]
    private static partial Regex ParseRegex();

    [GeneratedRegex(@"\d+\.\d+(\.\d+){0,2}")]
    private static partial Regex FindRegex();
}
