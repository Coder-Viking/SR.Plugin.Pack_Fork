using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace EnemyProgressionMod
{
    /// <summary>
    /// Configuration for district-based enemy progression.
    /// Each district maps to a fixed progression range (0.0-1.0).
    /// Enemies will only scale based on which district the player is in,
    /// NOT based on the player's equipment or upgrades.
    /// </summary>
    [Serializable]
    [XmlRoot("EnemyProgressionConfig")]
    public class ProgressionConfig
    {
        /// <summary>
        /// Whether the mod is enabled. When false, vanilla scaling applies.
        /// </summary>
        [XmlElement("Enabled")]
        public bool Enabled = true;

        /// <summary>
        /// How often (in seconds) to check and re-apply progression overrides.
        /// Lower values are more responsive but cost more performance.
        /// </summary>
        [XmlElement("UpdateIntervalSeconds")]
        public float UpdateIntervalSeconds = 5.0f;

        /// <summary>
        /// Whether to show a status message when the district changes.
        /// </summary>
        [XmlElement("ShowDistrictChangeMessage")]
        public bool ShowDistrictChangeMessage = true;

        /// <summary>
        /// Maps each district to its fixed progression range.
        /// Enemies spawned in that district will use these values
        /// instead of the vanilla player-power-based scaling.
        /// </summary>
        [XmlArray("DistrictProgressionOverrides")]
        [XmlArrayItem("District")]
        public List<DistrictProgressionEntry> DistrictProgressionOverrides = new List<DistrictProgressionEntry>();

        /// <summary>
        /// Creates the default configuration with sensible progression values per district.
        /// The game has ~5 districts progressing from early to endgame.
        /// </summary>
        public static ProgressionConfig CreateDefault()
        {
            var config = new ProgressionConfig();
            config.DistrictProgressionOverrides = new List<DistrictProgressionEntry>
            {
                // District 0: Starting area (Downtown / Red Light) - weakest enemies
                new DistrictProgressionEntry { DistrictIndex = 0, Name = "Downtown",   MinProgression = 0.00f, MaxProgression = 0.20f },
                // District 1: Industrial - early-mid enemies
                new DistrictProgressionEntry { DistrictIndex = 1, Name = "Industrial", MinProgression = 0.15f, MaxProgression = 0.40f },
                // District 2: The Grid - mid enemies
                new DistrictProgressionEntry { DistrictIndex = 2, Name = "Grid",       MinProgression = 0.35f, MaxProgression = 0.60f },
                // District 3: CBD - late-game enemies
                new DistrictProgressionEntry { DistrictIndex = 3, Name = "CBD",        MinProgression = 0.55f, MaxProgression = 0.80f },
                // District 4+: Endgame / Boss area - toughest enemies
                new DistrictProgressionEntry { DistrictIndex = 4, Name = "Endgame",    MinProgression = 0.75f, MaxProgression = 1.00f },
            };
            return config;
        }

        /// <summary>
        /// Gets the progression range for a given district index.
        /// Falls back to a calculated value if no explicit mapping exists.
        /// </summary>
        public void GetProgressionForDistrict(int districtIndex, out float minProg, out float maxProg)
        {
            foreach (var entry in DistrictProgressionOverrides)
            {
                if (entry.DistrictIndex == districtIndex)
                {
                    minProg = entry.MinProgression;
                    maxProg = entry.MaxProgression;
                    return;
                }
            }

            // Fallback: use last known entry or clamp to max
            if (DistrictProgressionOverrides.Count > 0)
            {
                // Use the highest district entry for unknown districts
                var last = DistrictProgressionOverrides[DistrictProgressionOverrides.Count - 1];
                minProg = last.MinProgression;
                maxProg = last.MaxProgression;
            }
            else
            {
                minProg = 0.0f;
                maxProg = 1.0f;
            }
        }

        private const string CONFIG_FILE_NAME = "EnemyProgressionConfig.xml";

        /// <summary>
        /// Saves this configuration to XML in the plugin directory.
        /// </summary>
        public void Save(string pluginPath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ProgressionConfig));
                string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
                Debug.Log("EnemyProgressionMod: Config saved to " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Failed to save config: " + e.Message);
            }
        }

        /// <summary>
        /// Loads configuration from XML. Returns default config if file doesn't exist.
        /// </summary>
        public static ProgressionConfig Load(string pluginPath)
        {
            string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
            if (File.Exists(filePath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(ProgressionConfig));
                    using (var reader = new StreamReader(filePath))
                    {
                        var config = (ProgressionConfig)serializer.Deserialize(reader);
                        Debug.Log("EnemyProgressionMod: Config loaded from " + filePath);
                        return config;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("EnemyProgressionMod: Failed to load config, using defaults: " + e.Message);
                }
            }

            // Create and save defaults
            var defaultConfig = CreateDefault();
            defaultConfig.Save(pluginPath);
            return defaultConfig;
        }
    }

    /// <summary>
    /// Maps a district index to a fixed progression range.
    /// </summary>
    [Serializable]
    public class DistrictProgressionEntry
    {
        /// <summary>
        /// The game district index (0 = Downtown/RedLight, 1 = Industrial, 2 = Grid, 3 = CBD, 4 = Boss).
        /// </summary>
        [XmlAttribute("Index")]
        public int DistrictIndex;

        /// <summary>
        /// Human-readable name for the district (for documentation only).
        /// </summary>
        [XmlAttribute("Name")]
        public string Name = "";

        /// <summary>
        /// Minimum progression value for enemies in this district.
        /// SpawnCards with m_MinProgression below this will be clamped up.
        /// </summary>
        [XmlAttribute("Min")]
        public float MinProgression;

        /// <summary>
        /// Maximum progression value for enemies in this district.
        /// SpawnCards with m_MaxProgression above this will be clamped down.
        /// </summary>
        [XmlAttribute("Max")]
        public float MaxProgression;
    }
}
