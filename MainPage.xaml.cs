
using System.Text.Json;
using EdgeKit.Models;
using EdgeKit.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls.Shapes;
using System.Text.RegularExpressions;

namespace EdgeKit;

public partial class MainPage : ContentPage
{
    private readonly EdgeStore _store = new();
    private readonly Grid _root = new();
    private readonly ContentView _content = new();
    private readonly Grid _nav = new();
    private readonly Border _navBorder;

    private List<QuickAction> _actions = new();
    private List<EdgeProfile> _profiles = new();

    private string _activeProfile = "default";
    private string _currentTab = "Home";
    private bool _busy;

    private Color Accent => IsDark
    ? Color.FromArgb("#B8C7FF")
    : Color.FromArgb("#3159B7");

    private Color AccentContainer => IsDark
        ? Color.FromArgb("#303F67")
        : Color.FromArgb("#DFE6FF");

    private Color Surface => IsDark
        ? Color.FromArgb("#1C1F27")
        : Colors.White;

    private Color SurfaceAlt => IsDark
        ? Color.FromArgb("#252934")
        : Color.FromArgb("#EEF0F8");

    private Color Background => IsDark
        ? Color.FromArgb("#101218")
        : Color.FromArgb("#F5F6FC");

    private Color Ink => IsDark
        ? Color.FromArgb("#F0F0F7")
        : Color.FromArgb("#191B24");

    private Color Muted => IsDark
        ? Color.FromArgb("#BFC3D0")
        : Color.FromArgb("#626575");

    private bool IsDark
{
    get
    {
        var app = Application.Current;

        if (app == null)
            return false;

        if (app.UserAppTheme == AppTheme.Dark)
            return true;

        if (app.UserAppTheme == AppTheme.Light)
            return false;

        return app.RequestedTheme == AppTheme.Dark;
    }
}
    public MainPage()
    {
        BackgroundColor = Background;

        _root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        _content.Opacity = 1;
        _root.Add(_content, 0, 0);

        _navBorder = new Border
        {
            BackgroundColor = Surface,
            Stroke = new SolidColorBrush(
                IsDark
                    ? Color.FromArgb("#343844")
                    : Color.FromArgb("#E4E6EF")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(30)
            },
            Padding = new Thickness(7, 7, 7, 7),
            Margin = new Thickness(14, 4, 14, 12),
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(
                    IsDark ? Colors.Black : Color.FromArgb("#220D1838")),
                Offset = new Point(0, 5),
                Radius = 18,
                Opacity = 0.2f
            },
            Content = _nav
        };

        _root.Add(_navBorder, 0, 1);
        Content = _root;

