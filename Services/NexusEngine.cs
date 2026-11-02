
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EdgeKit.Services;

public static class NexusEngine
{
    private static readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase);

    private static readonly Regex UrlRegex = new(
        @"https?://[^\s<>""']+",
        RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"(?<!\w)(?:\+?\d[\d\s().-]{7,}\d)(?!\w)");

    private static readonly Regex CurrencyRegex = new(
        @"(?:₦|NGN|[$€£])\s?\d[\d,]*(?:\.\d{1,2})?",
        RegexOptions.IgnoreCase);

    private static readonly Regex DateRegex = new(
        @"\b(?:\d{1,4}[-/.]\d{1,2}[-/.]\d{1,4}|(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+\d{1,2}(?:,?\s+\d{4})?)\b",
        RegexOptions.IgnoreCase);

    private static readonly Regex Ipv4Regex = new(
        @"(?<![\d.])(?:\d{1,3}\.){3}\d{1,3}(?![\d.])");

    private static readonly Regex UuidRegex = new(
        @"\b[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b",
        RegexOptions.IgnoreCase);

    private static readonly HashSet<string> StopWords = new(
        new[]
        {
            "the", "and", "for", "are", "but", "not", "you",
            "your", "this", "that", "with", "from", "have",
            "has", "was", "were", "will", "would", "could",
            "should", "their", "there", "they", "them", "then",
            "when", "what", "where", "which", "who", "how",
            "about", "into", "onto", "also", "been", "being",
            "can", "all", "any", "our", "out", "its", "it's",
            "his", "her", "she", "him", "one", "two", "use",
            "using", "used", "more", "some", "such", "than",
            "each", "other", "only", "very", "just", "like",
            "get", "got", "make", "made", "does", "did"
        },
        StringComparer.OrdinalIgnoreCase);

    public static string Analyze(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "No information was provided.";

        var output = new StringBuilder();

        string normalized = input.Trim();

        var emails = Find(EmailRegex, normalized);
        var urls = Find(UrlRegex, normalized);
        var phones = Find(PhoneRegex, normalized);
        var currencies = Find(CurrencyRegex, normalized);
        var dates = Find(DateRegex, normalized);
        var uuids = Find(UuidRegex, normalized);

        var ipAddresses = Find(Ipv4Regex, normalized)
            .Where(IsValidIp)
            .ToList();

        bool isJson = IsValidJson(normalized);
        bool isBase64 = IsPossibleBase64(normalized);

        var keywords = FindKeywords(normalized);

        output.AppendLine("NEXUS ANALYSIS");
        output.AppendLine(new string('═', 32));
        output.AppendLine();

        output.AppendLine("CONTENT OVERVIEW");
        output.AppendLine($"Characters: {normalized.Length}");
        output.AppendLine($"Words: {CountWords(normalized)}");
        output.AppendLine($"Lines: {normalized.Replace("\r\n", "\n").Split('\n').Length}");
        output.AppendLine($"Detected content: {GetContentType(normalized, isJson)}");
        output.AppendLine();

        AddSection(output, "EMAIL ADDRESSES", emails);
        AddSection(output, "WEB LINKS", urls);
        AddSection(output, "PHONE NUMBERS", phones);
        AddSection(output, "CURRENCY AMOUNTS", currencies);
        AddSection(output, "DATES", dates);
        AddSection(output, "IP ADDRESSES", ipAddresses);
        AddSection(output, "UUID IDENTIFIERS", uuids);

        output.AppendLine("FORMAT DETECTION");
        output.AppendLine($"Valid JSON: {(isJson ? "Yes" : "No")}");
        output.AppendLine($"Possible Base64: {(isBase64 ? "Yes" : "No")}");
        output.AppendLine();

        output.AppendLine("IMPORTANT KEYWORDS");

        if (keywords.Count == 0)
        {
            output.AppendLine("No strong recurring keywords detected.");
        }
        else
        {
            foreach (var keyword in keywords)
                output.AppendLine($"• {keyword.Key} ({keyword.Value} occurrences)");
        }

        output.AppendLine();
        output.AppendLine("SUGGESTED ACTIONS");

        var suggestions = GetSuggestions(
            emails, urls, phones, currencies,
            dates, isJson, isBase64);

        foreach (var suggestion in suggestions)
            output.AppendLine($"• {suggestion}");

        output.AppendLine();
        output.AppendLine("ANALYSIS NOTES");
        output.AppendLine(
            "Detected values are pattern-based candidates. " +
            "Always verify important information before acting on it.");

        return output.ToString();
    }

    private static List<string> Find(Regex regex, string input)
    {
        return regex.Matches(input)
            .Select(m => m.Value.Trim().TrimEnd('.', ',', ';', ')', ']'))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();
    }

    private static void AddSection(
        StringBuilder output,
        string title,
        List<string> values)
    {
        output.AppendLine(title);

        if (values.Count == 0)
        {
            output.AppendLine("None detected.");
        }
        else
        {
            foreach (var value in values)
                output.AppendLine($"• {value}");
        }

        output.AppendLine();
    }

    private static bool IsValidIp(string value)
    {
        return IPAddress.TryParse(value, out _);
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPossibleBase64(string value)
    {
        string candidate = value.Trim();

        if (candidate.Length < 12 ||
            candidate.Length % 4 != 0 ||
            candidate.Any(c =>
                !(char.IsLetterOrDigit(c) || c is '+' or '/' or '=')))
        {
            return false;
        }

        try
        {
            byte[] decoded = Convert.FromBase64String(candidate);
            return decoded.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static int CountWords(string input)
    {
        return Regex.Matches(input, @"\b[\p{L}\p{N}_'-]+\b").Count;
    }

    private static Dictionary<string, int> FindKeywords(string input)
    {
        return Regex.Matches(input.ToLowerInvariant(),
                @"\b[\p{L}]{4,}\b")
            .Select(m => m.Value)
            .Where(word => !StopWords.Contains(word))
            .GroupBy(word => word)
            .Select(group => new
            {
                Word = group.Key,
                Count = group.Count()
            })
            .Where(item => item.Count > 1)
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Word)
            .Take(8)
            .ToDictionary(item => item.Word, item => item.Count);
    }

    private static string GetContentType(
        string input,
        bool isJson)
    {
        if (isJson)
            return "JSON data";

        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "Web address";

        if (input.Contains("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            return "Contact card";

        if (input.Contains("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
            return "Calendar data";

        if (input.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase))
            return "Possible encoded or cryptographic data";

        if (input.Contains('@') && EmailRegex.IsMatch(input))
            return "Email-related information";

        if (CurrencyRegex.IsMatch(input))
            return "Financial information";

        if (UrlRegex.IsMatch(input))
            return "Text containing web links";

        if (input.Contains('\n'))
            return "Multi-line text";

        return "General text";
    }

    private static List<string> GetSuggestions(
        List<string> emails,
        List<string> urls,
        List<string> phones,
        List<string> currencies,
        List<string> dates,
        bool isJson,
        bool isBase64)
    {
        var suggestions = new List<string>();

        if (emails.Count > 0)
            suggestions.Add("Copy or save the detected email addresses.");

        if (urls.Count > 0)
            suggestions.Add("Inspect the detected links before opening them.");

        if (phones.Count > 0)
            suggestions.Add("Review the detected phone numbers.");

        if (currencies.Count > 0)
            suggestions.Add("Review the detected monetary amounts.");

        if (dates.Count > 0)
            suggestions.Add("Check the detected dates for deadlines or appointments.");

        if (isJson)
            suggestions.Add("Send this content to the JSON formatter.");

        if (isBase64)
            suggestions.Add("Try decoding this content in Encoding Lab.");

        if (suggestions.Count == 0)
            suggestions.Add("Save this information or analyze another text.");

        return suggestions;
    }
}
