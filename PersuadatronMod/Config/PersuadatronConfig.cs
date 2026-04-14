using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace PersuadatronMod.Config
{
    /// <summary>
    /// Configuration for the Persuadatron mod.
    /// Controls persuasion ranges, cooldowns, implant stats, weapon balance, and follower AI.
    /// Serialized to XML for easy user customization.
    /// </summary>
    [Serializable]
    [XmlRoot("PersuadatronConfig")]
    public class PersuadatronConfig
    {
        #region Persuasion Settings
        /// <summary>
        /// Whether the mod is enabled.
        /// </summary>
        [XmlElement("Enabled")]
        public bool Enabled = true;

        /// <summary>
        /// Range in world units within which the Persuadatron can affect targets.
        /// </summary>
        [XmlElement("PersuasionRange")]
        public float PersuasionRange = 15f;

        /// <summary>
        /// Cooldown in seconds between persuasion attempts.
        /// </summary>
        [XmlElement("PersuasionCooldown")]
        public float PersuasionCooldown = 5f;

        /// <summary>
        /// Duration in seconds that a persuaded unit remains loyal. 0 = permanent.
        /// </summary>
        [XmlElement("PersuasionDuration")]
        public float PersuasionDuration = 0f;

        /// <summary>
        /// Maximum number of simultaneously persuaded followers.
        /// </summary>
        [XmlElement("MaxFollowers")]
        public int MaxFollowers = 8;

        /// <summary>
        /// PowerLevel threshold for Mk1 Persuadatron (civilians only). Not used for target filtering
        /// since Mk1 is civilian-only, but defines the ceiling.
        /// </summary>
        [XmlElement("Mk1PowerLevelMax")]
        public float Mk1PowerLevelMax = 0.0f;

        /// <summary>
        /// PowerLevel threshold for Mk2 Persuadatron (police and light units).
        /// </summary>
        [XmlElement("Mk2PowerLevelMax")]
        public float Mk2PowerLevelMax = 0.25f;

        /// <summary>
        /// PowerLevel threshold for Mk3 Persuadatron (all except strongest/mechs).
        /// </summary>
        [XmlElement("Mk3PowerLevelMax")]
        public float Mk3PowerLevelMax = 0.75f;
        #endregion

        #region Follower AI Settings
        /// <summary>
        /// Distance at which followers try to stay from the Persuadatron carrier.
        /// </summary>
        [XmlElement("FollowDistance")]
        public float FollowDistance = 4f;

        /// <summary>
        /// Distance at which followers sprint to catch up.
        /// </summary>
        [XmlElement("SprintCatchUpDistance")]
        public float SprintCatchUpDistance = 15f;

        /// <summary>
        /// Range within which followers scan for enemies to engage.
        /// </summary>
        [XmlElement("FollowerCombatRange")]
        public float FollowerCombatRange = 20f;

        /// <summary>
        /// Range within which followers scan for dropped weapons to pick up.
        /// </summary>
        [XmlElement("WeaponPickupRange")]
        public float WeaponPickupRange = 10f;

        /// <summary>
        /// How often (in seconds) the follower AI updates.
        /// </summary>
        [XmlElement("FollowerUpdateInterval")]
        public float FollowerUpdateInterval = 0.5f;
        #endregion

        #region Implant Settings
        /// <summary>
        /// Leg implant sprint speed multipliers per tier (Mk1/Mk2/Mk3).
        /// </summary>
        [XmlArray("LegSprintMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> LegSprintMultipliers = new List<float> { 1.15f, 1.30f, 1.50f };

        /// <summary>
        /// Arm implant accuracy bonus per tier (added to AccuracyDelta).
        /// </summary>
        [XmlArray("ArmAccuracyBonuses")]
        [XmlArrayItem("Tier")]
        public List<float> ArmAccuracyBonuses = new List<float> { 0.05f, 0.10f, 0.18f };

        /// <summary>
        /// Arm implant carry capacity multipliers per tier.
        /// </summary>
        [XmlArray("ArmCarryMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> ArmCarryMultipliers = new List<float> { 1.15f, 1.30f, 1.50f };

        /// <summary>
        /// Body implant HP multipliers per tier.
        /// </summary>
        [XmlArray("BodyHPMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> BodyHPMultipliers = new List<float> { 1.20f, 1.40f, 1.60f };

        /// <summary>
        /// Body implant damage resistance bonus per tier (flat addition).
        /// </summary>
        [XmlArray("BodyResistBonuses")]
        [XmlArrayItem("Tier")]
        public List<float> BodyResistBonuses = new List<float> { 0.05f, 0.15f, 0.25f };

        /// <summary>
        /// Body implant HP regen per second per tier. Only Mk3 has regen by default.
        /// </summary>
        [XmlArray("BodyHPRegenPerSecond")]
        [XmlArrayItem("Tier")]
        public List<float> BodyHPRegenPerSecond = new List<float> { 0f, 0f, 2f };

        /// <summary>
        /// Brain implant persuadatron level unlocked per tier (1/2/3).
        /// </summary>
        [XmlArray("BrainPersuadatronLevels")]
        [XmlArrayItem("Tier")]
        public List<int> BrainPersuadatronLevels = new List<int> { 1, 2, 3 };
        #endregion

        #region Item ID Ranges
        /// <summary>
        /// Starting item ID for Persuadatron items. Must not conflict with existing items.
        /// </summary>
        [XmlElement("PersuadatronBaseItemID")]
        public int PersuadatronBaseItemID = 90000;

        /// <summary>
        /// Starting item ID for implant items.
        /// </summary>
        [XmlElement("ImplantBaseItemID")]
        public int ImplantBaseItemID = 90100;

        /// <summary>
        /// Starting item ID for weapon items.
        /// </summary>
        [XmlElement("WeaponBaseItemID")]
        public int WeaponBaseItemID = 90200;
        #endregion

        #region Serialization
        private const string CONFIG_FILE_NAME = "PersuadatronConfig.xml";

        public static PersuadatronConfig CreateDefault()
        {
            return new PersuadatronConfig();
        }

        public void Save(string pluginPath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PersuadatronConfig));
                string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
                Debug.Log("PersuadatronMod: Config saved to " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to save config: " + e.Message);
            }
        }

        public static PersuadatronConfig Load(string pluginPath)
        {
            string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
            if (File.Exists(filePath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(PersuadatronConfig));
                    using (var reader = new StreamReader(filePath))
                    {
                        var config = (PersuadatronConfig)serializer.Deserialize(reader);
                        Debug.Log("PersuadatronMod: Config loaded from " + filePath);
                        return config;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("PersuadatronMod: Failed to load config, using defaults: " + e.Message);
                }
            }

            var defaultConfig = CreateDefault();
            defaultConfig.Save(pluginPath);
            return defaultConfig;
        }
        #endregion
    }
}
