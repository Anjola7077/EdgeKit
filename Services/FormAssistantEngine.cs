
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EdgeKit.Services;

public sealed class FormFieldDefinition
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string[] Aliases { get; init; } = Array.Empty<string>();
}

public sealed class FormFieldMatch
{
    public string OriginalLabel { get; set; } = "";
    public string FieldKey { get; set; } = "";
    public string FieldLabel { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsMatched { get; set; }
}

public sealed class FormMatchResult
{
    public List<FormFieldMatch> Matches { get; set; } = new();
    public List<string> UnknownLabels { get; set; } = new();

    public int MatchedCount => Matches.Count(x =>
        x.IsMatched && !string.IsNullOrWhiteSpace(x.Value));

    public int MissingCount => Matches.Count(x =>
        !x.IsMatched || string.IsNullOrWhiteSpace(x.Value));
}

public static class FormAssistantEngine
{
    public static IReadOnlyList<FormFieldDefinition> Fields { get; } =
        new List<FormFieldDefinition>
        {
            new()
            {
                Key = "first_name",
                Label = "First name",
                Aliases = new[]
                {
                    "first name", "given name", "forename",
                    "first name(s)", "firstname"
                }
            },
            new()
            {
                Key = "middle_name",
                Label = "Middle name",
                Aliases = new[]
                {
                    "middle name", "middle initial", "middlename"
                }
            },
            new()
            {
                Key = "last_name",
                Label = "Surname / Last name",
                Aliases = new[]
                {
                    "surname", "last name", "family name",
                    "lastname", "second name"
                }
            },
            new()
            {
                Key = "full_name",
                Label = "Full name",
                Aliases = new[]
                {
                    "full name", "complete name", "applicant name",
                    "name of applicant", "your name", "name"
                }
            },
            new()
            {
                Key = "email",
                Label = "Email address",
                Aliases = new[]
                {
                    "email", "email address", "e-mail",
                    "e-mail address"
                }
            },
            new()
            {
                Key = "phone",
                Label = "Phone number",
                Aliases = new[]
                {
                    "phone", "phone number", "mobile",
                    "mobile number", "telephone", "contact number",
                    "primary phone"
                }
            },
            new()
            {
                Key = "alternate_phone",
                Label = "Alternative phone",
                Aliases = new[]
                {
                    "alternative phone", "alternate phone",
                    "secondary phone", "other phone"
                }
            },
            new()
            {
                Key = "date_of_birth",
                Label = "Date of birth",
                Aliases = new[]
                {
                    "date of birth", "dob", "birth date",
                    "birthday"
                }
            },
            new()
            {
                Key = "gender",
                Label = "Gender",
                Aliases = new[] { "gender", "sex" }
            },
            new()
            {
                Key = "address",
                Label = "Street address",
                Aliases = new[]
                {
                    "address", "residential address",
                    "home address", "street address",
                    "permanent address", "contact address"
                }
            },
            new()
            {
                Key = "city",
                Label = "City",
                Aliases = new[] { "city", "town" }
            },
            new()
            {
                Key = "state",
                Label = "State",
                Aliases = new[]
                {
                    "state", "state of residence",
                    "state of origin", "province", "region"
                }
            },
            new()
            {
                Key = "postal_code",
                Label = "Postal code",
                Aliases = new[]
                {
                    "postal code", "postcode", "zip code",
                    "zip"
                }
            },
            new()
            {
                Key = "country",
                Label = "Country",
                Aliases = new[] { "country", "country of residence" }
            },
            new()
            {
                Key = "nationality",
                Label = "Nationality",
                Aliases = new[] { "nationality", "citizenship" }
            },
            new()
            {
                Key = "occupation",
                Label = "Occupation",
                Aliases = new[]
                {
                    "occupation", "profession", "job title",
                    "employment", "current occupation"
                }
            },
            new()
            {
                Key = "organization",
                Label = "Organization",
                Aliases = new[]
                {
                    "organization", "organisation", "company",
                    "employer", "institution", "school"
                }
            },
            new()
            {
                Key = "website",
                Label = "Website",
                Aliases = new[]
                {
                    "website", "web address", "portfolio",
                    "personal website"
                }
            }
        };

    public static FormMatchResult Match(
        string input,
        Dictionary<string, string> profile)
    {
        var result = new FormMatchResult();

        if (string.IsNullOrWhiteSpace(input))
            return result;

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in input.Replace("\r\n", "\n")
                     .Split('\n'))
        {
            string line = rawLine.Trim();

            line = Regex.Replace(line, @"^[\s•*\-–—\d.)]+", "");

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Use the label before a colon or tab-separated value.
            string label = line;

            int colon = line.IndexOf(':');
            if (colon > 0)
                label = line[..colon].Trim();

            int tab = label.IndexOf('\t');
            if (tab > 0)
                label = label[..tab].Trim();

            if (label.Length == 0 || !seen.Add(label))
                continue;

            var definition = FindField(label);

            if (definition == null)
            {
                result.UnknownLabels.Add(label);
                continue;
            }

            string value = GetValue(
                definition.Key, profile);

            result.Matches.Add(new FormFieldMatch
            {
                OriginalLabel = label,
                FieldKey = definition.Key,
                FieldLabel = definition.Label,
                Value = value,
                IsMatched = !string.IsNullOrWhiteSpace(value)
            });
        }

        return result;
    }

    public static FormFieldDefinition? FindField(string label)
    {
        string normalized = Normalize(label);

        if (normalized.Length == 0)
            return null;

        foreach (var field in Fields)
        {
            foreach (string alias in field.Aliases)
            {
                string candidate = Normalize(alias);

                if (normalized == candidate ||
                    normalized.StartsWith(candidate + " ") ||
                    normalized.EndsWith(" " + candidate) ||
                    normalized.Contains(" " + candidate + " "))
                {
                    return field;
                }
            }
        }

        return null;
    }

    public static string GetValue(
        string key,
        Dictionary<string, string> profile)
    {
        if (profile.TryGetValue(key, out string? value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (key == "full_name")
        {
            string[] parts =
            {
                Read(profile, "first_name"),
                Read(profile, "middle_name"),
                Read(profile, "last_name")
            };

            return string.Join(" ",
                parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        return "";
    }

    private static string Read(
        Dictionary<string, string> profile,
        string key)
    {
        return profile.TryGetValue(key, out string? value)
            ? value?.Trim() ?? ""
            : "";
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(
                value.ToLowerInvariant(),
                @"[^a-z0-9]+",
                " ")
            .Trim();
    }
}
