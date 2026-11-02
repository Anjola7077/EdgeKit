
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EdgeKit;

public partial class MainPage
{
    private async Task OpenNetworkToolsAsync()
    {
        var picker = new Picker
        {
            Title = "Select network tool",
            ItemsSource = new List<string>
            {
                "Connection Monitor",
                "IP Inspector",
                "DNS Lookup",
                "TCP Port Tester"
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
                Txt("NETWORK DIAGNOSTICS", 11, true, Muted),
                picker
            }
        };

        string? choice = await ModalAsync(
            "Network Toolkit",
            "Choose a diagnostic tool.",
            content,
            ("Open tool", "open", true),
            ("Cancel", null, false));

        if (choice != "open")
            return;

        switch (picker.SelectedItem?.ToString())
        {
            case "Connection Monitor":
                await ShowConnectionMonitorAsync();
                break;

            case "IP Inspector":
                await ShowIPInspectorAsync();
                break;

            case "DNS Lookup":
                await RunDNSLookupAsync();
                break;

            case "TCP Port Tester":
                await RunTCPPortTestAsync();
                break;
        }
    }

    // CONNECTION MONITOR

    private async Task ShowConnectionMonitorAsync()
    {
        var access = Connectivity.Current.NetworkAccess;

var profiles = Connectivity.Current.ConnectionProfiles;

string connectionType = !profiles.Any()
    ? "Unknown / unavailable"
    : string.Join(", ", profiles);

string status = access switch
{
    Microsoft.Maui.Networking.NetworkAccess.Internet =>
        "Internet available",

    Microsoft.Maui.Networking.NetworkAccess.ConstrainedInternet =>
        "Limited internet access",

    Microsoft.Maui.Networking.NetworkAccess.Local =>
        "Local network only",

    Microsoft.Maui.Networking.NetworkAccess.None =>
        "No network connection",

    _ => "Unknown"
};

        string report =
            $"CONNECTION STATUS\n\n" +
            $"Status: {status}\n" +
            $"Connection profile: {connectionType}\n" +
            $"Checked: {DateTime.Now:dd MMM yyyy, HH:mm:ss}";

        var content = new VerticalStackLayout
        {
            Spacing = 12,

            Children =
            {
                Card(
                    new VerticalStackLayout
                    {
                        Spacing = 8,

                        Children =
                        {
                            Txt("NETWORK STATUS", 10, true, Muted),
                            Txt(status, 19, true,
                                access == Microsoft.Maui.Networking.NetworkAccess.Internet
                                    ? Color.FromArgb("#299D72")
                                    : Ink),
                            Txt(connectionType, 12, false, Muted)
                        }
                    },
                    16),

                Txt(
                    "This reports the operating system's network assessment. It does not guarantee that every website or server is reachable.",
                    11,
                    false,
                    Muted)
            }
        };

        string? action = await ModalAsync(
            "Connection Monitor",
            "Current device network information.",
            content,
            ("Copy report", "copy", true),
            ("Refresh", "refresh", false),
            ("Close", null, false));

        if (action == "copy")
        {
            await Clipboard.Default.SetTextAsync(report);
            await MessageAsync("Copied", "Network report copied.");
        }
        else if (action == "refresh")
        {
            await ShowConnectionMonitorAsync();
        }
    }

    // IP INSPECTOR

