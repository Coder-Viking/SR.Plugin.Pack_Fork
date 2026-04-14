using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace RunAndGunMod
{
    /// <summary>
    /// Configuration for the Run &amp; Gun Mod.
    /// Saved/loaded as XML in the plugin directory.
    /// </summary>
    [Serializable]
    [XmlRoot("RunAndGunConfig")]
    public class RunAndGunConfig
    {
        /// <summary>
        /// Multiplier applied to all weapon damage values (m_damage_min, m_damage_max, m_shield_damage).
        /// Default 2.0 = double damage. Affects ALL weapons globally (player and enemy).
        /// </summary>
        [XmlElement("DamageMultiplier")]
        public float DamageMultiplier = 2.0f;

        /// <summary>
        /// Multiplier for sprint energy cost. Lower = cheaper sprint = longer sprint duration.
        /// Default 0.2 = sprint costs only 20% of normal energy, allowing ~15 seconds of base sprint.
        /// Does NOT affect ability energy costs.
        /// </summary>
        [XmlElement("SprintEnergyCostMultiplier")]
        public float SprintEnergyCostMultiplier = 0.2f;

        /// <summary>
        /// Whether to show a popup message when the mod applies its changes.
        /// </summary>
        [XmlElement("ShowActivationMessage")]
        public bool ShowActivationMessage = true;

        private const string ConfigFileName = "RunAndGunConfig.xml";

        public static RunAndGunConfig Load()
        {
            try
            {
                string pluginPath = Manager.GetPluginManager().PluginPath;
                string configPath = Path.Combine(pluginPath, ConfigFileName);

                if (File.Exists(configPath))
                {
                    var serializer = new XmlSerializer(typeof(RunAndGunConfig));
                    using (var reader = new StreamReader(configPath))
                    {
                        var config = (RunAndGunConfig)serializer.Deserialize(reader);
                        Debug.Log("RunAndGunMod: Config loaded from " + configPath);
                        return config;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Error loading config: " + e.Message);
            }

            // Create and save default config
            var defaultConfig = new RunAndGunConfig();
            defaultConfig.Save();
            return defaultConfig;
        }

        public void Save()
        {
            try
            {
                string pluginPath = Manager.GetPluginManager().PluginPath;
                string configPath = Path.Combine(pluginPath, ConfigFileName);

                var serializer = new XmlSerializer(typeof(RunAndGunConfig));
                using (var writer = new StreamWriter(configPath))
                {
                    serializer.Serialize(writer, this);
                }
                Debug.Log("RunAndGunMod: Config saved to " + configPath);
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Error saving config: " + e.Message);
            }
        }
    }
}