        BuildNavigation();
        ShowTab("Home");

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (_, _) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BackgroundColor = Background;
                    _navBorder.BackgroundColor = Surface;
                    BuildNavigation();
                    ShowTab(_currentTab);
                });
            };
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _store.InitializeAsync();
            _actions = await _store.GetActionsAsync();
            _profiles = await _store.GetProfilesAsync();

            if (!_profiles.Any(p => p.Id == _activeProfile))
            {
                _activeProfile = _profiles.FirstOrDefault()?.Id ?? "default";
            }

            ShowTab(_currentTab);
        }
        catch (Exception ex)
        {
            await MessageAsync("Storage problem", ex.Message);
        }
    }

    // ---------------------------------------------------------
    // DESIGN SYSTEM
    // ---------------------------------------------------------

    private Label Txt(
        string value,
        double size = 14,
        bool bold = false,
        Color? color = null)
    {
        return new Label
        {
            Text = value,
            FontSize = size,
            FontFamily = bold ? "OpenSansSemibold" : "OpenSansRegular",
            TextColor = color ?? Ink,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private Border Card(View content, double padding = 18)
    {
        return new Border
        {
            BackgroundColor = Surface,
            Stroke = new SolidColorBrush(
                IsDark
                    ? Color.FromArgb("#30343F")
                    : Color.FromArgb("#E6E8F0")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(25)
            },
            Padding = padding,
            Content = content
        };
    }

    private Border IconBox(
        string glyph,
        double size = 54,
        bool highlighted = false)
    {
        var label = Txt(
            glyph,
            size * 0.43,
            true,
            highlighted ? Accent : Ink);

        label.HorizontalTextAlignment = TextAlignment.Center;
        label.VerticalTextAlignment = TextAlignment.Center;

        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = highlighted ? AccentContainer : SurfaceAlt,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(size * 0.32)
            },
            Content = label
        };
    }
    
    
    private Button Btn(
        string title,
        Action action,
        bool primary = false)
    {
        var button = new Button
        {
            Text = title,
            FontFamily = "OpenSansSemibold",
            FontSize = 14,
            CornerRadius = 19,
            Padding = new Thickness(18, 12),
            MinimumHeightRequest = 46,
            BackgroundColor = primary ? Accent : SurfaceAlt,
            TextColor = primary
                ? (IsDark ? Color.FromArgb("#151A28") : Colors.White)
                : Ink,
            BorderWidth = 0
        };

        button.Pressed += async (_, _) =>
        {
            await button.ScaleTo(0.96, 70, Easing.CubicOut);
        };

        button.Released += async (_, _) =>
        {
            await button.ScaleTo(1, 110, Easing.SpringOut);
        };

        button.Clicked += (_, _) => action();
        return button;
    }

    private VerticalStackLayout Page()
    {
        return new VerticalStackLayout
        {
            Padding = new Thickness(20, 24, 20, 28),
            Spacing = 18
        };
    }

    private ScrollView Scroll(View view)
    {
        return new ScrollView
        {
            Content = view,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never
        };
    }

    private Grid TwoColumns()
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 13,
            RowSpacing = 13
        };
    }

    // ---------------------------------------------------------
    // ANIMATED NAVIGATION
    // ---------------------------------------------------------

    private void BuildNavigation()
    {
        _nav.Children.Clear();
        _nav.ColumnDefinitions.Clear();

        string[] tabs = { "Home", "Tools", "Saved", "Settings" };
        string[] glyphs = { "⌂", "⊞", "▤", "⚙" };

        for (int i = 0; i < tabs.Length; i++)
        {
            _nav.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            string tab = tabs[i];
            bool selected = tab == _currentTab;

            var icon = Txt(glyphs[i], 25, true,
                selected ? Accent : Muted);

            icon.HorizontalTextAlignment = TextAlignment.Center;

            var label = Txt(tab, 11, true,
                selected ? Accent : Muted);

            label.HorizontalTextAlignment = TextAlignment.Center;

            var stack = new VerticalStackLayout
            {
                Spacing = 2,
                Padding = new Thickness(2, 5),
                HorizontalOptions = LayoutOptions.Fill,
                Children = { icon, label }
            };

            var button = new Border
            {
                BackgroundColor = selected
                    ? AccentContainer
                    : Colors.Transparent,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(23)
                },
                Padding = new Thickness(2, 1),
                Content = stack
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                await button.ScaleTo(0.92, 65, Easing.CubicOut);
                await button.ScaleTo(1, 130, Easing.SpringOut);
                ShowTab(tab);
            };

            button.GestureRecognizers.Add(tap);

            Grid.SetColumn(button, i);
            _nav.Add(button);
        }
    }

    private async void ShowTab(string tab)
{
    if (_busy)
        return;

    _busy = true;
    _currentTab = tab;

    try
    {
        BuildNavigation();

        await _content.FadeTo(0.35, 70, Easing.CubicOut);

        _content.Content = tab switch
        {
            "Home" => BuildHome(),
            "Tools" => BuildTools(),
            "Saved" => BuildSaved(),
            "Settings" => BuildSettings(),
            _ => BuildHome()
        };

        _content.Opacity = 0.35;

        await _content.FadeTo(1, 160, Easing.CubicOut);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Navigation error: {ex}");

        _content.Opacity = 1;
    }
    finally
    {
        _busy = false;
    }
}

    private View PageHeader(string eyebrow, string title, string subtitle)
    {
        return new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Txt(eyebrow.ToUpperInvariant(), 11, true, Accent),
                Txt(title, 31, true),
                Txt(subtitle, 14, color: Muted)
            }
        };
    }

    private View SectionHeader(string title, string? trailing = null, Action? action = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(Txt(title, 20, true), 0, 0);

        if (trailing != null && action != null)
            grid.Add(Btn(trailing, action, true), 1, 0);

        return grid;
    }

    // ---------------------------------------------------------
    // HOME
    // ---------------------------------------------------------

    
