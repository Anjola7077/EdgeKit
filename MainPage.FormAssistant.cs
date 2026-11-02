
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdgeKit.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace EdgeKit;

public partial class MainPage
{
    private Dictionary<string, string> _formProfile = new();

    private async Task OpenFormAssistantAsync()
    {
        try
        {
            _formProfile = await FormVault.LoadAsync();
        }
        catch (Exception ex)
        {
            await MessageAsync("Vault unavailable", ex.Message);
            return;
        }

        string choice = await ModalAsync(
            "Form Assistant",
            "Your reusable form information, stored securely on this device.",
            new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    Card(new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            Txt("YOUR PROFILE VAULT", 10, true, Accent),
                            Txt(
                                $"{_formProfile.Count(x => !string.IsNullOrWhiteSpace(x.Value))} saved fields",
                                21, true),
                            Txt(
                                "Your information stays in Android secure storage.",
                                12, color: Muted)
                        }
                    }),
                    Card(Txt(
                        "Paste form labels to get matching information and identify missing fields.")),
                    Card(Txt(
                        "Passwords, PINs, OTPs and payment-card details are not supported."))
                }
            },
            ("Edit profile vault", "edit", true),
            ("Assist with a form", "assist", false),
            ("View saved information", "view", false),
            ("Clear profile vault", "clear", false),
            ("Close", null, false));

        switch (choice)
        {
            case "edit":
                await EditFormProfileAsync();
                break;

            case "assist":
                await AssistWithFormAsync();
                break;

            case "view":
                await ViewFormProfileAsync();
                break;

            case "clear":
                await ClearFormProfileAsync();
                break;
        }
    }

    private async Task EditFormProfileAsync()
    {
        while (true)
        {
            var options = FormAssistantEngine.Fields
                .Select(f => f.Label)
                .Append("Finish editing")
                .ToArray();

            string? selected = await ChooseAsync(
                "Edit profile",
                "Choose the information you want to add or update.",
                options);

            if (selected == null || selected == "Finish editing")
                break;

            var field = FormAssistantEngine.Fields
                .FirstOrDefault(f => f.Label == selected);

            if (field == null)
                continue;

            string current = FormAssistantEngine.GetValue(
                field.Key, _formProfile);

            string? value = await PromptAsync(
                field.Label,
                "Enter the value to store in your private vault. Leave empty to clear it.",
                field.Label,
                current);

            if (value == null)
                continue;

            if (string.IsNullOrWhiteSpace(value))
                _formProfile.Remove(field.Key);
            else
                _formProfile[field.Key] = value.Trim();

            try
            {
                await FormVault.SaveAsync(_formProfile);
            }
            catch (Exception ex)
            {
                await MessageAsync("Could not save", ex.Message);
                return;
            }
        }

        await MessageAsync(
            "Profile updated",
            "Your changes have been saved to the secure local vault.");
    }

    private async Task ViewFormProfileAsync()
    {
        if (!_formProfile.Any(x =>
            !string.IsNullOrWhiteSpace(x.Value)))
        {
            await MessageAsync(
                "Vault is empty",
                "Add your information using Edit profile vault.");
            return;
        }

        var body = new VerticalStackLayout
        {
            Spacing = 10
        };

        foreach (var field in FormAssistantEngine.Fields)
        {
            string value = FormAssistantEngine.GetValue(
                field.Key, _formProfile);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 8
            };

            row.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    Txt(field.Label.ToUpperInvariant(), 10, true, Muted),
                    Txt(value, 14, true)
                }
            }, 0, 0);

            row.Add(Btn("Copy", async () =>
            {
                await Clipboard.Default.SetTextAsync(value);
                await MessageAsync(
                    "Copied",
                    $"{field.Label} copied to clipboard.");
            }), 1, 0);

            body.Children.Add(Card(row, 12));
        }

        string report = BuildVaultReport();

        string? choice = await ModalAsync(
            "Saved information",
            "Only fields with saved values are shown.",
            Scroll(body),
            ("Copy all information", "copy", true),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);
            await MessageAsync(
                "Copied",
                "Your saved profile information has been copied.");
        }
    }

    private string BuildVaultReport()
    {
        var builder = new StringBuilder();

        builder.AppendLine("EDGEKIT FORM ASSISTANT");
        builder.AppendLine("SAVED PROFILE INFORMATION");
        builder.AppendLine();

        foreach (var field in FormAssistantEngine.Fields)
        {
            string value = FormAssistantEngine.GetValue(
                field.Key, _formProfile);

            if (!string.IsNullOrWhiteSpace(value))
                builder.AppendLine($"{field.Label}: {value}");
        }

        return builder.ToString();
    }

    private async Task ClearFormProfileAsync()
    {
        string? choice = await ModalAsync(
            "Clear profile vault?",
            "This permanently removes the saved Form Assistant profile from this device.",
            IconBox("!", 58, true),
            ("Clear vault", "clear", true),
            ("Cancel", null, false));

        if (choice != "clear")
            return;

        try
        {
            FormVault.Clear();
            _formProfile.Clear();

            await MessageAsync(
                "Vault cleared",
                "Your saved Form Assistant profile has been removed.");
        }
        catch (Exception ex)
        {
            await MessageAsync("Could not clear vault", ex.Message);
        }
    }

    private async Task AssistWithFormAsync()
    {
        if (!_formProfile.Any(x =>
            !string.IsNullOrWhiteSpace(x.Value)))
        {
            string? setup = await ModalAsync(
                "Profile needed",
                "You need to add some information before matching form fields.",
                IconBox("◈", 58, true),
                ("Set up profile", "setup", true),
                ("Cancel", null, false));

            if (setup != "setup")
                return;

            await EditFormProfileAsync();

            if (!_formProfile.Any(x =>
                !string.IsNullOrWhiteSpace(x.Value)))
                return;
        }

        string? input = await PromptAsync(
            "Paste form labels",
            "Paste the field labels from your form, one per line. You can include colons.",
            "First Name:\nSurname:\nEmail Address:\nPhone Number:\nResidential Address:",
            multiline: true);

        if (string.IsNullOrWhiteSpace(input))
            return;

        FormMatchResult result = FormAssistantEngine.Match(
            input, _formProfile);

        await ShowFormMatchResultsAsync(result);
    }

    private async Task ShowFormMatchResultsAsync(
        FormMatchResult result)
    {
        if (result.Matches.Count == 0 &&
            result.UnknownLabels.Count == 0)
        {
            await MessageAsync(
                "No fields detected",
                "Paste one or more form labels, each on a separate line.");
            return;
        }

        var body = new VerticalStackLayout
        {
            Spacing = 12
        };

        body.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Txt("MATCHING OVERVIEW", 10, true, Accent),
                Txt($"{result.MatchedCount} fields ready", 22, true),
                Txt(
                    $"{result.MissingCount} fields need information",
                    13, color: Muted)
            }
        }));

        foreach (var match in result.Matches)
        {
            bool available = match.IsMatched;

            var content = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    Txt(match.OriginalLabel, 15, true),
                    Txt(
                        "RECOGNISED AS: " + match.FieldLabel.ToUpperInvariant(),
                        10, true, Muted),
                    Txt(
                        available ? match.Value : "No saved value",
                        14,
                        available,
                        available ? Ink : Muted)
                }
            };

            if (available)
            {
                content.Children.Add(Btn(
                    "Copy value",
                    async () =>
                    {
                        await Clipboard.Default.SetTextAsync(match.Value);
                        await MessageAsync(
                            "Copied",
                            $"{match.FieldLabel} copied to clipboard.");
                    },
                    true));
            }
            else
            {
                content.Children.Add(Btn(
                    "Add this information",
                    async () =>
                    {
                        await EditSingleFormFieldAsync(match.FieldKey);
                    }));
            }

            body.Children.Add(Card(content, 14));
        }

        if (result.UnknownLabels.Count > 0)
        {
            var unknown = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    Txt("UNRECOGNISED LABELS", 11, true, Accent)
                }
            };

            foreach (string label in result.UnknownLabels)
                unknown.Children.Add(Txt("• " + label, 13, color: Muted));

            body.Children.Add(Card(unknown, 14));
        }

        string report = BuildMatchReport(result);

        string? choice = await ModalAsync(
            "Form matches",
            "Review each value before pasting it into your form.",
            Scroll(body),
            ("Copy matched fields", "copy", true),
            ("Done", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);

            await MessageAsync(
                "Copied",
                "Available field values have been copied as labelled text.");
        }
    }

    private string BuildMatchReport(FormMatchResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("EDGEKIT FORM ASSISTANT");
        builder.AppendLine("FORM FIELD MATCHES");
        builder.AppendLine();

        foreach (var match in result.Matches)
        {
            if (match.IsMatched)
                builder.AppendLine(
                    $"{match.OriginalLabel}: {match.Value}");
        }

        return builder.ToString();
    }

    private async Task EditSingleFormFieldAsync(string key)
    {
        var field = FormAssistantEngine.Fields
            .FirstOrDefault(f => f.Key == key);

        if (field == null)
            return;

        string current = FormAssistantEngine.GetValue(
            field.Key, _formProfile);

        string? value = await PromptAsync(
            field.Label,
            "Enter the value to save in your secure profile.",
            field.Label,
            current);

        if (value == null)
            return;

        if (string.IsNullOrWhiteSpace(value))
            _formProfile.Remove(field.Key);
        else
            _formProfile[field.Key] = value.Trim();

        try
        {
            await FormVault.SaveAsync(_formProfile);

            await MessageAsync(
                "Information saved",
                $"{field.Label} has been updated. Run the form matcher again to use it.");
        }
        catch (Exception ex)
        {
            await MessageAsync("Could not save", ex.Message);
        }
    }
}
