
using System;
using System.IO;
using System.Linq;
using ZXing;

namespace EdgeKit;

public partial class MainPage
{
    private async Task OpenQRCommunicationAsync()
    {
        var picker = new Picker
        {
            Title = "Choose QR type",
            ItemsSource = new List<string>
            {
                "Email",
                "SMS",
                "WhatsApp",
                "Contact Card"
            },
            SelectedIndex = 0,
            TextColor = Ink,
            BackgroundColor = SurfaceAlt
        };

        var content = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Txt("CREATE A COMMUNICATION QR CODE", 11, true, Muted),
                picker
            }
        };

        string? choice = await ModalAsync(
            "QR Communication Studio",
            "Choose what happens when someone scans your code.",
            content,
            ("Continue", "continue", true),
            ("Cancel", null, false));

        if (choice != "continue")
            return;

        switch (picker.SelectedItem?.ToString())
        {
            case "Email":
                await CreateEmailQRAsync();
                break;

            case "SMS":
                await CreateSMSQRAsync();
                break;

            case "WhatsApp":
                await CreateWhatsAppQRAsync();
                break;

            case "Contact Card":
                await CreateContactQRAsync();
                break;
        }
    }

    // EMAIL QR

    private async Task CreateEmailQRAsync()
    {
        string? email = await PromptAsync(
            "Email recipient",
            "Enter the destination email address.",
            "example@email.com");

        if (email == null)
            return;

        email = email.Trim();

        if (!email.Contains('@') || !email.Contains('.'))
        {
            await MessageAsync(
                "Invalid email",
                "Enter a valid email address.");

            return;
        }

        string? subject = await PromptAsync(
            "Email subject",
            "Enter an optional subject.",
            "Hello!");

        if (subject == null)
            return;

        string? message = await PromptAsync(
            "Email message",
            "Enter an optional message.",
            "Hello, I would like to contact you.",
            multiline: true);

        if (message == null)
            return;

        string payload =
            "mailto:" + email +
            "?subject=" + Uri.EscapeDataString(subject) +
            "&body=" + Uri.EscapeDataString(message);

        await GenerateCommunicationQRAsync(
            "Email QR Code",
            "Opens an email composer.",
            payload);
    }

    // SMS QR

    private async Task CreateSMSQRAsync()
    {
        string? phone = await PromptAsync(
            "Phone number",
            "Enter the recipient's phone number, preferably with country code.",
            "+2348012345678");

        if (phone == null)
            return;

        phone = phone.Trim();

        if (string.IsNullOrWhiteSpace(phone) ||
            !phone.Any(char.IsDigit))
        {
            await MessageAsync(
                "Invalid number",
                "Enter a valid phone number.");

            return;
        }

        string? message = await PromptAsync(
            "SMS message",
            "Enter the message to prefill.",
            "Hello, I would like to contact you.",
            multiline: true);

        if (message == null)
            return;

        string payload =
            "SMSTO:" + phone + ":" + message;

        await GenerateCommunicationQRAsync(
            "SMS QR Code",
            "Opens the messaging app with a prefilled message.",
            payload);
    }

    // WHATSAPP QR

    private async Task CreateWhatsAppQRAsync()
    {
        string? phone = await PromptAsync(
            "WhatsApp number",
            "Enter the international number without spaces or the + sign.",
            "2348012345678");

        if (phone == null)
            return;

        string digits = new string(
            phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 7 || digits.Length > 15)
        {
            await MessageAsync(
                "Invalid number",
                "Enter a valid international phone number.");

            return;
        }

        string? message = await PromptAsync(
            "WhatsApp message",
            "Enter an optional prefilled message.",
            "Hello! I would like to connect with you.",
            multiline: true);

        if (message == null)
            return;

        string payload =
            "https://wa.me/" + digits +
            "?text=" + Uri.EscapeDataString(message);

        await GenerateCommunicationQRAsync(
            "WhatsApp QR Code",
            "Opens a WhatsApp conversation.",
            payload);
    }

    // CONTACT CARD / VCARD

    private async Task CreateContactQRAsync()
    {
        string? fullName = await PromptAsync(
            "Full name",
            "Enter the contact's full name.",
            "John Doe");

        if (fullName == null)
            return;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            await MessageAsync(
                "Name required",
                "Enter a contact name.");

            return;
        }

        string? phone = await PromptAsync(
            "Phone number",
            "Enter a phone number. This field is optional.",
            "+2348012345678");

        if (phone == null)
            return;

        string? email = await PromptAsync(
            "Email address",
            "Enter an email address. This field is optional.",
            "john@example.com");

        if (email == null)
            return;

        string? organization = await PromptAsync(
            "Organization",
            "Enter a company or organization. Optional.",
            "EdgeKit");

        if (organization == null)
            return;

        string? website = await PromptAsync(
            "Website",
            "Enter a website URL. Optional.",
            "https://example.com");

        if (website == null)
            return;

        if (string.IsNullOrWhiteSpace(phone) &&
            string.IsNullOrWhiteSpace(email))
        {
            await MessageAsync(
                "Contact information required",
                "Add at least a phone number or email address.");

            return;
        }

        string escapedName = EscapeVCard(fullName.Trim());

        var lines = new List<string>
        {
            "BEGIN:VCARD",
            "VERSION:3.0",
            "FN:" + escapedName
        };

        string[] nameParts = fullName.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string familyName = nameParts.Length > 1
            ? EscapeVCard(nameParts[^1])
            : "";

        string givenName = nameParts.Length > 1
            ? EscapeVCard(string.Join(" ", nameParts[..^1]))
            : escapedName;

        lines.Add($"N:{familyName};{givenName};;;");

        if (!string.IsNullOrWhiteSpace(phone))
            lines.Add("TEL;TYPE=CELL:" + EscapeVCard(phone.Trim()));

        if (!string.IsNullOrWhiteSpace(email))
            lines.Add("EMAIL:" + EscapeVCard(email.Trim()));

        if (!string.IsNullOrWhiteSpace(organization))
            lines.Add("ORG:" + EscapeVCard(organization.Trim()));

        if (!string.IsNullOrWhiteSpace(website))
        {
            string url = website.Trim();

            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                (parsed.Scheme == Uri.UriSchemeHttp ||
                 parsed.Scheme == Uri.UriSchemeHttps))
            {
                lines.Add("URL:" + EscapeVCard(url));
            }
        }

        lines.Add("END:VCARD");

        string payload = string.Join("\r\n", lines);

        await GenerateCommunicationQRAsync(
            "Contact Card QR",
            "Scanners can import this contact using vCard format.",
            payload);
    }

    private string EscapeVCard(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }

    // SHARED QR GENERATOR

    private async Task GenerateCommunicationQRAsync(
        string title,
        string description,
        string payload)
    {
        try
        {
            string fileName =
                $"edgekit-{DateTime.Now:yyyyMMdd-HHmmss}.png";

            string filePath = Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

            BarcodeImageGenerator.WritePng(
                payload,
                filePath,
                BarcodeFormat.QR_CODE,
                900,
                900,
                3);

            var preview = new Image
            {
                Source = ImageSource.FromFile(filePath),
                Aspect = Aspect.AspectFit,
                HeightRequest = 270,
                WidthRequest = 270,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = Colors.White
            };

            var resultContent = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Border
                    {
                        BackgroundColor = Colors.White,
                        Stroke = Colors.Transparent,
                        Padding = 12,
                        Content = preview
                    },

                    Txt(title, 16, true, Ink),

                    Txt(description, 12, false, Muted),

                    Txt(
                        "Generated locally on your device.",
                        11,
                        false,
                        Muted)
                }
            };

            string? action = await ModalAsync(
                "QR code ready",
                "Your communication QR code has been generated.",
                resultContent,
                ("Share QR image", "share", true),
                ("Copy QR content", "copy", false),
                ("Close", null, false));

            if (action == "share")
            {
                await Share.Default.RequestAsync(
                    new ShareFileRequest
                    {
                        Title = title,
                        File = new ShareFile(filePath)
                    });
            }
            else if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(payload);

                await MessageAsync(
                    "Copied",
                    "The QR code content has been copied.");
            }
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "QR generation failed",
                ex.Message);
        }
    }
}