private View BuildHome()
{
    var page = Page();

    // HEADER
    var header = new Grid
    {
        ColumnDefinitions =
        {
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto)
        }
    };

    header.Add(new VerticalStackLayout
    {
        Spacing = 4,
        Children =
        {
            Txt("YOUR PERSONAL EDGE", 11, true, Accent),
            Txt("EdgeKit", 34, true)
        }
    }, 0, 0);

    var themeButton = new Border
    {
        WidthRequest = 54,
        HeightRequest = 54,
        BackgroundColor = Surface,
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(
            IsDark
                ? Color.FromArgb("#343844")
                : Color.FromArgb("#E1E4EF")),
        StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(19)
        },
        Content = new Grid
        {
            Children =
            {
                new Label
                {
                    Text = IsDark ? "☼" : "☾",
                    FontSize = 27,
                    FontFamily = "OpenSansSemibold",
                    TextColor = Accent,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        }
    };

    var themeTap = new TapGestureRecognizer();
    themeTap.Tapped += async (_, _) =>
    {
        await themeButton.RotateTo(25, 90);
        ToggleTheme();
        await themeButton.RotateTo(0, 90);
    };

    themeButton.GestureRecognizers.Add(themeTap);

    header.Add(themeButton, 1, 0);
    page.Children.Add(header);

    page.Children.Add(Txt(
        "Your favourite actions, ready when you are.",
        15,
        color: Muted));

    // ACTIVE PROFILE
    var profile = _profiles.FirstOrDefault(
        p => p.Id == _activeProfile);

    var profileContent = new Grid
    {
        ColumnDefinitions =
        {
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto)
        },
        ColumnSpacing = 13
    };

    profileContent.Add(IconBox("◈", 54, true), 0, 0);

    profileContent.Add(new VerticalStackLayout
    {
        Spacing = 4,
        VerticalOptions = LayoutOptions.Center,
        Children =
        {
            Txt("CURRENT PROFILE", 10, true, Muted),
            Txt(profile?.Name ?? "Everyday", 19, true),
            Txt(
                profile?.Description ?? "Your daily shortcuts",
                12,
                color: Muted)
        }
    }, 1, 0);

    profileContent.Add(
        Btn("Change", () => ShowTab("Settings")), 2, 0);

    page.Children.Add(Card(profileContent));

    // QUICK ACCESS
    page.Children.Add(SectionHeader(
        "Quick access",
        "+ Add",
        async () => await AddActionAsync()));

    var actions = _actions
        .Where(a => a.ProfileId == _activeProfile)
        .OrderByDescending(a => a.IsPinned)
        .ThenByDescending(a => a.CreatedAt)
        .ToList();

    if (actions.Count == 0)
    {
        page.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                IconBox("＋", 60, true),
                Txt("Build your own toolkit", 20, true),
                Txt(
                    "Add calculators, saved text, notes and developer utilities to this profile.",
                    14,
                    color: Muted),
                Btn(
                    "Create first shortcut",
                    async () => await AddActionAsync(),
                    true)
            }
        }));
    }
    else
    {
        var grid = TwoColumns();

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var tile = BuildActionTile(action);

            Grid.SetRow(tile, i / 2);
            Grid.SetColumn(tile, i % 2);

            grid.Add(tile);
        }

        page.Children.Add(grid);
    }

    // TOOLKIT CARD
    page.Children.Add(Card(new VerticalStackLayout
    {
        Spacing = 12,
        Children =
        {
            Txt("YOUR TOOLKIT", 11, true, Accent),
            Txt("Everything useful, in one place.", 19, true),
            Txt(
                "Explore practical utilities for calculations, text, files and everyday tasks.",
                13,
                color: Muted),
            Btn(
                "Explore tools",
                () => ShowTab("Tools"),
                true)
        }
    }));

    // PRIVACY CARD
    page.Children.Add(Card(new HorizontalStackLayout
    {
        Spacing = 12,
        Children =
        {
            IconBox("▣", 42),
            new VerticalStackLayout
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    Txt("Private by design", 14, true),
                    Txt(
                        "Your saved items stay on this device.",
                        12,
                        color: Muted)
                }
            }
        }
    }, 14));

    return Scroll(page);
}

    private View BuildActionTile(QuickAction action)
    {
        string glyph = action.Type switch
        {
            "note" => "▤",
            "snippet" => "⌘",
            "calculator" => "÷",
            "json" => "{ }",
            "uuid" => "#",
            _ => action.Icon
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 13,
            Children =
            {
                IconBox(glyph, 58, true),
                Txt(action.Title, 16, true),
                Txt(action.Type.ToUpperInvariant(), 10, true, Muted)
            }
        };

        var tile = Card(stack, 16);
        tile.MinimumHeightRequest = 155;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            await tile.ScaleTo(0.97, 70, Easing.CubicOut);
            await tile.ScaleTo(1, 130, Easing.SpringOut);
            await OpenActionAsync(action);
        };
        tile.GestureRecognizers.Add(tap);

        return tile;
    }

    // ---------------------------------------------------------
    // TOOLS
    // ---------------------------------------------------------

    private View BuildTools()
    {
        var page = Page();

        page.Children.Add(PageHeader(
            "YOUR TOOLBOX",
            "Tools",
            "Small utilities, without the clutter."));
            
            page.Children.Add(ToolRow(
    "Aa", "Text Lab",
    "Transform, analyse and clean text.",
    async () => await OpenTextLabAsync()));
    
    
    page.Children.Add(ToolRow(
    "❃",
    "NEXUS",
    "Analyze text, identify information and detect data formats.",
    async () => await OpenNexusAsync()));

    
    page.Children.Add(ToolRow(
    "◉",
    "Device Inspector",
    "Inspect device specifications and display.",
    async () => await OpenDeviceInspectorAsync()));
    
    page.Children.Add(ToolRow(
    "║▌║",
    "QR & Barcode Studio",
    "Create QR codes and product barcodes offline.",
    async () => await OpenQRBarcodeAsync()));
    
    page.Children.Add(ToolRow(
    "⚑",
    "ScamSense",
    "Inspect suspicious messages, payment requests and links.",
    async () => await OpenScamSenseAsync()));
    
    page.Children.Add(ToolRow(
    "▧",
    "Form Assistant",
    "Store reusable information and fill forms faster.",
    async () => await OpenFormAssistantAsync()));

    
    page.Children.Add(ToolRow(
    "✉",
    "QR Communication Studio",
    "Create email, SMS, WhatsApp and contact QR codes.",
    async () => await OpenQRCommunicationAsync()));
    
    page.Children.Add(ToolRow(
    "⌕",
    "File Inspector",
    "Inspect files and verify SHA-256 integrity.",
    async () => await OpenFileInspectorAsync()));
    
    
    page.Children.Add(ToolRow(
    "✎",
    "Text Compare",
    "Compare two versions and find every change.",
    async () => await OpenTextCompareAsync()));
    
    page.Children.Add(ToolRow(
    "⛨",
    "Password Studio",
    "Generate secure passwords and check their strength.",
    async () => await OpenPasswordGeneratorAsync()));

        page.Children.Add(ToolRow(
            "÷", "Calculator", "Evaluate arithmetic expressions.",
            async () => await OpenCalculatorAsync()));
            
            page.Children.Add(ToolRow(
    "⌁",
    "Network Toolkit",
    "Inspect connections, IP addresses, DNS and TCP ports.",
    async () => await OpenNetworkToolsAsync()));

        page.Children.Add(ToolRow(
            "{ }", "JSON formatter", "Format and validate JSON.",
            async () => await FormatJsonAsync()));
            
            page.Children.Add(ToolRow(
    "⌘", "Encoding Lab",
    "Encode, decode and inspect different data formats.",
    async () => await OpenEncodingLabAsync()));
       
        page.Children.Add(ToolRow(
            "#", "UUID generator", "Generate and copy unique identifiers.",
            async () => await GenerateUuidAsync()));
 
page.Children.Add(ToolRow(
    "⇄", "Unit Converter",
    "Convert length, mass, temperature, storage and more.",
    async () => await OpenUnitConverterAsync()));

        page.Children.Add(ToolRow(
    "⌘", "Saved text", "Keep reusable commands and text.",
    () =>
    {
        ShowTab("Saved");
        return Task.CompletedTask;
    }));

        page.Children.Add(ToolRow(
            "▤", "Quick note", "Capture a thought without leaving the app.",
            async () => await AddNoteAsync()));

        page.Children.Add(ToolRow(
            "▣", "Clipboard", "Read and copy the current clipboard text.",
            async () => await ShowClipboardAsync()));

        page.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Txt("Designed for quick actions", 18, true),
                Txt("More utilities can be added without turning the app into a crowded toolbox.",
                    13, color: Muted)
            }
        }));

        return Scroll(page);
    }

    private View ToolRow(
        string glyph,
        string title,
        string description,
        Func<Task> run)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 14
        };

        grid.Add(IconBox(glyph, 58, true), 0, 0);

        grid.Add(new VerticalStackLayout
        {
            Spacing = 5,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Txt(title, 16, true),
                Txt(description, 12, color: Muted)
            }
        }, 1, 0);

        grid.Add(Txt("›", 30, true, Accent), 2, 0);

        var card = Card(grid, 15);

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            await card.ScaleTo(0.985, 65, Easing.CubicOut);
            await card.ScaleTo(1, 120, Easing.SpringOut);
            await run();
        };
        card.GestureRecognizers.Add(tap);

        return card;
    }

    // ---------------------------------------------------------
