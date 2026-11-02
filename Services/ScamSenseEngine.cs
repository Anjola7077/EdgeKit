
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace EdgeKit.Services;

public sealed class ScamSenseResult
{
    public int Score { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public string Summary { get; set; } = "";
    public List<string> Indicators { get; set; } = new();
    public List<string> DetectedLinks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public static class ScamSenseEngine
{
    private sealed class Signal
    {
        public string Name { get; set; } = "";
        public string Explanation { get; set; } = "";
        public int Weight { get; set; }
    }

    private static readonly Regex UrlRegex = new(
        @"(?:(?:https?://)|(?:www\.))[^\s<>""']+",
        RegexOptions.IgnoreCase);

    private static readonly string[] Shorteners =
    {
        "bit.ly", "tinyurl.com", "t.co", "goo.gl",
        "ow.ly", "is.gd", "buff.ly", "cutt.ly",
        "shorturl.at", "rb.gy", "tiny.cc"
    };

    private static readonly string[] UrgencyPhrases =
    {
        "act now", "immediately", "urgent", "right now",
        "within 24 hours", "limited time", "last chance",
        "expires today", "final warning", "final notice",
        "account will be blocked", "account will be suspended",
        "account has been suspended", "verify immediately"
    };

    private static readonly string[] ThreatPhrases =
    {
        "you will be arrested", "legal action will be taken",
        "your account will be closed", "your account will be blocked",
        "your account will be suspended", "warrant for your arrest",
        "police will come", "your account has been compromised"
    };

    private static readonly string[] CredentialPhrases =
    {
        "share your otp", "send your otp", "enter your otp",
        "provide your otp", "send your password",
        "share your password", "confirm your password",
        "provide your pin", "share your pin",
        "send your verification code", "share your verification code",
        "enter your card details", "send your card details",
        "verify your bank details"
    };

    private static readonly string[] PaymentPhrases =
    {
        "send money", "transfer money", "make a payment",
        "pay now", "pay immediately", "processing fee",
        "activation fee", "claim fee", "release fee",
        "send your bank details", "pay to receive",
        "deposit now", "wallet recharge"
    };

    private static readonly string[] PrizePhrases =
    {
        "you have won", "you won", "congratulations",
        "claim your prize", "free gift", "cash reward",
        "you have been selected", "lucky winner",
        "unclaimed reward", "free data", "free airtime"
    };

    private static readonly string[] SecrecyPhrases =
    {
        "keep this confidential", "do not tell anyone",
        "don't tell anyone", "keep this secret",
        "do not share this message", "act privately"
    };

    private static readonly string[] ImpersonationPhrases =
    {
        "customer support", "bank security department",
        "account verification team", "technical support",
        "official representative", "government agent",
        "central bank", "security department",
        "account department"
    };

    public static ScamSenseResult Analyze(string input)
    {
        var result = new ScamSenseResult();

        if (string.IsNullOrWhiteSpace(input))
        {
            result.Summary = "No message or link was provided.";
            return result;
        }

        string text = input.Trim();
        string lower = text.ToLowerInvariant();

        var signals = new List<Signal>();

        CheckPhraseGroup(
            lower, UrgencyPhrases, signals,
            "Urgency or pressure",
            "The message creates pressure to act quickly.",
            12);

        CheckPhraseGroup(
            lower, ThreatPhrases, signals,
            "Threatening language",
            "The message uses threats or frightening consequences to influence the recipient.",
            22);

        CheckPhraseGroup(
            lower, CredentialPhrases, signals,
            "Sensitive information request",
            "The message asks for credentials, PINs, OTPs, or other sensitive information.",
            30);

        CheckPhraseGroup(
            lower, PaymentPhrases, signals,
            "Payment or money request",
            "The message requests a transfer, payment, deposit, or fee.",
            22);

        CheckPhraseGroup(
            lower, PrizePhrases, signals,
            "Unexpected reward or prize",
            "The message uses a prize, gift, or reward claim that may be designed to attract attention.",
            14);

        CheckPhraseGroup(
            lower, SecrecyPhrases, signals,
            "Secrecy request",
            "The sender encourages secrecy, which can prevent independent verification.",
            15);

        CheckPhraseGroup(
            lower, ImpersonationPhrases, signals,
            "Authority or organisation claim",
            "The message uses organisational or official-sounding language. Verify the sender independently.",
            8);

        var links = ExtractLinks(text);
        result.DetectedLinks = links;

        foreach (string link in links)
        {
            AnalyzeLink(link, signals);
        }

        int score = CalculateScore(signals);

        result.Score = score;
        result.RiskLevel = GetRiskLevel(score);

        result.Indicators = signals
            .Select(s => $"{s.Name}: {s.Explanation}")
            .Distinct()
            .ToList();

        result.Summary = GetSummary(result.RiskLevel, score);

        result.Recommendations = BuildRecommendations(
            signals, links);

        if (result.Indicators.Count == 0)
        {
            result.Indicators.Add(
                "No recognised warning patterns were detected. " +
                "This does not establish that the message is safe.");
        }

        return result;
    }

    private static void CheckPhraseGroup(
        string text,
        string[] phrases,
        List<Signal> signals,
        string name,
        string explanation,
        int weight)
    {
        string? matched = phrases.FirstOrDefault(
            phrase => text.Contains(
                phrase,
                StringComparison.OrdinalIgnoreCase));

        if (matched == null)
            return;

        signals.Add(new Signal
        {
            Name = name,
            Explanation = $"{explanation} Example detected: \"{matched}\".",
            Weight = weight
        });
    }

    private static List<string> ExtractLinks(string text)
    {
        return UrlRegex.Matches(text)
            .Select(m => m.Value.Trim().TrimEnd(
                '.', ',', ';', ':', ')', ']', '}', '!', '?'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private static void AnalyzeLink(
        string link,
        List<Signal> signals)
    {
        string candidate = link.StartsWith(
            "www.", StringComparison.OrdinalIgnoreCase)
            ? "https://" + link
            : link;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri))
        {
            signals.Add(new Signal
            {
                Name = "Unrecognised link structure",
                Explanation = $"The link could not be reliably interpreted: {link}",
                Weight = 12
            });

            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            signals.Add(new Signal
            {
                Name = "Unexpected URL scheme",
                Explanation = "The link uses a non-standard web protocol.",
                Weight = 20
            });

            return;
        }

        string host = uri.Host.TrimEnd('.').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(host))
            return;

        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            signals.Add(new Signal
            {
                Name = "Insecure HTTP link",
                Explanation = $"The link to {host} does not use HTTPS.",
                Weight = 8
            });
        }

        if (Shorteners.Contains(host))
        {
            signals.Add(new Signal
            {
                Name = "Shortened URL",
                Explanation = $"The link uses {host}, which hides the final destination.",
                Weight = 16
            });
        }

        if (IPAddress.TryParse(host, out _))
        {
            signals.Add(new Signal
            {
                Name = "IP-address-based link",
                Explanation = "The destination uses an IP address instead of a normal domain name.",
                Weight = 24
            });
        }

        if (host.StartsWith("xn--", StringComparison.OrdinalIgnoreCase) ||
            host.Split('.').Any(part =>
                part.StartsWith("xn--", StringComparison.OrdinalIgnoreCase)))
        {
            signals.Add(new Signal
            {
                Name = "Encoded domain name",
                Explanation = "The domain contains Punycode encoding. Verify its actual displayed identity.",
                Weight = 20
            });
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            signals.Add(new Signal
            {
                Name = "Unusual URL credentials",
                Explanation = "The URL contains text before the @ symbol that may disguise the actual destination.",
                Weight = 22
            });
        }

        if (host.Length > 60)
        {
            signals.Add(new Signal
            {
                Name = "Unusually long domain",
                Explanation = "The destination domain is unusually long and deserves closer inspection.",
                Weight = 10
            });
        }

        if (host.Split('.').Length >= 5)
        {
            signals.Add(new Signal
            {
                Name = "Deep subdomain structure",
                Explanation = "The domain contains many subdomain levels. Check which organisation actually owns it.",
                Weight = 8
            });
        }

        if (uri.Port != 80 && uri.Port != 443 &&
            !uri.IsDefaultPort)
        {
            signals.Add(new Signal
            {
                Name = "Unusual web port",
                Explanation = $"The URL uses port {uri.Port} instead of a standard HTTP/HTTPS port.",
                Weight = 12
            });
        }
    }

    private static int CalculateScore(List<Signal> signals)
    {
        if (signals.Count == 0)
            return 0;

        // Prevent several similar phrases from inflating the score.
        int total = signals
            .Select(s => s.Weight)
            .Distinct()
            .OrderByDescending(weight => weight)
            .Take(5)
            .Sum();

        return Math.Min(total, 100);
    }

    private static string GetRiskLevel(int score)
    {
        if (score >= 60)
            return "Very high";

        if (score >= 35)
            return "High";

        if (score >= 15)
            return "Moderate";

        return "Low";
    }

    private static string GetSummary(string level, int score)
    {
        return level switch
        {
            "Very high" =>
                $"Very high concern. Multiple strong warning patterns were detected. Risk score: {score}/100.",

            "High" =>
                $"High concern. Several suspicious patterns were detected. Risk score: {score}/100.",

            "Moderate" =>
                $"Some warning patterns were detected. Verify the sender and information independently. Risk score: {score}/100.",

            _ =>
                $"Few recognised warning patterns were detected. This is not proof of safety. Risk score: {score}/100."
        };
    }

    private static List<string> BuildRecommendations(
        List<Signal> signals,
        List<string> links)
    {
        var recommendations = new List<string>();

        if (signals.Any(s =>
            s.Name == "Sensitive information request"))
        {
            recommendations.Add(
                "Never share an OTP, PIN, password, or verification code with a sender.");
        }

        if (signals.Any(s =>
            s.Name == "Payment or money request"))
        {
            recommendations.Add(
                "Verify payment requests using a contact channel you already trust.");
        }

        if (links.Count > 0)
        {
            recommendations.Add(
                "Do not open suspicious links. Visit the organisation's official website independently.");
        }

        if (signals.Any(s =>
            s.Name == "Shortened URL"))
        {
            recommendations.Add(
                "Treat shortened links cautiously because their final destinations are hidden.");
        }

        if (signals.Any(s =>
            s.Name == "Threatening language" ||
            s.Name == "Urgency or pressure"))
        {
            recommendations.Add(
                "Pause before acting. Pressure and threats are not proof that a message is genuine.");
        }

        recommendations.Add(
            "Contact the organisation through its independently verified phone number or official app.");

        recommendations.Add(
            "Do not reply, transfer money, or provide personal information until the claim is verified.");

        return recommendations.Distinct().ToList();
    }
}
