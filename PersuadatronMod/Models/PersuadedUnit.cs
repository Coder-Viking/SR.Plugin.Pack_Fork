using UnityEngine;

namespace PersuadatronMod.Models
{
    /// <summary>
    /// Tracks a unit that has been persuaded by the Persuadatron.
    /// Stores reference to the entity, its state, and timing info.
    /// </summary>
    public class PersuadedUnit
    {
        /// <summary>
        /// The game entity representing this persuaded unit.
        /// </summary>
        public AIEntity Entity { get; set; }

        /// <summary>
        /// The transform of the persuaded unit for position tracking.
        /// </summary>
        public Transform Transform
        {
            get { return Entity != null ? Entity.transform : null; }
        }

        /// <summary>
        /// Time.time when this unit was persuaded.
        /// </summary>
        public float PersuadedAtTime { get; set; }

        /// <summary>
        /// Duration in seconds this unit stays persuaded. 0 = permanent.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Whether this unit currently has a weapon equipped.
        /// </summary>
        public bool HasWeapon { get; set; }

        /// <summary>
        /// Whether this unit is currently moving to pick up a weapon.
        /// </summary>
        public bool IsPickingUpWeapon { get; set; }

        /// <summary>
        /// Whether this unit is currently engaging an enemy.
        /// </summary>
        public bool IsInCombat { get; set; }

        /// <summary>
        /// The current enemy target this unit is engaging.
        /// </summary>
        public AIEntity CurrentTarget { get; set; }

        /// <summary>
        /// Estimated power level of this unit (cached from spawn data or HP estimation).
        /// </summary>
        public float PowerLevel { get; set; }

        /// <summary>
        /// Returns true if the unit is still alive and valid.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (Entity == null)
                    return false;

                try
                {
                    var health = Entity.m_Health;
                    return health != null && health.HealthValue > 0f;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if the persuasion duration has expired.
        /// </summary>
        public bool HasExpired
        {
            get
            {
                if (Duration <= 0f)
                    return false; // Permanent
                return Time.time >= PersuadedAtTime + Duration;
            }
        }

        /// <summary>
        /// Returns true if the unit should be removed from the follower list.
        /// </summary>
        public bool ShouldRemove
        {
            get { return !IsAlive || HasExpired; }
        }

        public PersuadedUnit(AIEntity entity, float duration)
        {
            Entity = entity;
            PersuadedAtTime = Time.time;
            Duration = duration;
            HasWeapon = false;
            IsPickingUpWeapon = false;
            IsInCombat = false;
            CurrentTarget = null;
            PowerLevel = 0f;
        }
    }
}