// TEXT LAB
// ---------------------------------------------------------

private async Task OpenTextLabAsync()
{
    string? input = await PromptAsync(
        "Text Lab",
        "Enter or paste the text you want to process.",
        "Your text goes here...",
        multiline: true);

    if (string.IsNullOrEmpty(input))
        return;

    while (true)
    {
        string? operation = await ChooseAsync(
            "Text Lab",
            "Choose an operation.",
            new[]
            {
                "Text statistics",
                "UPPERCASE",
                "lowercase",
                "Title Case",
                "Sentence case",
                "Reverse text",
                "Clean whitespace",
                "Remove duplicate lines",
                "Sort lines A-Z",
                "Find and replace",
                "Extract emails",
                "Extract URLs",
                "Extract phone numbers"
            });

        if (operation == null)
            return;

        string result = input;

        switch (operation)
        {
            case "Text statistics":
                await ShowTextStatisticsAsync(input);
                continue;

            case "UPPERCASE":
                result = input.ToUpperInvariant();
                break;

            case "lowercase":
                result = input.ToLowerInvariant();
                break;

            case "Title Case":
                result = System.Globalization.CultureInfo
                    .CurrentCulture.TextInfo.ToTitleCase(
                        input.ToLowerInvariant());
                break;

            case "Sentence case":
                result = ToSentenceCase(input);
                break;

            case "Reverse text":
                result = new string(input.Reverse().ToArray());
                break;

            case "Clean whitespace":
                result = string.Join(
                    Environment.NewLine,
                    input.Split('\n')
                        .Select(line => Regex.Replace(
                            line.Trim(), @"[ \t]+", " "))
                        .Where(line => !string.IsNullOrWhiteSpace(line)));
                break;

            case "Remove duplicate lines":
                result = string.Join(
                    Environment.NewLine,
                    input.Replace("\r\n", "\n")
                        .Split('\n')
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                break;

            case "Sort lines A-Z":
                result = string.Join(
                    Environment.NewLine,
                    input.Replace("\r\n", "\n")
                        .Split('\n')
                        .OrderBy(line => line,
                            StringComparer.OrdinalIgnoreCase));
                break;

            case "Find and replace":
            {
                string? search = await PromptAsync(
                    "Find text",
                    "What text should be replaced?",
                    "Search text");

                if (string.IsNullOrEmpty(search))
                    continue;

                string? replacement = await PromptAsync(
                    "Replace with",
                    "Enter the replacement text.",
                    "Replacement");

                if (replacement == null)
                    continue;

                result = input.Replace(
                    search,
                    replacement,
                    StringComparison.OrdinalIgnoreCase);
                break;
            }

            case "Extract emails":
                result = ExtractMatches(
                    input,
                    @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
                    true);
                break;

            case "Extract URLs":
                result = ExtractMatches(
                    input,
                    @"https?://[^\s<>""']+",
                    true);
                break;

            case "Extract phone numbers":
                result = ExtractMatches(
                    input,
                    @"(?<!\w)(?:\+?\d[\d\s().-]{6,}\d)(?!\w)",
                    false);
                break;
        }

        await ShowTextResultAsync(operation, result);

        bool useResult = await ConfirmAsync(
            "Use processed text?",
            "Would you like to apply this result to your working text?");

        if (useResult)
            input = result;
    }
}

private string ToSentenceCase(string input)
{
    var text = input.ToLowerInvariant().ToCharArray();
    bool capitalize = true;

    for (int i = 0; i < text.Length; i++)
    {
        if (capitalize && char.IsLetter(text[i]))
        {
            text[i] = char.ToUpperInvariant(text[i]);
            capitalize = false;
        }

        if (text[i] is '.' or '!' or '?')
            capitalize = true;
    }

    return new string(text);
}

private string ExtractMatches(
    string input,
    string pattern,
    bool ignoreCase)
{
    var options = ignoreCase
        ? RegexOptions.IgnoreCase
        : RegexOptions.None;

    var matches = Regex.Matches(input, pattern, options)
        .Select(match => match.Value.Trim())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    string result = string.Join(Environment.NewLine, matches);

    return string.IsNullOrEmpty(result)
        ? "No matches found."
        : result;
}

private async Task ShowTextStatisticsAsync(string input)
{
    int words = Regex.Matches(input, @"\b[\p{L}\p{N}_'-]+\b").Count;
    int characters = input.Length;
    int charactersWithoutSpaces = input.Count(c => !char.IsWhiteSpace(c));
    int lines = input.Replace("\r\n", "\n").Split('\n').Length;
    int sentences = Regex.Matches(input, @"[.!?]+(?=\s|$)").Count;

    string statistics =
        $"Words: {words}\n\n" +
        $"Characters: {characters}\n\n" +
        $"Characters without spaces: {charactersWithoutSpaces}\n\n" +
        $"Lines: {lines}\n\n" +
        $"Sentences: {sentences}";

    await ModalAsync(
        "Text statistics",
        "A breakdown of your text.",
        Txt(statistics, 16, true),
        ("Close", null, true));
}

private async Task ShowTextResultAsync(
    string operation,
    string result)
{
    string? choice = await ModalAsync(
        operation,
        "Your processed text.",
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
            "Processed text copied to your clipboard.");
    }
}

    // ---------------------------------------------------------
    // SAVED TEXT AND NOTES
    // ---------------------------------------------------------

    private View BuildSaved()
    {
        var page = Page();

        page.Children.Add(PageHeader(
            "YOUR PRIVATE LIBRARY",
            "Saved items",
            "Notes and snippets, organised in one place."));

        page.Children.Add(Btn(
            "＋  New saved item",
            async () => await AddSnippetAsync(),
            true));

        var saved = _actions
            .Where(a => a.Type is "note" or "snippet")
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        if (saved.Count == 0)
        {
            page.Children.Add(Card(new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    IconBox("▤", 60, true),
                    Txt("Nothing saved yet", 19, true),
                    Txt("Save useful text, commands, and notes here for quick access.",
                        14, color: Muted),
                    Btn("Create a note",
                        async () => await AddNoteAsync(), true)
                }
            }));
        }

        foreach (var item in saved)
        {
            page.Children.Add(BuildSavedCard(item));
        }

        return Scroll(page);
    }

    private View BuildSavedCard(QuickAction item)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 11,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        IconBox(item.Type == "note" ? "▤" : "⌘", 42, true),
                        new VerticalStackLayout
                        {
                            Spacing = 3,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                Txt(item.Title, 16, true),
                                Txt(item.Type == "note" ? "NOTE" : "SAVED TEXT",
                                    10, true, Muted)
                            }
                        }
                    }
                },
                Txt(
                    string.IsNullOrWhiteSpace(item.Content)
                        ? "No content yet"
                        : item.Content,
                    13, color: Muted)
            }
        };

        var buttons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 7
        };

        buttons.Add(Btn("Edit", async () =>
            await EditSavedAsync(item)), 0, 0);

        buttons.Add(Btn("Copy", async () =>
        {
            await Clipboard.Default.SetTextAsync(item.Content ?? "");
            await MessageAsync("Copied", "Text copied to your clipboard.");
        }), 1, 0);

        buttons.Add(Btn("Delete", async () =>
            await DeleteActionAsync(item)), 2, 0);

        stack.Children.Add(buttons);

        return Card(stack, 15);
    }

    private async Task AddNoteAsync()
    {
        string? title = await PromptAsync(
            "New note", "Give your note a short title", "Note title");

        if (string.IsNullOrWhiteSpace(title))
            return;

        string? content = await PromptAsync(
            "Write your note", "Your note is saved on this device.",
            "Enter note content", multiline: true);

        if (content == null)
            return;

        _actions.Add(new QuickAction
        {
            Title = title.Trim(),
            Content = content,
            Type = "note",
            ProfileId = _activeProfile,
            Icon = "▤"
        });

        await _store.SaveActionsAsync(_actions);
        ShowTab("Saved");
    }

    private async Task AddSnippetAsync()
    {
        string? title = await PromptAsync(
            "New saved text", "Name this reusable text",
            "Snippet name");

        if (string.IsNullOrWhiteSpace(title))
            return;

        string? content = await PromptAsync(
            "Saved text", "Enter the text you want to reuse",
            "Text content", multiline: true);

        if (content == null)
            return;

        _actions.Add(new QuickAction
        {
            Title = title.Trim(),
            Content = content,
            Type = "snippet",
            ProfileId = _activeProfile,
            Icon = "⌘"
        });

        await _store.SaveActionsAsync(_actions);
        ShowTab("Saved");
    }

    private async Task EditSavedAsync(QuickAction item)
    {
        string? title = await PromptAsync(
            "Edit title", "Update the item's name",
            "Title", item.Title);

        if (string.IsNullOrWhiteSpace(title))
            return;

        string? content = await PromptAsync(
            "Edit content", "Update the saved content",
            "Content", item.Content, multiline: true);

        if (content == null)
            return;

        item.Title = title.Trim();
        item.Content = content;
        item.CreatedAt = DateTime.Now;

        await _store.SaveActionsAsync(_actions);
        ShowTab("Saved");
    }

    private async Task DeleteActionAsync(QuickAction action)
    {
        bool confirmed = await ConfirmAsync(
            "Delete this item?",
            $"“{action.Title}” will be removed from your saved items.");

        if (!confirmed)
            return;

        _actions.Remove(action);
        await _store.SaveActionsAsync(_actions);
        ShowTab("Saved");
    }

    // ---------------------------------------------------------
    // SETTINGS
    // ---------------------------------------------------------

    private View BuildSettings()
    {
        var page = Page();

        page.Children.Add(PageHeader(
            "MAKE IT YOURS",
            "Settings",
            "Your theme, your profiles, your workflow."));

        page.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        IconBox(IsDark ? "☾" : "☼", 54, true),
                        new VerticalStackLayout
                        {
                            Spacing = 4,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                Txt("Appearance", 17, true),
                                Txt(IsDark ? "Dark theme enabled" : "Light theme enabled",
                                    13, color: Muted)
                            }
                        }
                    }
                },
                Btn("Switch appearance", ToggleTheme, true)
            }
        }));

        page.Children.Add(Txt("Activity profiles", 21, true));
        page.Children.Add(Txt(
            "Keep different shortcut collections for different activities.",
            13, color: Muted));

        foreach (var profile in _profiles)
        {
            bool active = profile.Id == _activeProfile;

            page.Children.Add(Card(new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            IconBox(active ? "✓" : "◈", 46, active),
                            new VerticalStackLayout
                            {
                                Spacing = 4,
                                VerticalOptions = LayoutOptions.Center,
                                Children =
                                {
                                    Txt(profile.Name, 17, true),
                                    Txt(profile.Description, 12, color: Muted)
                                }
                            }
                        }
                    },
                    Btn(
                        active ? "Currently active" : "Switch to profile",
                        () =>
                        {
                            _activeProfile = profile.Id;
                            ShowTab("Home");
                        },
                        active)
                }
            }));
        }

          
         


        page.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 7,
            Children =
            {
                Txt("About EdgeKit", 18, true),
                Txt("Version 1.1", 13, color: Muted),
                Txt("An offline-first personal toolkit.",
                    13, color: Muted)
            }
        }));

        return Scroll(page);
    }

    private void ToggleTheme()
    {
        if (Application.Current == null)
            return;

        Application.Current.UserAppTheme =
            IsDark ? AppTheme.Light : AppTheme.Dark;

        BackgroundColor = Background;
        BuildNavigation();
        ShowTab(_currentTab);
    }
    
    



    // ---------------------------------------------------------
    // SHORTCUTS
    // ---------------------------------------------------------

    private async Task AddActionAsync()
    {
        string? title = await PromptAsync(
            "New shortcut", "Give this shortcut a name", "Shortcut name");

        if (string.IsNullOrWhiteSpace(title))
            return;

        string? type = await ChooseAsync(
            "Choose a shortcut",
            "What should this shortcut open?",
            new[]
            {
                "Note",
                "Saved text",
                "Calculator",
                "JSON formatter",
                "UUID generator"
            });

        if (type == null)
            return;

        var action = new QuickAction
        {
            Title = title.Trim(),
            ProfileId = _activeProfile,
            Type = type switch
            {
                "Note" => "note",
                "Saved text" => "snippet",
                "Calculator" => "calculator",
                "JSON formatter" => "json",
                "UUID generator" => "uuid",
                _ => "note"
            },
            Icon = type switch
            {
                "Note" => "▤",
                "Saved text" => "⌘",
                "Calculator" => "÷",
                "JSON formatter" => "{ }",
                "UUID generator" => "#",
                _ => "•"
            }
        };

        if (action.Type is "note" or "snippet")
        {
            string? content = await PromptAsync(
                "Add content", "Enter the content for this shortcut",
                "Content", multiline: true);

            if (content == null)
                return;

            action.Content = content;
        }

        _actions.Add(action);
        await _store.SaveActionsAsync(_actions);
        ShowTab("Home");
    }

    private async Task OpenActionAsync(QuickAction action)
    {
        switch (action.Type)
        {
            case "note":
            case "snippet":
                await EditSavedAsync(action);
                break;

            case "calculator":
                await OpenCalculatorAsync();
                break;

            case "json":
                await FormatJsonAsync();
                break;

            case "uuid":
                await GenerateUuidAsync();
                break;

            default:
                await MessageAsync("Shortcut", "This shortcut type is not supported yet.");
                break;
        }
    }

     private View BuildModalButtons(
    (string Label, string? Value, bool Primary)[] buttons,
    Action<string?> onSelected)
{
    var layout = new VerticalStackLayout
    {
        Spacing = 10
    };

    foreach (var item in buttons)
    {
        var button = new Button
        {
            Text = item.Label,
            HeightRequest = 50,
            CornerRadius = 16,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = item.Primary
                ? Accent
                : IsDark
                    ? Color.FromArgb("#292E39")
                    : Color.FromArgb("#F0F2F8"),
            TextColor = item.Primary
                ? Colors.White
                : IsDark
                    ? Color.FromArgb("#F1F3F9")
                    : Color.FromArgb("#252936"),
            BorderWidth = 0
        };

        string? selectedValue = item.Value;

        button.Clicked += (_, _) =>
            onSelected(selectedValue);

        layout.Children.Add(button);
    }

    return layout;
}

    // ---------------------------------------------------------
    // UTILITIES
    // ---------------------------------------------------------

    private async Task GenerateUuidAsync()
    {
        string value = Guid.NewGuid().ToString();

        string? result = await ModalAsync(
            "UUID generated",
            "A new unique identifier is ready.",
            Txt(value, 15, true),
            ("Copy UUID", "copy", true),
            ("Generate another", "again", false),
            ("Close", null, false));

        if (result == "copy")
        {
            await Clipboard.Default.SetTextAsync(value);
            await MessageAsync("Copied", "UUID copied to clipboard.");
        }
        else if (result == "again")
        {
            await GenerateUuidAsync();
        }
    }

    private async Task FormatJsonAsync()
    {
        string? input = await PromptAsync(
            "JSON formatter", "Paste JSON to format and validate it.",
            "Paste JSON", multiline: true);

        if (string.IsNullOrWhiteSpace(input))
            return;

        try
        {
            using var document = JsonDocument.Parse(input);

            string result = JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions { WriteIndented = true });

            string? choice = await ModalAsync(
                "Formatted JSON",
                "Valid JSON, formatted for readability.",
                new Editor
                {
                    Text = result,
                    IsReadOnly = true,
                    AutoSize = EditorAutoSizeOption.Disabled,
                    HeightRequest = 250,
                    BackgroundColor = SurfaceAlt,
                    TextColor = Ink,
                    FontFamily = "OpenSansRegular",
                    FontSize = 13
                },
                ("Copy result", "copy", true),
                ("Close", null, false));

            if (choice == "copy")
            {
                await Clipboard.Default.SetTextAsync(result);
                await MessageAsync("Copied", "Formatted JSON copied.");
            }
        }
        catch (JsonException)
        {
            await MessageAsync(
                "Invalid JSON",
                "The JSON could not be parsed. Check quotation marks, commas, braces and brackets.");
        }
    }

    
