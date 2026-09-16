using System;
using System.Text.RegularExpressions;

namespace PostService.Helpers;

public static class HashtagExtractor
{
    private static readonly Regex HashtagRegex = new(@"#([\w\u0600-\u06FF]+)", RegexOptions.Compiled);

    public static List<string> Extract(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new();

        return HashtagRegex.Matches(content)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}