using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SyndicateWarsMod.Config
{
    /// <summary>
    /// Master configuration for the Syndicate Wars Mod.
    /// Controls all sub-systems: Arsenal, Implants, IPA, Shields, and Environment Effects.
    /// Serialized to XML for easy user customization.
    /// </summary>
    [Serializable]
    [XmlRoot("SyndicateWarsConfig")]
    public class SyndicateWarsConfig
    {
        #region General Settings
        /// <summary>
        /// Whether the mod is enabled.
        /// </summary>
        [XmlElement("Enabled")]
        public bool Enabled = true;
        #endregion

        #region Arsenal Settings
        /// <summary>
        /// Starting item ID for new Syndicate Wars weapons. Must not conflict with other mods.
        /// PersuadatronMod uses 90000-90299, so we start at 90300.
        /// </summary>
        [XmlElement("WeaponBaseItemID")]
        public int WeaponBaseItemID = 90300;
        #endregion

        #region Implant Settings
        /// <summary>
        /// Starting item ID for enhanced implants.
        /// </summary>
        [XmlElement("ImplantBaseItemID")]
        public int ImplantBaseItemID = 90400;

        /// <summary>
        /// Brain implant accuracy bonuses per tier (Eyes combo effect).
        /// </summary>
        [XmlArray("BrainAccuracyBonuses")]
        [XmlArrayItem("Tier")]
        public List<float> BrainAccuracyBonuses = new List<float> { 0.05f, 0.10f, 0.18f };

        /// <summary>
        /// Brain implant weapon range multipliers per tier (Eyes combo effect).
        /// </summary>
        [XmlArray("BrainRangeMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> BrainRangeMultipliers = new List<float> { 1.0f, 1.15f, 1.30f };

        /// <summary>
        /// Body implant HP multipliers per tier.
        /// </summary>
        [XmlArray("BodyHPMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> BodyHPMultipliers = new List<float> { 1.20f, 1.40f, 1.60f };

        /// <summary>
        /// Body implant damage resistance bonuses per tier (Chest effect).
        /// </summary>
        [XmlArray("BodyResistBonuses")]
        [XmlArrayItem("Tier")]
        public List<float> BodyResistBonuses = new List<float> { 0.05f, 0.15f, 0.25f };

        /// <summary>
        /// Body implant HP regen per second per tier (Heart combo effect).
        /// </summary>
        [XmlArray("BodyHPRegenPerSecond")]
        [XmlArrayItem("Tier")]
        public List<float> BodyHPRegenPerSecond = new List<float> { 1f, 3f, 5f };

        /// <summary>
        /// Body implant energy regen per second per tier (Heart combo effect).
        /// </summary>
        [XmlArray("BodyEnergyRegenPerSecond")]
        [XmlArrayItem("Tier")]
        public List<float> BodyEnergyRegenPerSecond = new List<float> { 0f, 2f, 4f };

        /// <summary>
        /// Arm implant accuracy bonuses per tier.
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
        /// Leg implant sprint speed multipliers per tier.
        /// </summary>
        [XmlArray("LegSprintMultipliers")]
        [XmlArrayItem("Tier")]
        public List<float> LegSprintMultipliers = new List<float> { 1.15f, 1.30f, 1.50f };
        #endregion

        #region IPA Settings
        /// <summary>
        /// Whether the IPA system is enabled.
        /// </summary>
        [XmlElement("IPAEnabled")]
        public bool IPAEnabled = true;

        /// <summary>
        /// Adrenaline drain rate per second when adrenaline is above 0.
        /// </summary>
        [XmlElement("AdrenalineDrainRate")]
        public float AdrenalineDrainRate = 0.02f;

        /// <summary>
        /// Adrenaline threshold below which exhaustion kicks in.
        /// </summary>
        [XmlElement("AdrenalineExhaustionThreshold")]
        public float AdrenalineExhaustionThreshold = 0.1f;

        /// <summary>
        /// Adrenaline level above which the exhaustion flag is cleared.
        /// </summary>
        [XmlElement("AdrenalineRecoveryThreshold")]
        public float AdrenalineRecoveryThreshold = 0.3f;

        /// <summary>
        /// Maximum speed multiplier bonus from high adrenaline.
        /// </summary>
        [XmlElement("AdrenalineMaxSpeedBonus")]
        public float AdrenalineMaxSpeedBonus = 0.5f;

        /// <summary>
        /// Speed penalty multiplier when exhausted.
        /// </summary>
        [XmlElement("AdrenalineExhaustionPenalty")]
        public float AdrenalineExhaustionPenalty = -0.3f;

        /// <summary>
        /// Maximum accuracy bonus from high perception.
        /// </summary>
        [XmlElement("PerceptionMaxAccuracyBonus")]
        public float PerceptionMaxAccuracyBonus = 0.20f;

        /// <summary>
        /// Maximum crit chance bonus from high perception.
        /// </summary>
        [XmlElement("PerceptionMaxCritBonus")]
        public float PerceptionMaxCritBonus = 0.15f;

        /// <summary>
        /// How often (seconds) the IPA system updates agent stats.
        /// </summary>
        [XmlElement("IPAUpdateInterval")]
        public float IPAUpdateInterval = 0.5f;

        /// <summary>
        /// Step size when adjusting IPA values via hotkeys.
        /// </summary>
        [XmlElement("IPAAdjustStep")]
        public float IPAAdjustStep = 0.1f;
        #endregion

        #region Shield Settings
        /// <summary>
        /// Starting item ID for shield items.
        /// </summary>
        [XmlElement("ShieldBaseItemID")]
        public int ShieldBaseItemID = 90500;

        /// <summary>
        /// Energy Shield capacity per tier (Mk1/Mk2/Mk3).
        /// </summary>
        [XmlArray("EnergyShieldCapacities")]
        [XmlArrayItem("Tier")]
        public List<float> EnergyShieldCapacities = new List<float> { 50f, 100f, 200f };

        /// <summary>
        /// Energy Shield regen rates per tier (points per second).
        /// </summary>
        [XmlArray("EnergyShieldRegenRates")]
        [XmlArrayItem("Tier")]
        public List<float> EnergyShieldRegenRates = new List<float> { 5f, 10f, 20f };

        /// <summary>
        /// Delay in seconds after taking damage before Energy Shield starts regenerating.
        /// </summary>
        [XmlElement("EnergyShieldRegenDelay")]
        public float EnergyShieldRegenDelay = 3f;

        /// <summary>
        /// Hard Shield capacity per tier (Mk1/Mk2/Mk3).
        /// </summary>
        [XmlArray("HardShieldCapacities")]
        [XmlArrayItem("Tier")]
        public List<float> HardShieldCapacities = new List<float> { 75f, 150f, 300f };

        /// <summary>
        /// How often (seconds) the shield system updates.
        /// </summary>
        [XmlElement("ShieldUpdateInterval")]
        public float ShieldUpdateInterval = 0.25f;
        #endregion

        #region Environment Effects Settings
        /// <summary>
        /// Satellite Rain damage radius.
        /// </summary>
        [XmlElement("SatelliteRainRadius")]
        public float SatelliteRainRadius = 15f;

        /// <summary>
        /// Satellite Rain damage amount.
        /// </summary>
        [XmlElement("SatelliteRainDamage")]
        public float SatelliteRainDamage = 500f;

        /// <summary>
        /// Satellite Rain delay before impact (seconds).
        /// </summary>
        [XmlElement("SatelliteRainDelay")]
        public float SatelliteRainDelay = 3f;

        /// <summary>
        /// Satellite Rain cooldown (seconds).
        /// </summary>
        [XmlElement("SatelliteRainCooldown")]
        public float SatelliteRainCooldown = 60f;

        /// <summary>
        /// Chain reaction radius for explosive environment effects.
        /// </summary>
        [XmlElement("ChainReactionRadius")]
        public float ChainReactionRadius = 8f;

        /// <summary>
        /// Chain reaction damage multiplier (relative to trigger damage).
        /// </summary>
        [XmlElement("ChainReactionDamageMultiplier")]
        public float ChainReactionDamageMultiplier = 0.5f;
        #endregion

        #region Serialization
        private const string CONFIG_FILE_NAME = "SyndicateWarsConfig.xml";

        public static SyndicateWarsConfig CreateDefault()
        {
            return new SyndicateWarsConfig();
        }

        public void Save(string pluginPath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(SyndicateWarsConfig));
                string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
                Debug.Log("SyndicateWarsMod: Config saved to " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Failed to save config: " + e.Message);
            }
        }

        public static SyndicateWarsConfig Load(string pluginPath)
        {
            string filePath = Path.Combine(pluginPath, CONFIG_FILE_NAME);
            if (File.Exists(filePath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(SyndicateWarsConfig));
                    using (var reader = new StreamReader(filePath))
                    {
                        var config = (SyndicateWarsConfig)serializer.Deserialize(reader);
                        Debug.Log("SyndicateWarsMod: Config loaded from " + filePath);
                        return config;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("SyndicateWarsMod: Failed to load config, using defaults: " + e.Message);
                }
            }

            var defaultConfig = CreateDefault();
            defaultConfig.Save(pluginPath);
            return defaultConfig;
        }
        #endregion
    }
}
