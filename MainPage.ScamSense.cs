
using System.Text;
using EdgeKit.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace EdgeKit;

public partial class MainPage : ContentPage
{
    private async Task OpenScamSenseAsync()
    {
        string? input = await PromptAsync(
            "ScamSense",
            "Paste a suspicious SMS, WhatsApp message, email, or URL.",
            "Paste message or link here...",
            multiline: true);

        if (string.IsNullOrWhiteSpace(input))
            return;

        ScamSenseResult result;

        try
        {
            result = ScamSenseEngine.Analyze(input);
        }
        catch (Exception ex)
        {
            await MessageAsync("Analysis error", ex.Message);
            return;
        }

        string report = BuildScamSenseReport(result);

        var body = new VerticalStackLayout
        {
            Spacing = 13,
            Children =
            {
                Card(new VerticalStackLayout
                {
                    Spacing = 7,
                    Children =
                    {
                        Txt("RISK ASSESSMENT", 10, true, Muted),
                        Txt(result.RiskLevel.ToUpperInvariant(), 25, true, Accent),
                        Txt($"{result.Score}/100 risk score", 17, true),
                        Txt(result.Summary, 13, color: Muted)
                    }
                }, 15),

                Txt("DETECTED WARNING SIGNS", 16, true)
            }
        };

        foreach (string indicator in result.Indicators)
        {
            body.Children.Add(Card(
                Txt("⚑  " + indicator, 13),
                12));
        }

        if (result.DetectedLinks.Count > 0)
        {
            body.Children.Add(Txt("DETECTED LINKS", 16, true));

            foreach (string link in result.DetectedLinks)
            {
                body.Children.Add(Card(
                    Txt(link, 12, color: Accent),
                    12));
            }
        }

        body.Children.Add(Txt("WHAT TO DO", 16, true));

        foreach (string recommendation in result.Recommendations)
        {
            body.Children.Add(Card(
                Txt("•  " + recommendation, 13),
                12));
        }

        body.Children.Add(Txt(
            "IMPORTANT: ScamSense uses offline pattern detection. " +
            "Its score is an estimate, not a verified fraud verdict. " +
            "A low score does not guarantee that a message is safe.",
            12,
            color: Muted));

        string? choice = await ModalAsync(
            "ScamSense report",
            "Suspicious message and link analysis.",
            body,
            ("Copy full report", "copy", true),
            ("Analyze another", "again", false),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);

            await MessageAsync(
                "Report copied",
                "The ScamSense analysis has been copied.");
        }
        else if (choice == "again")
        {
            await OpenScamSenseAsync();
        }
    }

    private string BuildScamSenseReport(
        ScamSenseResult result)
    {
        var report = new StringBuilder();

        report.AppendLine("SCAMSENSE SECURITY REPORT");
        report.AppendLine("========================");
        report.AppendLine();

        report.AppendLine($"Risk level: {result.RiskLevel}");
        report.AppendLine($"Risk score: {result.Score}/100");
        report.AppendLine(result.Summary);
        report.AppendLine();

        report.AppendLine("WARNING SIGNS");

        foreach (string indicator in result.Indicators)
            report.AppendLine("- " + indicator);

        report.AppendLine();
        report.AppendLine("DETECTED LINKS");

        if (result.DetectedLinks.Count == 0)
            report.AppendLine("None detected.");

        foreach (string link in result.DetectedLinks)
            report.AppendLine("- " + link);

        report.AppendLine();
        report.AppendLine("RECOMMENDATIONS");

        foreach (string recommendation in result.Recommendations)
            report.AppendLine("- " + recommendation);

        report.AppendLine();
        report.AppendLine(
            "Disclaimer: This is an offline heuristic assessment, " +
            "not proof that a message is fraudulent or safe.");

        return report.ToString();
    }
}
