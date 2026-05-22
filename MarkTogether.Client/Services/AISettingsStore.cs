using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MarkTogether.Client.Services
{
    public class AISettings
    {
        public string Provider { get; set; } = "gemini";
        public string Model { get; set; } = "gemini-2.5-flash";
        public string ApiKey { get; set; } = "";
    }

    public static class AISettingsStore
    {
        public static readonly IReadOnlyList<string> SupportedModels = new[]
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-2.5-flash-lite",
            "gemini-3-flash",
            "gemini-3.1-pro",
        };

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MarkTogether");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ai_settings.json");
            }
        }

        public static AISettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AISettings();

            try
            {
                string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var dto = JsonConvert.DeserializeObject<PersistedDto>(json) ?? new PersistedDto();
                return new AISettings
                {
                    Provider = string.IsNullOrEmpty(dto.Provider) ? "gemini" : dto.Provider,
                    Model = string.IsNullOrEmpty(dto.Model) ? "gemini-2.5-flash" : dto.Model,
                    ApiKey = Decrypt(dto.ApiKeyEncrypted)
                };
            }
            catch
            {
                return new AISettings();
            }
        }

        public static void Save(AISettings settings)
        {
            settings = settings ?? new AISettings();

            var dto = new PersistedDto
            {
                Provider = string.IsNullOrEmpty(settings.Provider) ? "gemini" : settings.Provider,
                Model = string.IsNullOrEmpty(settings.Model) ? "gemini-2.5-flash" : settings.Model,
                ApiKeyEncrypted = Encrypt(settings.ApiKey ?? "")
            };

            File.WriteAllText(
                SettingsPath,
                JsonConvert.SerializeObject(dto, Formatting.Indented),
                Encoding.UTF8);
        }

        private static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";

            byte[] data = Encoding.UTF8.GetBytes(plain);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        private static string Decrypt(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return "";

            try
            {
                byte[] encrypted = Convert.FromBase64String(b64);
                byte[] data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return "";
            }
        }

        private class PersistedDto
        {
            public string Provider { get; set; }
            public string Model { get; set; }
            public string ApiKeyEncrypted { get; set; }
        }
    }
}