using System;
using System.Collections.Generic;
using System.Reflection;
using PersuadatronMod.Config;
using PersuadatronMod.Models;
using UnityEngine;

namespace PersuadatronMod.Services
{
    /// <summary>
    /// Core persuasion logic. Handles target validation, range checking,
    /// and persuasion activation based on brain implant level.
    /// 
    /// Persuadatron levels:
    ///   Mk1 (Brain Implant Lvl 1): Only civilians
    ///   Mk2 (Brain Implant Lvl 2): + Police and light units (PowerLevel ≤ 0.25)
    ///   Mk3 (Brain Implant Lvl 3): All units up to PowerLevel ≤ 0.75 (no mechs/strongest)
    /// </summary>
    public class PersuasionService
    {
        private readonly PersuadatronConfig config;
        private readonly ImplantService implantService;
        private float lastPersuasionTime;

        // Cached reflection for accessing entity faction/type info
        private FieldInfo entityFactionField;
        private bool reflectionReady;

        public PersuasionService(PersuadatronConfig config, ImplantService implantService)
        {
            this.config = config;
            this.implantService = implantService;
            this.lastPersuasionTime = 0f;

            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                entityFactionField = typeof(AIEntity).GetField("m_Faction",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                reflectionReady = entityFactionField != null;
            }
            catch (Exception e)
            {
                Debug.LogError("PersuasionService: Reflection init failed: " + e.Message);
                reflectionReady = false;
            }
        }

        /// <summary>
        /// Returns true if the persuasion cooldown has elapsed.
        /// </summary>
        public bool IsReady
        {
            get { return Time.time >= lastPersuasionTime + config.PersuasionCooldown; }
        }

        /// <summary>
        /// Returns the remaining cooldown time in seconds.
        /// </summary>
        public float CooldownRemaining
        {
            get
            {
                float remaining = (lastPersuasionTime + config.PersuasionCooldown) - Time.time;
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>
        /// Gets the current Persuadatron level based on the equipped brain implant.
        /// Returns 0 if no brain implant is equipped.
        /// </summary>
        public int GetCurrentPersuadatronLevel()
        {
            return implantService.GetEquippedBrainImplantLevel();
        }

        /// <summary>
        /// Checks whether a given entity is a valid persuasion target based on
        /// the current brain implant level and target properties.
        /// </summary>
        public bool IsValidTarget(AIEntity entity, int persuadatronLevel)
        {
            if (entity == null)
                return false;

            try
            {
                // Don't persuade player agents
                if (IsPlayerAgent(entity))
                    return false;

                // Check if entity is alive
                var health = entity.m_Health;
                if (health == null || health.HealthValue <= 0f)
                    return false;

                // Get the entity's power level (estimated from health/modifiers)
                float powerLevel = EstimatePowerLevel(entity);

                switch (persuadatronLevel)
                {
                    case 1:
                        // Mk1: Only civilians
                        return IsCivilian(entity);

                    case 2:
                        // Mk2: Civilians + police and light units
                        return IsCivilian(entity) || powerLevel <= config.Mk2PowerLevelMax;

                    case 3:
                        // Mk3: All units up to PowerLevel 0.75
                        return powerLevel <= config.Mk3PowerLevelMax;

                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuasionService: Error checking target validity: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Attempts to persuade a target entity. Returns the PersuadedUnit if successful, null otherwise.
        /// </summary>
        public PersuadedUnit TryPersuade(AIEntity target, int persuadatronLevel, int currentFollowerCount)
        {
            if (!IsReady)
            {
                Debug.Log("PersuadatronMod: Persuadatron is on cooldown (" +
                    CooldownRemaining.ToString("F1") + "s remaining)");
                return null;
            }

            if (currentFollowerCount >= config.MaxFollowers)
            {
                Debug.Log("PersuadatronMod: Maximum followers reached (" + config.MaxFollowers + ")");
                return null;
            }

            if (!IsValidTarget(target, persuadatronLevel))
            {
                Debug.Log("PersuadatronMod: Target is not valid for current Persuadatron level");
                return null;
            }

            // Perform persuasion
            lastPersuasionTime = Time.time;
            float powerLevel = EstimatePowerLevel(target);

            var persuadedUnit = new PersuadedUnit(target, config.PersuasionDuration)
            {
                PowerLevel = powerLevel
            };

            Debug.Log("PersuadatronMod: Successfully persuaded target (PowerLevel: " +
                powerLevel.ToString("F2") + ")");

            return persuadedUnit;
        }

        /// <summary>
        /// Finds all valid persuasion targets within range of the given position.
        /// </summary>
        public List<AIEntity> FindTargetsInRange(Vector3 position, int persuadatronLevel)
        {
            var targets = new List<AIEntity>();

            try
            {
                // Use Physics.OverlapSphere to find nearby entities
                Collider[] colliders = Physics.OverlapSphere(position, config.PersuasionRange);

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    AIEntity entity = collider.GetComponent<AIEntity>();
                    if (entity == null)
                        entity = collider.GetComponentInParent<AIEntity>();

                    if (entity != null && IsValidTarget(entity, persuadatronLevel))
                    {
                        if (!targets.Contains(entity))
                        {
                            targets.Add(entity);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuasionService: Error finding targets: " + e.Message);
            }

            return targets;
        }

        /// <summary>
        /// Estimates the power level of an entity based on its health relative to baseline.
        /// </summary>
        private float EstimatePowerLevel(AIEntity entity)
        {
            try
            {
                var health = entity.m_Health;
                if (health == null)
                    return 1f; // Assume maximum if unknown

                float maxHealth = health.GetMaxHealth();

                // Estimate power level based on max health
                // Civilians typically have ~50 HP, strongest units ~500+ HP
                // Scale: 50 HP = 0.0, 500 HP = 1.0
                const float civilianHP = 50f;
                const float maxEnemyHP = 500f;

                float normalized = (maxHealth - civilianHP) / (maxEnemyHP - civilianHP);
                return Mathf.Clamp01(normalized);
            }
            catch
            {
                return 1f; // Assume maximum power if estimation fails
            }
        }

        /// <summary>
        /// Checks if an entity is a civilian (low power, non-combatant).
        /// </summary>
        private bool IsCivilian(AIEntity entity)
        {
            try
            {
                // Civilians typically have very low HP and no weapons
                float powerLevel = EstimatePowerLevel(entity);
                if (powerLevel > 0.1f)
                    return false;

                // Additional check: civilians typically have no abilities
                var abilities = entity.GetAbilities();
                if (abilities != null)
                {
                    var allAbilities = abilities.AllAbilities();
                    if (allAbilities != null && allAbilities.Count > 2)
                        return false; // Combatants have multiple abilities
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an entity is a player-controlled agent.
        /// </summary>
        private bool IsPlayerAgent(AIEntity entity)
        {
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent != null && agent.gameObject == entity.gameObject)
                        return true;
                }
            }
            catch
            {
                // Ignore errors
            }
            return false;
        }
    }
}
