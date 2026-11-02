using System.Globalization;

namespace EdgeKit;

public partial class MainPage
{
    private static readonly Dictionary<string, Dictionary<string, double>>
        ConversionUnits = new()
    {
        ["Length"] = new()
        {
            ["Millimetre (mm)"] = 0.001,
            ["Centimetre (cm)"] = 0.01,
            ["Metre (m)"] = 1,
            ["Kilometre (km)"] = 1000,
            ["Inch (in)"] = 0.0254,
            ["Foot (ft)"] = 0.3048,
            ["Yard (yd)"] = 0.9144,
            ["Mile (mi)"] = 1609.344
        },

        ["Mass"] = new()
        {
            ["Milligram (mg)"] = 0.000001,
            ["Gram (g)"] = 0.001,
            ["Kilogram (kg)"] = 1,
            ["Metric tonne (t)"] = 1000,
            ["Ounce (oz)"] = 0.028349523125,
            ["Pound (lb)"] = 0.45359237,
            ["Stone (st)"] = 6.35029318
        },

        ["Temperature"] = new()
        {
            ["Celsius (°C)"] = 1,
            ["Fahrenheit (°F)"] = 1,
            ["Kelvin (K)"] = 1
        },

        ["Area"] = new()
        {
            ["Square centimetre (cm²)"] = 0.0001,
            ["Square metre (m²)"] = 1,
            ["Square kilometre (km²)"] = 1_000_000,
            ["Hectare (ha)"] = 10_000,
            ["Acre (ac)"] = 4046.8564224,
            ["Square foot (ft²)"] = 0.09290304
        },

        ["Volume"] = new()
        {
            ["Millilitre (ml)"] = 0.001,
            ["Litre (L)"] = 1,
            ["Cubic metre (m³)"] = 1000,
            ["US gallon (gal)"] = 3.785411784,
            ["US cup"] = 0.2365882365,
            ["Fluid ounce (fl oz)"] = 0.0295735296
        },

        ["Speed"] = new()
        {
            ["Metres/second (m/s)"] = 1,
            ["Kilometres/hour (km/h)"] = 0.2777777778,
            ["Miles/hour (mph)"] = 0.44704,
            ["Feet/second (ft/s)"] = 0.3048,
            ["Knots (kn)"] = 0.5144444444
        },

        ["Digital storage"] = new()
        {
            ["Bit (b)"] = 0.125,
            ["Byte (B)"] = 1,
            ["Kilobyte (KB)"] = 1000,
            ["Megabyte (MB)"] = 1_000_000,
            ["Gigabyte (GB)"] = 1_000_000_000,
            ["Terabyte (TB)"] = 1_000_000_000_000,
            ["Kibibyte (KiB)"] = 1024,
            ["Mebibyte (MiB)"] = 1_048_576,
            ["Gibibyte (GiB)"] = 1_073_741_824
        }
    };

    private View BuildUnitConverter()
    {
        var page = Page();

        page.Children.Add(PageHeader(
            "EVERYDAY UTILITY",
            "Unit converter",
            "Convert measurements quickly, entirely offline."));

        page.Children.Add(Btn(
            "‹  Back to tools",
            () => ShowTab("Tools")));

        var categoryPicker = new Picker
        {
            Title = "Choose category",
            ItemsSource = ConversionUnits.Keys.ToList(),
            SelectedIndex = 0,
            TextColor = Ink,
            TitleColor = Muted,
            BackgroundColor = SurfaceAlt
        };

        var valueEntry = new Entry
        {
            Placeholder = "Enter a value",
            Keyboard = Keyboard.Numeric,
            FontSize = 22,
            TextColor = Ink,
            PlaceholderColor = Muted,
            BackgroundColor = SurfaceAlt
        };

        var fromPicker = new Picker
        {
            Title = "From",
            TextColor = Ink,
            BackgroundColor = SurfaceAlt
        };

        var toPicker = new Picker
        {
            Title = "To",
            TextColor = Ink,
            BackgroundColor = SurfaceAlt
        };

        var resultLabel = Txt("Your result will appear here", 25, true, Accent);
        resultLabel.HorizontalTextAlignment = TextAlignment.Start;

        var detailLabel = Txt(
            "Enter a value to begin converting.",
            12,
            color: Muted);

        void RefreshUnits()
        {
            string category = categoryPicker.SelectedItem?.ToString()
                ?? "Length";

            var units = ConversionUnits[category].Keys.ToList();

            fromPicker.ItemsSource = units;
            toPicker.ItemsSource = units;

            fromPicker.SelectedIndex = 0;
            toPicker.SelectedIndex = Math.Min(1, units.Count - 1);

            UpdateResult();
        }

        void UpdateResult()
        {
            if (fromPicker.SelectedItem == null ||
                toPicker.SelectedItem == null)
            {
                return;
            }

            string category = categoryPicker.SelectedItem?.ToString()
                ?? "Length";

            string from = fromPicker.SelectedItem.ToString()!;
            string to = toPicker.SelectedItem.ToString()!;

            string rawValue = valueEntry.Text?.Trim() ?? "";

            if (!double.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double input))
            {
                if (!double.TryParse(
                    rawValue,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out input))
                {
                    resultLabel.Text = "Waiting for input";
                    detailLabel.Text = "Enter a valid number.";
                    return;
                }
            }

            if (double.IsNaN(input) || double.IsInfinity(input))
            {
                resultLabel.Text = "Invalid value";
                detailLabel.Text = "Enter a finite number.";
                return;
            }

            double converted;

            if (category == "Temperature")
            {
                converted = ConvertTemperature(input, from, to);
            }
            else
            {
                double fromFactor = ConversionUnits[category][from];
                double toFactor = ConversionUnits[category][to];

                converted = input * fromFactor / toFactor;
            }

            if (double.IsNaN(converted) || double.IsInfinity(converted))
            {
                resultLabel.Text = "Unable to convert";
                detailLabel.Text = "Try a different value.";
                return;
            }

            resultLabel.Text = converted.ToString(
                "G10",
                CultureInfo.InvariantCulture);

            detailLabel.Text =
                $"{input.ToString("G10", CultureInfo.InvariantCulture)} {from} = " +
                $"{converted.ToString("G10", CultureInfo.InvariantCulture)} {to}";
        }