private async Task OpenCalculatorAsync()
{
    string? expression = await PromptAsync(
        "Calculator",
        "Use +, -, *, / and parentheses.",
        "e.g. 12 * (4 + 3)");

    if (string.IsNullOrWhiteSpace(expression))
        return;

    try
    {
        double result = SimpleCalculator.Evaluate(expression);

        string value = result.ToString(
            "G",
            System.Globalization.CultureInfo.InvariantCulture);

        string? choice = await ModalAsync(
            "Result",
            expression,
            Txt(value, 32, true, Accent),
            ("Copy result", value, true),
            ("Calculate again", "again", false),
            ("Close", null, false));

        if (choice == "again")
        {
            await OpenCalculatorAsync();
        }
        else if (choice != null)
        {
            await Clipboard.Default.SetTextAsync(choice);
            await MessageAsync("Copied", "Result copied to clipboard.");
        }
    }
    catch
    {
        await MessageAsync(
            "Calculation error",
            "Enter a valid arithmetic expression, such as 12 * (4 + 3).");
    }
}


    private async Task ShowClipboardAsync()
    {
        string? text = await Clipboard.Default.GetTextAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            await MessageAsync(
                "Clipboard is empty",
                "Copy some text first, then return here to view it.");
            return;
        }

        string? choice = await ModalAsync(
            "Current clipboard",
            "This reads the current clipboard item, not your device's full clipboard history.",
            new Editor
            {
                Text = text,
                IsReadOnly = true,
                AutoSize = EditorAutoSizeOption.Disabled,
                HeightRequest = 180,
                BackgroundColor = SurfaceAlt,
                TextColor = Ink,
                FontSize = 14
            },
            ("Copy again", "copy", true),
            ("Save as snippet", "save", false),
            ("Close", null, false));

        if (choice == "copy")
        {
            await Clipboard.Default.SetTextAsync(text);
            await MessageAsync("Copied", "Clipboard text copied again.");
        }
        else if (choice == "save")
        {
            string? title = await PromptAsync(
                "Save clipboard text", "Give the saved text a name", "Snippet name");

            if (string.IsNullOrWhiteSpace(title))
                return;

            _actions.Add(new QuickAction
            {
                Title = title.Trim(),
                Content = text,
                Type = "snippet",
                ProfileId = _activeProfile,
                Icon = "⌘"
            });

            await _store.SaveActionsAsync(_actions);
            await MessageAsync("Saved", "Clipboard text was saved as a snippet.");
        }
    }

    // ---------------------------------------------------------
    // CUSTOM MODALS
    // ---------------------------------------------------------

    
