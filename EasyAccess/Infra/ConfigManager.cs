using global::System;
using global::System.IO;
using global::System.Text.Json;

namespace EasyAccess.Infra
{
    internal sealed class AppConfig
    {
        public bool ShowOverlayOnDetect { get; set; } = true;
        public int MaxOverlayItems { get; set; } = 10;
        public string OverlayPosition { get; set; } = "bottom";
        public string Theme { get; set; } = "system";
        public string LogLevel { get; set; } = "info";
        public string WhitelistFile { get; set; } = "whitelist.json";
    }

    internal sealed class ConfigManager
    {
        private readonly string _configPath;
        private readonly string _configDir;
        private AppConfig _config;

        public AppConfig Config => _config;

        public ConfigManager(string configDir)
        {
            _configDir = configDir;
            _configPath = Path.Combine(_configDir, "config.json");
            _config = new AppConfig();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
                _config = new AppConfig();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public static string GetDefaultConfigDirectory()
        {
            var exeDir = AppContext.BaseDirectory;
            try
            {
                var testFile = Path.Combine(exeDir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return exeDir;
            }
            catch
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EasyAccess");
            }
        }
    }
}