        var categoryCard = Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Txt("CONVERSION CATEGORY", 11, true, Accent),
                categoryPicker
            }
        });

        var inputCard = Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Txt("VALUE", 11, true, Accent),
                valueEntry
            }
        });

        var unitGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        unitGrid.Add(new VerticalStackLayout
        {
            Spacing = 7,
            Children =
            {
                Txt("FROM", 11, true, Muted),
                fromPicker
            }
        }, 0, 0);

        unitGrid.Add(new VerticalStackLayout
        {
            Spacing = 7,
            Children =
            {
                Txt("TO", 11, true, Muted),
                toPicker
            }
        }, 1, 0);

        var swapButton = Btn("⇄  Swap units", () =>
        {
            int oldFrom = fromPicker.SelectedIndex;
            int oldTo = toPicker.SelectedIndex;

            fromPicker.SelectedIndex = oldTo;
            toPicker.SelectedIndex = oldFrom;

            UpdateResult();
        });

        var resultCard = Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Txt("CONVERSION RESULT", 11, true, Accent),
                resultLabel,
                detailLabel
            }
        });

        categoryPicker.SelectedIndexChanged += (_, _) =>
            RefreshUnits();

        fromPicker.SelectedIndexChanged += (_, _) =>
            UpdateResult();

        toPicker.SelectedIndexChanged += (_, _) =>
            UpdateResult();

        valueEntry.TextChanged += (_, _) =>
            UpdateResult();

        page.Children.Add(categoryCard);
        page.Children.Add(inputCard);
        page.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 13,
            Children =
            {
                unitGrid,
                swapButton
            }
        }));

        page.Children.Add(resultCard);

        page.Children.Add(Btn(
            "Copy result",
            async () =>
            {
                if (resultLabel.Text == "Waiting for input" ||
                    resultLabel.Text == "Invalid value" ||
                    resultLabel.Text == "Unable to convert")
                {
                    await MessageAsync(
                        "Nothing to copy",
                        "Enter a valid value first.");
                    return;
                }

                await Clipboard.Default.SetTextAsync(resultLabel.Text);
                await MessageAsync(
                    "Copied",
                    "Conversion result copied to your clipboard.");
            },
            true));

        page.Children.Add(Txt(
            "All conversions run locally on your device. Digital storage includes both decimal (KB, MB) and binary (KiB, MiB) units.",
            12,
            color: Muted));

        RefreshUnits();

        return Scroll(page);
    }

    private double ConvertTemperature(
        double value,
        string from,
        string to)
    {
        double celsius = from switch
        {
            "Celsius (°C)" => value,
            "Fahrenheit (°F)" => (value - 32) * 5 / 9,
            "Kelvin (K)" => value - 273.15,
            _ => value
        };

        return to switch
        {
            "Celsius (°C)" => celsius,
            "Fahrenheit (°F)" => celsius * 9 / 5 + 32,
            "Kelvin (K)" => celsius + 273.15,
            _ => celsius
        };
    }
    private Task OpenUnitConverterAsync()
{
    _content.Content = BuildUnitConverter();
    return Task.CompletedTask;
}
}