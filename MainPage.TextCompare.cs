
namespace EdgeKit;

public partial class MainPage
{
    private sealed record DiffLine(string Type, string Text);

    private async Task OpenTextCompareAsync()
    {
        string? original = await PromptAsync(
            "Original text",
            "Paste the first version.",
            "Original text...",
            multiline: true);

        if (original == null)
            return;

        string? modified = await PromptAsync(
            "Modified text",
            "Paste the version you want to compare.",
            "Modified text...",
            multiline: true);

        if (modified == null)
            return;

        var oldLines = original.Replace("\r\n", "\n")
            .Split('\n');

        var newLines = modified.Replace("\r\n", "\n")
            .Split('\n');

        if (oldLines.Length > 500 || newLines.Length > 500)
        {
            await MessageAsync(
                "Text too large",
                "For this version, each text block is limited to 500 lines.");

            return;
        }

        var differences = CompareLines(oldLines, newLines);

        int added = differences.Count(x => x.Type == "added");
        int removed = differences.Count(x => x.Type == "removed");
        int unchanged = differences.Count(x => x.Type == "same");

        string output = string.Join(
            Environment.NewLine,
            differences.Select(x =>
                x.Type switch
                {
                    "added" => "+ " + x.Text,
                    "removed" => "- " + x.Text,
                    _ => "  " + x.Text
                }));

        var resultContent = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star)
                    },
                    Children =
                    {
                        BuildDiffStat("ADDED", added, Color.FromArgb("#299D72"), 0),
                        BuildDiffStat("REMOVED", removed, Color.FromArgb("#E05A5A"), 1),
                        BuildDiffStat("UNCHANGED", unchanged, Muted, 2)
                    }
                },

                new Editor
                {
                    Text = output,
                    IsReadOnly = true,
                    AutoSize = EditorAutoSizeOption.Disabled,
                    HeightRequest = 300,
                    BackgroundColor = SurfaceAlt,
                    TextColor = Ink,
                    FontSize = 13
                }
            }
        };

        string? choice = await ModalAsync(
            "Comparison result",
            "Lines marked + were added. Lines marked - were removed.",
            resultContent,
            ("Copy comparison", "copy", true),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(output);

            await MessageAsync(
                "Copied",
                "Your comparison has been copied.");
        }
    }

    private View BuildDiffStat(
        string title,
        int count,
        Color color,
        int column)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                Txt(title, 9, true, Muted),
                Txt(count.ToString(), 21, true, color)
            }
        };

        var card = Card(stack, 9);

        Grid.SetColumn(card, column);

        return card;
    }

    private List<DiffLine> CompareLines(
        string[] oldLines,
        string[] newLines)
    {
        int m = oldLines.Length;
        int n = newLines.Length;

        int[,] dp = new int[m + 1, n + 1];

        // Build the longest common subsequence table.
        for (int i = m - 1; i >= 0; i--)
        {
            for (int j = n - 1; j >= 0; j--)
            {
                if (oldLines[i] == newLines[j])
                {
                    dp[i, j] = 1 + dp[i + 1, j + 1];
                }
                else
                {
                    dp[i, j] = Math.Max(
                        dp[i + 1, j],
                        dp[i, j + 1]);
                }
            }
        }

        var result = new List<DiffLine>();

        int x = 0;
        int y = 0;

        while (x < m && y < n)
        {
            if (oldLines[x] == newLines[y])
            {
                result.Add(new DiffLine("same", oldLines[x]));
                x++;
                y++;
            }
            else if (dp[x + 1, y] >= dp[x, y + 1])
            {
                result.Add(new DiffLine("removed", oldLines[x]));
                x++;
            }
            else
            {
                result.Add(new DiffLine("added", newLines[y]));
                y++;
            }
        }

        while (x < m)
        {
            result.Add(new DiffLine("removed", oldLines[x]));
            x++;
        }

        while (y < n)
        {
            result.Add(new DiffLine("added", newLines[y]));
            y++;
        }

        return result;
    }
}
