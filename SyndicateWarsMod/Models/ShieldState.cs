using System;

namespace SyndicateWarsMod.Models
{
    /// <summary>
    /// Per-agent dual shield state tracking.
    /// Energy Shield: Absorbs projectile/laser damage, regenerates after delay.
    /// Hard Shield: Absorbs physical/explosion damage, no regeneration.
    /// </summary>
    [Serializable]
    public class ShieldState
    {
        /// <summary>
        /// Current Energy Shield points (blue shield, absorbs projectiles and lasers).
        /// </summary>
        public float EnergyShieldCurrent;

        /// <summary>
        /// Maximum Energy Shield capacity.
        /// </summary>
        public float EnergyShieldMax;

        /// <summary>
        /// Energy Shield regeneration rate per second.
        /// </summary>
        public float EnergyShieldRegenRate;

        /// <summary>
        /// Current Hard Shield points (red shield, absorbs physical and explosions).
        /// </summary>
        public float HardShieldCurrent;

        /// <summary>
        /// Maximum Hard Shield capacity.
        /// </summary>
        public float HardShieldMax;

        /// <summary>
        /// Time of last damage taken (for regen delay).
        /// </summary>
        public float LastDamageTime;

        /// <summary>
        /// The equipped Energy Shield tier (0 = none, 1-3 = Mk1-Mk3).
        /// </summary>
        public int EnergyShieldTier;

        /// <summary>
        /// The equipped Hard Shield tier (0 = none, 1-3 = Mk1-Mk3).
        /// </summary>
        public int HardShieldTier;

        public ShieldState()
        {
            EnergyShieldCurrent = 0f;
            EnergyShieldMax = 0f;
            EnergyShieldRegenRate = 0f;
            HardShieldCurrent = 0f;
            HardShieldMax = 0f;
            LastDamageTime = 0f;
            EnergyShieldTier = 0;
            HardShieldTier = 0;
        }

        /// <summary>
        /// Returns a formatted status string for this shield state.
        /// </summary>
        public string GetStatusString()
        {
            string energy = EnergyShieldTier > 0
                ? "Energy Mk" + EnergyShieldTier + ": " + EnergyShieldCurrent.ToString("F0") + "/" + EnergyShieldMax.ToString("F0")
                : "Energy: None";
            string hard = HardShieldTier > 0
                ? "Hard Mk" + HardShieldTier + ": " + HardShieldCurrent.ToString("F0") + "/" + HardShieldMax.ToString("F0")
                : "Hard: None";
            return energy + " | " + hard;
        }
    }
}
