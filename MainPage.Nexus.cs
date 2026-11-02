
using EdgeKit.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace EdgeKit;

public partial class MainPage : ContentPage
{
    private async Task OpenNexusAsync()
    {
        string? input = await PromptAsync(
            "NEXUS",
            "Paste text, data, a URL, or any information you want analyzed.",
            "Paste information here...",
            multiline: true);

        if (string.IsNullOrWhiteSpace(input))
            return;

        string report;

        try
        {
            report = NexusEngine.Analyze(input);
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "Analysis failed",
                ex.Message);
            return;
        }

        var reportEditor = new Editor
        {
            Text = report,
            IsReadOnly = true,
            AutoSize = EditorAutoSizeOption.Disabled,
            HeightRequest = 390,
            BackgroundColor = SurfaceAlt,
            TextColor = Ink,
            FontSize = 13
        };

        string? choice = await ModalAsync(
            "NEXUS report",
            "Information detected from your input.",
            reportEditor,
            ("Copy report", "copy", true),
            ("Analyze another", "again", false),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);

            await MessageAsync(
                "Copied",
                "The NEXUS analysis report has been copied.");
        }
        else if (choice == "again")
        {
            await OpenNexusAsync();
        }
    }
}