private async Task<string?> PromptAsync(
    string title,
    string subtitle,
    string placeholder,
    string? initialValue = null,
    bool multiline = false)
{
    View editor = multiline
        ? new Editor
        {
            Text = initialValue ?? "",
            Placeholder = placeholder,
            AutoSize = EditorAutoSizeOption.Disabled,
            HeightRequest = 160,
            BackgroundColor = SurfaceAlt,
            TextColor = Ink,
            PlaceholderColor = Muted,
            FontSize = 14
        }
        : new Entry
        {
            Text = initialValue ?? "",
            Placeholder = placeholder,
            BackgroundColor = SurfaceAlt,
            TextColor = Ink,
            PlaceholderColor = Muted,
            FontSize = 15
        };

    var wrapper = new Border
    {
        BackgroundColor = SurfaceAlt,
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(18)
        },
        Padding = new Thickness(12, 4),
        Content = editor
    };

    return await ModalAsync(
        title,
        subtitle,
        wrapper,
        new[]
        {
            ("Save", "save", true),
            ("Cancel", (string?)null, false)
        },
        () => editor is Entry entry
            ? entry.Text
            : ((Editor)editor).Text);
}

    
private async Task<string?> ChooseAsync(
    string title,
    string subtitle,
    IEnumerable<string> options)
{
    var availableOptions = options.ToArray();

    var body = new VerticalStackLayout
    {
        Spacing = 8,
        Children =
        {
            Txt(
                "Choose what this shortcut should open.",
                13,
                color: Muted)
        }
    };

    var buttons = availableOptions
        .Select(option =>
            (option, (string?)option, true))
        .Append(("Cancel", (string?)null, false))
        .ToArray();

    return await ModalAsync(
        title,
        subtitle,
        body,
        buttons);
}

    private async Task<bool> ConfirmAsync(string title, string subtitle)
    {
        string? result = await ModalAsync(
            title,
            subtitle,
            IconBox("!", 58, true),
            ("Delete", "yes", true),
            ("Keep item", null, false));

        return result == "yes";
    }

    private async Task MessageAsync(string title, string message)
    {
        await ModalAsync(
            title,
            message,
            IconBox("✓", 56, true),
            ("Got it", "ok", true));
    }

    private async Task<string?> ModalAsync(
        string title,
        string subtitle,
        View body,
        params (string Label, string? Value, bool Primary)[] buttons)
    {
        return await ModalAsync(
            title, subtitle, body, buttons, null);
    }

    

