
using System.Text.Json;
using EdgeKit.Models;

namespace EdgeKit.Services;

public class EdgeStore
{
    private readonly string _folder =
        Path.Combine(FileSystem.AppDataDirectory, "edgekit");

    private readonly JsonSerializerOptions _options =
        new() { WriteIndented = true };

    private readonly SemaphoreSlim _lock = new(1, 1);

    private string ActionsPath => Path.Combine(_folder, "actions.json");
    private string ProfilesPath => Path.Combine(_folder, "profiles.json");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);

        await _lock.WaitAsync();

        try
        {
            if (!File.Exists(ActionsPath))
            {
                await WriteAsync(ActionsPath, new List<QuickAction>
                {
                    new()
                    {
                        Title = "Quick note",
                        Type = "note",
                        Content = "",
                        Icon = "N"
                    },
                    new()
                    {
                        Title = "Calculator",
                        Type = "calculator",
                        Icon = "÷"
                    },
                    new()
                    {
                        Title = "Saved text",
                        Type = "snippet",
                        Content = "Paste frequently used text here.",
                        Icon = "T"
                    },
                    new()
                    {
                        Title = "JSON formatter",
                        Type = "json",
                        Content = "",
                        Icon = "{ }"
                    },
                    new()
                    {
                        Title = "UUID generator",
                        Type = "uuid",
                        Icon = "#"
                    }
                });
            }

            if (!File.Exists(ProfilesPath))
            {
                await WriteAsync(ProfilesPath, new List<EdgeProfile>
                {
                    new()
                    {
                        Id = "default",
                        Name = "Everyday",
                        Description = "Your daily shortcuts",
                        IsDefault = true
                    },
                    new()
                    {
                        Id = "coding",
                        Name = "Developer",
                        Description = "Tools for coding sessions"
                    },
                    new()
                    {
                        Id = "study",
                        Name = "Study",
                        Description = "Notes and study helpers"
                    }
                });
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteAsync<T>(string path, T value)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, _options);
    }

    private async Task<T> ReadAsync<T>(string path, T fallback)
    {
        if (!File.Exists(path))
            return fallback;

        await using var stream = File.OpenRead(path);

        return await JsonSerializer.DeserializeAsync<T>(stream, _options)
            ?? fallback;
    }

    public async Task<List<QuickAction>> GetActionsAsync()
    {
        await InitializeAsync();

        await _lock.WaitAsync();

        try
        {
            return await ReadAsync(ActionsPath, new List<QuickAction>());
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<EdgeProfile>> GetProfilesAsync()
    {
        await InitializeAsync();

        await _lock.WaitAsync();

        try
        {
            return await ReadAsync(ProfilesPath, new List<EdgeProfile>());
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveActionsAsync(List<QuickAction> actions)
    {
        await InitializeAsync();

        await _lock.WaitAsync();

        try
        {
            await WriteAsync(ActionsPath, actions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveProfilesAsync(List<EdgeProfile> profiles)
    {
        await InitializeAsync();

        await _lock.WaitAsync();

        try
        {
            await WriteAsync(ProfilesPath, profiles);
        }
        finally
        {
            _lock.Release();
        }
    }
}
