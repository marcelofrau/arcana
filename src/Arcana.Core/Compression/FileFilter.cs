using System.Text.RegularExpressions;

namespace Arcana.Core.Compression;

public class FileFilter
{
    private readonly List<string> _includes;
    private readonly List<string> _excludes;

    public FileFilter(IEnumerable<string>? include = null, IEnumerable<string>? exclude = null)
    {
        _includes = include?.ToList() ?? [];
        _excludes = exclude?.ToList() ?? [];
    }

    public bool IsIncluded(string path)
    {
        if (_excludes.Count > 0 && _excludes.Any(p => GlobMatch(path, p)))
            return false;

        if (_includes.Count > 0)
            return _includes.Any(p => GlobMatch(path, p));

        return true;
    }

    private static string GlobToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern);
        return "^" + escaped
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".")
            + "$";
    }

    private static bool GlobMatch(string path, string pattern)
        => Regex.IsMatch(path, GlobToRegex(pattern), RegexOptions.IgnoreCase);
}