private async Task<string?> ModalAsync(
    string title,
    string subtitle,
    View body,
    (string Label, string? Value, bool Primary)[] buttons,
    Func<string?>? getInput)
{
    // Prevent multiple modal windows from opening simultaneously.
    if (_root.Children.Any(v =>
        v is Grid g && g.AutomationId == "EdgeKitModal"))
    {
        return null;
    }

    var completion = new TaskCompletionSource<string?>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    var scrim = new BoxView
    {
        BackgroundColor = Color.FromArgb("#77000000"),
        Opacity = 0
    };

    // Modal heading
    var heading = new VerticalStackLayout
    {
        Spacing = 7,
        Children =
        {
            new Border
            {
                WidthRequest = 38,
                HeightRequest = 5,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = IsDark
                    ? Color.FromArgb("#555B68")
                    : Color.FromArgb("#D3D6E1"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(5)
                }
            },

            Txt(title, 23, true),
            Txt(subtitle, 13, color: Muted)
        }
    };

    // Body and buttons are placed together in ONE scroll view.
    var modalContent = new VerticalStackLayout
    {
        Spacing = 15,
        Children =
        {
            body,

            BuildModalButtons(buttons, value =>
            {
                string? finalValue =
                    value == "save" && getInput != null
                        ? getInput()
                        : value;

                completion.TrySetResult(finalValue);
            })
        }
    };

    double screenHeight =
        DeviceDisplay.MainDisplayInfo.Height /
        DeviceDisplay.MainDisplayInfo.Density;

    var contentScroll = new ScrollView
    {
        Content = modalContent,
        Orientation = ScrollOrientation.Vertical,
        VerticalScrollBarVisibility = ScrollBarVisibility.Default,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        VerticalOptions = LayoutOptions.Start,
        MaximumHeightRequest = screenHeight * 0.68
    };

    var sheetContent = new VerticalStackLayout
    {
        Spacing = 18,
        Children =
        {
            heading,
            contentScroll
        }
    };

    var sheet = new Border
    {
        AutomationId = "EdgeKitModalSheet",
        BackgroundColor = Surface,
        Stroke = new SolidColorBrush(
            IsDark
                ? Color.FromArgb("#414653")
                : Color.FromArgb("#E0E3ED")),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(30)
        },
        Padding = new Thickness(21, 17, 21, 20),
        VerticalOptions = LayoutOptions.End,
        HorizontalOptions = LayoutOptions.Fill,
        Margin = new Thickness(10, 0, 10, 12),
        MaximumHeightRequest = screenHeight * 0.86,
        TranslationY = 500,
        Opacity = 0,
        Content = sheetContent
    };

    // Full-screen overlay.
    var overlay = new Grid
    {
        AutomationId = "EdgeKitModal",
        BackgroundColor = Colors.Transparent
    };

    // IMPORTANT: Add each view exactly once.
    overlay.Children.Add(scrim);
    overlay.Children.Add(sheet);

    // Make the overlay cover BOTH the main content and navigation rows.
    Grid.SetRowSpan(overlay, 2);
       // Outside-tap dismissal.
    var outsideTap = new TapGestureRecognizer();

    outsideTap.Tapped += (_, _) =>
        completion.TrySetResult(null);

    scrim.GestureRecognizers.Add(outsideTap);

    _root.Add(overlay);
    overlay.ZIndex = 100;

    try
    {
        await Task.WhenAll(
            scrim.FadeTo(1, 180, Easing.CubicOut),
            sheet.FadeTo(1, 200, Easing.CubicOut),
            sheet.TranslateTo(0, 0, 220, Easing.CubicOut));

        string? result = await completion.Task;

        await Task.WhenAll(
            scrim.FadeTo(0, 130, Easing.CubicIn),
            sheet.FadeTo(0, 130, Easing.CubicIn),
            sheet.TranslateTo(0, 350, 150, Easing.CubicIn));

        return result;
    }
    finally
    {
        if (_root.Children.Contains(overlay))
        {
            _root.Children.Remove(overlay);
        }
    }
}}
    