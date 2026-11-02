
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace EdgeKit.Services;

public static class FormVault
{
    private const string StorageKey = "edgekit_form_assistant_vault_v1";

    public static async Task<Dictionary<string, string>> LoadAsync()
    {
        try
        {
            string? json = await SecureStorage.Default.GetAsync(StorageKey);

            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, string>();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            // Do not overwrite data automatically if decryption fails.
            throw new InvalidOperationException(
                "The secure profile vault could not be opened. " +
                "Your stored information has not been intentionally deleted. " +
                "Check device security settings and try again.");
        }
    }

    public static async Task SaveAsync(
        Dictionary<string, string> profile)
    {
        try
        {
            string json = JsonSerializer.Serialize(profile);

            await SecureStorage.Default.SetAsync(
                StorageKey, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not save the secure profile. " +
                "Check that Android secure storage is available. " +
                ex.Message);
        }
    }

    public static void Clear()
    {
        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not clear the secure profile vault. " +
                ex.Message);
        }
    }
}
