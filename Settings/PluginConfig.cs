using System;
using System.IO;
using Newtonsoft.Json;

namespace ClassworksPlugin.Settings
{
    /// <summary>
    /// Represents persistent settings for the Classworks plugin.  These
    /// properties are loaded from and saved to a JSON file in the
    /// plugin's working directory.  You may adjust the storage location
    /// according to the plugin SDK conventions.
    /// </summary>
    public sealed class PluginConfig
    {
        public string NamespaceId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;

        private static readonly string _configPath = Path.Combine(
            AppContext.BaseDirectory, "classworks.config.json");

        /// <summary>
        /// Loads the configuration from disk.  If no file exists a new
        /// configuration is returned.
        /// </summary>
        public static PluginConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    return JsonConvert.DeserializeObject<PluginConfig>(json) ?? new PluginConfig();
                }
            }
            catch
            {
                // Ignore errors and return defaults
            }
            return new PluginConfig();
        }

        /// <summary>
        /// Saves the current configuration to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // Suppress exceptions to avoid crashing the plugin
            }
        }
    }
}