using System.Security.Cryptography;

namespace EdgeKit;

public partial class MainPage
{
    private async Task OpenFileInspectorAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select a file to inspect"
                });

            if (file == null)
                return;

            await InspectFileAsync(file);
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "File selection failed",
                ex.Message);
        }
    }

    private async Task InspectFileAsync(FileResult file)
    {
        string hash;
        long fileSize = 0;

        try
        {
            using var stream = await file.OpenReadAsync();

            using var sha256 = IncrementalHash.CreateHash(
                HashAlgorithmName.SHA256);

            byte[] buffer = new byte[81920];

            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                sha256.AppendData(buffer, 0, bytesRead);
                fileSize += bytesRead;
            }

            hash = Convert.ToHexString(sha256.GetHashAndReset());
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "Inspection failed",
                $"Unable to read this file.\n\n{ex.Message}");

            return;
        }

        string extension = Path.GetExtension(file.FileName);

        string contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "Unknown"
            : file.ContentType;

        string size = FormatFileSize(fileSize);

        string report =
            $"FILE INSPECTION REPORT\n\n" +
            $"Name: {file.FileName}\n" +
            $"Extension: {(string.IsNullOrEmpty(extension) ? "None" : extension)}\n" +
            $"MIME type: {contentType}\n" +
            $"File size: {size}\n" +
            $"Size in bytes: {fileSize}\n\n" +
            $"SHA-256:\n{hash}";

        var resultContent = new VerticalStackLayout
        {
            Spacing = 13,

            Children =
            {
                Txt("FILE INFORMATION", 11, true, Muted),

                Card(
                    new VerticalStackLayout
                    {
                        Spacing = 9,

                        Children =
                        {
                            BuildFileInfoRow("Name", file.FileName),
                            BuildFileInfoRow("Extension",
                                string.IsNullOrEmpty(extension) ? "None" : extension),
                            BuildFileInfoRow("MIME type", contentType),
                            BuildFileInfoRow("File size", size),
                            BuildFileInfoRow("Bytes", fileSize.ToString("N0"))
                        }
                    },
                    14),

                Txt("SHA-256 FINGERPRINT", 11, true, Muted),

                new Border
                {
                    BackgroundColor = SurfaceAlt,
                    Stroke = Colors.Transparent,
                    Padding = 12,

                    Content = new VerticalStackLayout
                    {
                        Spacing = 8,

                        Children =
                        {
                            Txt(hash, 11, true, Ink),

                            Txt(
                                "64 hexadecimal characters",
                                10,
                                false,
                                Muted)
                        }
                    }
                },

                Txt(
                    "The fingerprint was calculated locally. EdgeKit does not upload your file.",
                    11,
                    false,
                    Muted)
            }
        };

        string? action = await ModalAsync(
            "File inspection",
            "Your file analysis is complete.",
            resultContent,
            ("Copy report", "copy", true),
            ("Verify hash", "verify", false),
            ("Close", null, false));

        if (action == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);

            await MessageAsync(
                "Copied",
                "The file inspection report has been copied.");
        }
        else if (action == "verify")
        {
            await VerifyFileHashAsync(hash);
        }
    }

    private View BuildFileInfoRow(string title, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };

        var titleLabel = Txt(title, 12, false, Muted);

        var valueLabel = new Label
        {
            Text = value,
            FontSize = 12,
            TextColor = Ink,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.Fill,
            LineBreakMode = LineBreakMode.WordWrap
        };

        Grid.SetColumn(valueLabel, 1);

        grid.Children.Add(titleLabel);
        grid.Children.Add(valueLabel);

        return grid;
    }

    private async Task VerifyFileHashAsync(string actualHash)
    {
        string? expectedHash = await PromptAsync(
            "Verify SHA-256",
            "Paste the expected SHA-256 fingerprint.",
            "Enter expected hash...");

        if (expectedHash == null)
            return;

        expectedHash = expectedHash.Trim();

        if (expectedHash.Length != 64 ||
            !expectedHash.All(Uri.IsHexDigit))
        {
            await MessageAsync(
                "Invalid fingerprint",
                "A valid SHA-256 fingerprint must contain exactly 64 hexadecimal characters.");

            return;
        }

        bool matches = string.Equals(
            actualHash,
            expectedHash,
            StringComparison.OrdinalIgnoreCase);

        if (matches)
        {
            await MessageAsync(
                "Fingerprint matches",
                "The file's SHA-256 fingerprint matches the expected value.");
        }
        else
        {
            await MessageAsync(
                "Fingerprint mismatch",
                "The file's SHA-256 fingerprint does not match the expected value.");
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };

        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
