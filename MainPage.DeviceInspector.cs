
using Microsoft.Maui.Devices;

namespace EdgeKit;

public partial class MainPage
{
    private async Task OpenDeviceInspectorAsync()
    {
        await ShowDeviceInspectorAsync();
    }

    private async Task ShowDeviceInspectorAsync()
    {
        try
        {
            var device = DeviceInfo.Current;
            var display = DeviceDisplay.Current.MainDisplayInfo;
            var app = AppInfo.Current;

 

            string resolution =
                $"{display.Width:0} × {display.Height:0} pixels";

            string density =
                $"{display.Density:0.##}x";

            string refreshRate =
                $"{display.RefreshRate:0.##} Hz";

            string orientation = display.Orientation.ToString();

            string rotation = display.Rotation.ToString();

            string deviceType = device.DeviceType.ToString();

            string report =
                "EDGEKIT DEVICE INSPECTION\n\n" +
                $"Manufacturer: {device.Manufacturer}\n" +
                $"Model: {device.Model}\n" +
                $"Device name: {device.Name}\n" +
                $"Platform: {device.Platform}\n" +
                $"OS version: {device.VersionString}\n" +
                $"Device type: {deviceType}\n" +
                $"Device idiom: {device.Idiom}\n\n" +
                
                
                $"DISPLAY\n" +
                $"Resolution: {resolution}\n" +
                $"Density: {density}\n" +
                $"Refresh rate: {refreshRate}\n" +
                $"Orientation: {orientation}\n" +
                $"Rotation: {rotation}\n\n" +
                $"APPLICATION\n" +
                $"Name: {app.Name}\n" +
                $"Version: {app.VersionString}\n" +
                $"Build: {app.BuildString}\n" +
                $"Package: {app.PackageName}";

            var content = new VerticalStackLayout
            {
                Spacing = 13,

                Children =
                {
                    Txt("DEVICE INFORMATION", 11, true, Muted),

                    Card(
                        new VerticalStackLayout
                        {
                            Spacing = 8,

                            Children =
                            {
                                Txt(
                                    $"{device.Manufacturer} {device.Model}",
                                    19,
                                    true,
                                    Ink),

                                Txt(
                                    $"{device.Platform} {device.VersionString}",
                                    12,
                                    false,
                                    Muted),

                                Txt(
                                    $"Device type: {deviceType}",
                                    11,
                                    false,
                                    Muted)
                            }
                        },
                        15),

                    
                    Txt("DISPLAY", 11, true, Muted),

                    Card(
                        new VerticalStackLayout
                        {
                            Spacing = 9,

                            Children =
                            {
                                BuildDeviceInfoRow(
                                    "Resolution",
                                    resolution),

                                BuildDeviceInfoRow(
                                    "Pixel density",
                                    density),

                                BuildDeviceInfoRow(
                                    "Refresh rate",
                                    refreshRate),

                                BuildDeviceInfoRow(
                                    "Orientation",
                                    orientation),

                                BuildDeviceInfoRow(
                                    "Rotation",
                                    rotation)
                            }
                        },
                        15),

                    Txt("EDGEKIT APPLICATION", 11, true, Muted),

                    Card(
                        new VerticalStackLayout
                        {
                            Spacing = 9,

                            Children =
                            {
                                BuildDeviceInfoRow("Name", app.Name),
                                BuildDeviceInfoRow("Version", app.VersionString),
                                BuildDeviceInfoRow("Build", app.BuildString),
                                BuildDeviceInfoRow("Package", app.PackageName)
                            }
                        },
                        15)
                }
            };

            string? action = await ModalAsync(
                "Device Inspector",
                "Live device and application information.",
                new ScrollView
                {
                    Content = content
                },
                ("Copy report", "copy", true),
                ("Refresh", "refresh", false),
                ("Close", null, false));

            if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(report);

                await MessageAsync(
                    "Copied",
                    "Device inspection report copied.");
            }
            else if (action == "refresh")
            {
                await ShowDeviceInspectorAsync();
            }
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "Inspection failed",
                ex.Message);
        }
    }

    private View BuildDeviceInfoRow(
        string title,
        string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };

        var left = Txt(title, 12, false, Muted);

        var right = new Label
        {
            Text = value,
            FontSize = 12,
            TextColor = Ink,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.Fill,
            LineBreakMode = LineBreakMode.WordWrap
        };

        Grid.SetColumn(right, 1);

        grid.Children.Add(left);
        grid.Children.Add(right);

        return grid;
    }
}
