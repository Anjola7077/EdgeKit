
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ZXing;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace EdgeKit;

public partial class MainPage
{
    private async Task OpenQRBarcodeAsync()
    {
        string? value = await PromptAsync(
            "QR & Barcode Studio",
            "Enter the text, website URL, product number or other content to encode.",
            "Enter content...",
            multiline: true);

        if (value == null)
            return;

        value = value.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            await MessageAsync(
                "Empty content",
                "Enter something to generate a code.");

            return;
        }

        var formatPicker = new Picker
        {
            Title = "Select code format",
            ItemsSource = new List<string>
            {
                "QR Code",
                "Code 128",
                "EAN-13"
            },
            SelectedIndex = 0,
            TextColor = Ink,
            BackgroundColor = SurfaceAlt
        };

        var formatContent = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Txt("Choose the type of code to create.", 13, false, Muted),
                formatPicker
            }
        };

        string? selection = await ModalAsync(
            "Choose format",
            "Select a QR or barcode format.",
            formatContent,
            ("Generate", "generate", true),
            ("Cancel", null, false));

        if (selection != "generate")
            return;

        string selectedFormat =
            formatPicker.SelectedItem?.ToString() ?? "QR Code";

        BarcodeFormat format;
        int width;
        int height;

        switch (selectedFormat)
        {
            case "Code 128":
                format = BarcodeFormat.CODE_128;
                width = 1000;
                height = 400;
                break;

            case "EAN-13":
                if (value.Length != 12 && value.Length != 13)
                {
                    await MessageAsync(
                        "Invalid EAN-13 number",
                        "EAN-13 requires 12 or 13 numeric digits.");

                    return;
                }

                if (!value.All(char.IsDigit))
                {
                    await MessageAsync(
                        "Invalid EAN-13 number",
                        "EAN-13 can only contain numeric digits.");

                    return;
                }

                format = BarcodeFormat.EAN_13;
                width = 1000;
                height = 400;
                break;

            default:
                format = BarcodeFormat.QR_CODE;
                width = 700;
                height = 700;
                break;
        }

        try
        {
            string fileName =
                $"edgekit-code-{DateTime.Now:yyyyMMdd-HHmmss}.png";

            string filePath = Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

            // Generate PNG using ZXing.Net and Android Bitmap.
            BarcodeImageGenerator.WritePng(
                value,
                filePath,
                format,
                width,
                height,
                3);

            var preview = new Image
            {
                Source = ImageSource.FromFile(filePath),
                Aspect = Aspect.AspectFit,
                HeightRequest = 260,
                WidthRequest = 280,
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

                    Txt(selectedFormat, 15, true, Ink),

                    Txt(
                        value.Length > 100
                            ? value[..100] + "..."
                            : value,
                        12,
                        false,
                        Muted),

                    Txt(
                        "Your code has been generated locally.",
                        11,
                        false,
                        Muted)
                }
            };

            string? action = await ModalAsync(
                "Code generated",
                "Your QR code or barcode is ready.",
                resultContent,
                ("Share PNG", "share", true),
                ("Copy content", "copy", false),
                ("Close", null, false));

            if (action == "share")
            {
                await Share.Default.RequestAsync(
                    new ShareFileRequest
                    {
                        Title = "Share generated code",
                        File = new ShareFile(filePath)
                    });
            }
            else if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(value);

                await MessageAsync(
                    "Copied",
                    "The encoded content has been copied.");
            }
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "Generation failed",
                $"Could not generate this code.\n\n{ex.Message}");
        }
    }
}