    private async Task ShowIPInspectorAsync()
    {
        try
        {
            var lines = new List<string>();

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var properties = adapter.GetIPProperties();

                var addresses = properties.UnicastAddresses
                    .Where(x =>
                        !IPAddress.IsLoopback(x.Address) &&
                        (x.Address.AddressFamily == AddressFamily.InterNetwork ||
                         x.Address.AddressFamily == AddressFamily.InterNetworkV6))
                    .Select(x => x.Address.ToString())
                    .Distinct()
                    .ToList();

                if (addresses.Count == 0)
                    continue;

                lines.Add($"INTERFACE: {adapter.Name}");
                lines.Add($"TYPE: {adapter.NetworkInterfaceType}");

                foreach (string address in addresses)
                    lines.Add($"IP: {address}");

                foreach (var gateway in properties.GatewayAddresses)
                {
                    if (gateway.Address != null &&
                        !IPAddress.IsLoopback(gateway.Address) &&
                        !gateway.Address.Equals(IPAddress.Any))
                    {
                        lines.Add($"GATEWAY: {gateway.Address}");
                    }
                }

                foreach (var dns in properties.DnsAddresses)
                {
                    if (!IPAddress.IsLoopback(dns))
                        lines.Add($"DNS: {dns}");
                }

                lines.Add("");
            }

            if (lines.Count == 0)
            {
                lines.Add("No active network interface addresses were found.");
            }

            string report = string.Join(Environment.NewLine, lines);

            var content = new VerticalStackLayout
            {
                Spacing = 12,

                Children =
                {
                    Txt("DEVICE NETWORK INTERFACES", 11, true, Muted),

                    new Editor
                    {
                        Text = report,
                        IsReadOnly = true,
                        AutoSize = EditorAutoSizeOption.Disabled,
                        HeightRequest = 280,
                        BackgroundColor = SurfaceAlt,
                        TextColor = Ink,
                        FontSize = 12
                    }
                }
            };

            string? action = await ModalAsync(
                "IP Inspector",
                "Local network interface information.",
                content,
                ("Copy details", "copy", true),
                ("Close", null, false));

            if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(report);

                await MessageAsync(
                    "Copied",
                    "Network interface details copied.");
            }
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "IP inspection failed",
                ex.Message);
        }
    }

    // DNS LOOKUP

    private async Task RunDNSLookupAsync()
    {
        string? host = await PromptAsync(
            "DNS Lookup",
            "Enter a domain name. Example: google.com",
            "google.com");

        if (host == null)
            return;

        host = host.Trim();

        if (host.Contains("://") || host.Contains('/') ||
            host.Contains(' '))
        {
            await MessageAsync(
                "Invalid domain",
                "Enter only a hostname, such as example.com.");

            return;
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host)
                .WaitAsync(TimeSpan.FromSeconds(10));

            string report =
                $"DNS LOOKUP: {host}\n\n" +
                string.Join(
                    Environment.NewLine,
                    addresses.Select(x =>
                        $"{x.AddressFamily}: {x}"));

            var content = new VerticalStackLayout
            {
                Spacing = 12,

                Children =
                {
                    Txt("RESOLVED ADDRESSES", 11, true, Muted),

                    Txt(host, 16, true, Ink),

                    new Editor
                    {
                        Text = string.Join(
                            Environment.NewLine,
                            addresses.Select(x =>
                                $"{x.AddressFamily}: {x}")),
                        IsReadOnly = true,
                        AutoSize = EditorAutoSizeOption.Disabled,
                        HeightRequest = 180,
                        BackgroundColor = SurfaceAlt,
                        TextColor = Ink,
                        FontSize = 13
                    }
                }
            };

            string? action = await ModalAsync(
                "DNS Lookup Result",
                $"{addresses.Length} address(es) resolved.",
                content,
                ("Copy results", "copy", true),
                ("Close", null, false));

            if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(report);

                await MessageAsync(
                    "Copied",
                    "DNS results copied.");
            }
        }
        catch (Exception ex)
        {
            await MessageAsync(
                "DNS lookup failed",
                $"Could not resolve {host}.\n\n{ex.Message}");
        }
    }

    // TCP PORT TESTER

    private async Task RunTCPPortTestAsync()
    {
        string? host = await PromptAsync(
            "Server hostname",
            "Enter a hostname or IP address.",
            "google.com");

        if (host == null)
            return;

        host = host.Trim();

        if (string.IsNullOrWhiteSpace(host) ||
            host.Contains(' ') ||
            host.Contains("://") ||
            host.Contains('/'))
        {
            await MessageAsync(
                "Invalid hostname",
                "Enter a hostname or IP address without a URL path.");

            return;
        }

        string? portText = await PromptAsync(
            "TCP port",
            "Enter a port number between 1 and 65535.",
            "443");

        if (portText == null)
            return;

        if (!int.TryParse(portText.Trim(), out int port) ||
            port < 1 || port > 65535)
        {
            await MessageAsync(
                "Invalid port",
                "Enter a number between 1 and 65535.");

            return;
        }

        var timer = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            await client.ConnectAsync(host, port)
                .WaitAsync(TimeSpan.FromSeconds(6));

            timer.Stop();

            string report =
                $"TCP CONNECTION TEST\n\n" +
                $"Host: {host}\n" +
                $"Port: {port}\n" +
                $"Status: OPEN / REACHABLE\n" +
                $"Connection time: {timer.ElapsedMilliseconds} ms\n" +
                $"Remote endpoint: {client.Client.RemoteEndPoint}";

            var content = new VerticalStackLayout
            {
                Spacing = 10,

                Children =
                {
                    Txt("CONNECTION SUCCESSFUL", 11, true,
                        Color.FromArgb("#299D72")),

                    Txt($"{host}:{port}", 19, true, Ink),

                    Txt(
                        $"Connection time: {timer.ElapsedMilliseconds} ms",
                        13,
                        false,
                        Muted),

                    Txt(
                        "The remote server accepted a TCP connection.",
                        12,
                        false,
                        Muted)
                }
            };

            string? action = await ModalAsync(
                "Port Test Result",
                "TCP connection established.",
                content,
                ("Copy report", "copy", true),
                ("Close", null, false));

            if (action == "copy")
            {
                await Clipboard.Default.SetTextAsync(report);

                await MessageAsync(
                    "Copied",
                    "Port test report copied.");
            }
        }
        catch (Exception ex)
        {
            timer.Stop();

            await MessageAsync(
                "Connection failed",
                $"Could not connect to {host}:{port}.\n\n{ex.Message}");
        }
    }
}
