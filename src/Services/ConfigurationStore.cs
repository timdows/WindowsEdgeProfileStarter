using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace EdgeProfileStarter
{
    internal sealed class ConfigurationStore
    {
        private static readonly Regex ProfileDirectory = new Regex(@"\A(?:Default|Profile [0-9]+)\z", RegexOptions.Compiled);
        private readonly string path;

        internal ConfigurationStore(string path) { this.path = path; }

        internal Configuration Load()
        {
            Configuration config = new JavaScriptSerializer().Deserialize<Configuration>(File.ReadAllText(path, Encoding.UTF8));
            Validate(config);
            return config;
        }

        internal void SetDarkMode(bool darkMode)
        {
            // Read the latest file so changing the theme preserves manual edits and extra fields.
            string json = File.ReadAllText(path, Encoding.UTF8);
            var serializer = new JavaScriptSerializer();
            Validate(serializer.Deserialize<Configuration>(json));
            var values = serializer.Deserialize<Dictionary<string, object>>(json);
            values["darkMode"] = darkMode;
            Save(values);
        }

        private static void Validate(Configuration config)
        {
            if (config == null || config.profiles == null || config.profiles.Count == 0)
                throw new InvalidDataException("Voeg minimaal één profiel toe aan config.json.");
            if (config.rdpSessions != null)
                foreach (RdpSession session in config.rdpSessions) RdpLauncher.Validate(session);
            if (config.vncSessions != null)
                foreach (VncSession session in config.vncSessions) VncLauncher.Validate(session);
            var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Profile profile in config.profiles)
            {
                if (profile == null || String.IsNullOrWhiteSpace(profile.name) ||
                    profile.directory == null || !ProfileDirectory.IsMatch(profile.directory))
                    throw new InvalidDataException("Elk profiel moet een naam en directory hebben, bijvoorbeeld Default of Profile 3.");
                if (!directories.Add(profile.directory))
                    throw new InvalidDataException("Dubbele profieldirectory: " + profile.directory);
            }
        }
        private void Save(Dictionary<string, object> values)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllText(temporary, JsonFormatter.Format(new JavaScriptSerializer().Serialize(values)), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

    }
}
