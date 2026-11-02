
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EdgeKit;

public partial class MainPage
{
    // ---------------------------------------------------------
    // ENCODING LAB
    // ---------------------------------------------------------

    private async Task OpenEncodingLabAsync()
    {
        string? input = await PromptAsync(
            "Encoding Lab",
            "Enter or paste the text you want to process.",
            "Enter text or encoded data",
            multiline: true);

        if (input == null)
            return;

        while (true)
        {
            string? operation = await ChooseAsync(
                "Encoding Lab",
                "Choose an encoding or conversion tool.",
                new[]
                {
                    "Base64 Encode",
                    "Base64 Decode",
                    "URL Encode",
                    "URL Decode",
                    "HTML Encode",
                    "HTML Decode",
                    "Unicode Escape",
                    "Unicode Decode",
                    "Text to HEX",
                    "HEX to Text",
                    "Format JSON",
                    "Minify JSON",
                    "Decode JWT Payload"
                });

            if (operation == null)
                return;

            try
            {
                string result = operation switch
                {
                    "Base64 Encode" =>
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(input)),

                    "Base64 Decode" =>
                        Encoding.UTF8.GetString(
                            Convert.FromBase64String(input.Trim())),

                    "URL Encode" =>
                        Uri.EscapeDataString(input),

                    "URL Decode" =>
                        Uri.UnescapeDataString(input),

                    "HTML Encode" =>
                        WebUtility.HtmlEncode(input),

                    "HTML Decode" =>
                        WebUtility.HtmlDecode(input),

                    "Unicode Escape" =>
                        EncodeUnicode(input),

                    "Unicode Decode" =>
                        DecodeUnicode(input),

                    "Text to HEX" =>
                        Convert.ToHexString(
                            Encoding.UTF8.GetBytes(input)),

                    "HEX to Text" =>
                        Encoding.UTF8.GetString(
                            Convert.FromHexString(
                                input.Replace(" ", "")
                                     .Replace("-", "")
                                     .Replace(":", "")
                                     .Trim())),

                    "Format JSON" =>
                        ProcessJson(input, true),

                    "Minify JSON" =>
                        ProcessJson(input, false),

                    "Decode JWT Payload" =>
                        DecodeJwtPayload(input),

                    _ => input
                };

                await ShowEncodingResultAsync(
                    operation,
                    result);

                bool useResult = await ConfirmAsync(
                    "Use this result?",
                    "Replace your working text with the processed result?");

                if (useResult)
                    input = result;
            }
            catch (Exception ex)
            {
                string error = ex switch
                {
                    FormatException =>
                        "The supplied data has an invalid format.",

                    JsonException =>
                        "The JSON is invalid. Check its syntax and try again.",

                    _ =>
                        ex.Message
                };

                await MessageAsync(
                    "Conversion failed",
                    error);
            }
        }
    }

    // ---------------------------------------------------------
    // UNICODE
    // ---------------------------------------------------------

    private string EncodeUnicode(string input)
    {
        var result = new StringBuilder();

        foreach (char character in input)
        {
            result.Append("\\u");
            result.Append(
                ((int)character).ToString("X4"));
        }

        return result.ToString();
    }

    private string DecodeUnicode(string input)
    {
        return Regex.Replace(
            input,
            @"\\u([0-9a-fA-F]{4})",
            match =>
            {
                int value = Convert.ToInt32(
                    match.Groups[1].Value,
                    16);

                return ((char)value).ToString();
            });
    }

    // ---------------------------------------------------------
    // JSON
    // ---------------------------------------------------------

    private string ProcessJson(
        string input,
        bool indented)
    {
        using var document = JsonDocument.Parse(input);

        return JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions
            {
                WriteIndented = indented
            });
    }

    // ---------------------------------------------------------
    // JWT PAYLOAD DECODER
    // ---------------------------------------------------------

    private string DecodeJwtPayload(string token)
    {
        string[] parts = token.Trim().Split('.');

        if (parts.Length != 3)
        {
            throw new FormatException(
                "A JWT should contain three dot-separated sections.");
        }

        string payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        payload = payload.PadRight(
            payload.Length + (4 - payload.Length % 4) % 4,
            '=');

        byte[] bytes = Convert.FromBase64String(payload);

        string json = Encoding.UTF8.GetString(bytes);

        using var document = JsonDocument.Parse(json);

        return JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    // ---------------------------------------------------------
    // RESULT DISPLAY
    // ---------------------------------------------------------

    private async Task ShowEncodingResultAsync(
        string operation,
        string result)
    {
        string? choice = await ModalAsync(
            operation,
            "Your converted output.",
            new Editor
            {
                Text = result,
                IsReadOnly = true,
                AutoSize = EditorAutoSizeOption.Disabled,
                HeightRequest = 220,
                BackgroundColor = SurfaceAlt,
                TextColor = Ink,
                FontSize = 14
            },
            ("Copy result", "copy", true),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(result);

            await MessageAsync(
                "Copied",
                "The converted result was copied to your clipboard.");
        }
    }
}
