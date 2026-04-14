using System;
using System.Collections.Generic;
using SyndicateWarsMod.Config;
using UnityEngine;

namespace SyndicateWarsMod.Services
{
    /// <summary>
    /// Manages environment interaction effects for Syndicate Wars-style gameplay:
    /// 
    /// 1. Satellite Rain - Orbital strike at a target position (hotkey-activated)
    ///    - 3-second delay after activation
    ///    - Massive AOE damage at target position
    ///    - 60-second cooldown
    /// 
    /// 2. Chain Reactions - When explosive damage occurs near vehicles/objects,
    ///    nearby entities take secondary damage
    /// 
    /// 3. AOE Knockback Zones - Graviton Gun hits create temporary knockback zones
    /// </summary>
    public class EnvironmentEffectsService
    {
        private readonly SyndicateWarsConfig config;

        // Satellite Rain state
        private bool satRainPending;
        private float satRainActivationTime;
        private Vector3 satRainTargetPosition;
        private float satRainLastUseTime;

        // Active AOE zones
        private readonly List<AOEZone> activeZones;

        public EnvironmentEffectsService(SyndicateWarsConfig config)
        {
            this.config = config;
            this.satRainPending = false;
            this.satRainActivationTime = 0f;
            this.satRainTargetPosition = Vector3.zero;
            this.satRainLastUseTime = -999f;
            this.activeZones = new List<AOEZone>();
        }

        /// <summary>
        /// Whether Satellite Rain is ready to use (cooldown elapsed).
        /// </summary>
        public bool IsSatelliteRainReady
        {
            get { return Time.time >= satRainLastUseTime + config.SatelliteRainCooldown; }
        }

        /// <summary>
        /// Remaining cooldown for Satellite Rain in seconds.
        /// </summary>
        public float SatelliteRainCooldown
        {
            get
            {
                float remaining = (satRainLastUseTime + config.SatelliteRainCooldown) - Time.time;
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>
        /// Activates Satellite Rain at the selected agent's current look-at position.
        /// </summary>
        public void TriggerSatelliteRain()
        {
            try
            {
                if (!IsSatelliteRainReady)
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Satellite Rain: Cooldown - " + SatelliteRainCooldown.ToString("F0") + "s", 2);
                    return;
                }

                // Get target position from selected agent
                AgentAI agent = AgentAI.FirstSelectedAgentAi();
                if (agent == null)
                {
                    Manager.GetUIManager().ShowSubtitle("Satellite Rain: No agent selected!", 2);
                    return;
                }

                // Use agent's forward position as target
                satRainTargetPosition = agent.transform.position + agent.transform.forward * 15f;
                satRainPending = true;
                satRainActivationTime = Time.time;
                satRainLastUseTime = Time.time;

                Manager.GetUIManager().ShowSubtitle(
                    "SATELLITE RAIN INCOMING! Impact in " +
                    config.SatelliteRainDelay.ToString("F0") + " seconds!", 3);

                Debug.Log("SyndicateWarsMod: Satellite Rain activated at " + satRainTargetPosition);
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Satellite Rain activation error: " + e.Message);
            }
        }

        /// <summary>
        /// Main update loop. Processes pending Satellite Rain impacts and active AOE zones.
        /// </summary>
        public void Update(float deltaTime)
        {
            // Process pending Satellite Rain
            if (satRainPending)
            {
                ProcessSatelliteRain();
            }

            // Process active AOE zones
            ProcessActiveZones(deltaTime);
        }

        /// <summary>
        /// Creates a temporary AOE knockback zone at a position (for Graviton Gun effects).
        /// </summary>
        public void CreateKnockbackZone(Vector3 position, float radius, float damage, float duration)
        {
            activeZones.Add(new AOEZone
            {
                Position = position,
                Radius = radius,
                DamagePerSecond = damage,
                RemainingDuration = duration,
                KnockbackForce = 3f
            });

            Debug.Log("SyndicateWarsMod: Created knockback zone at " + position +
                " (radius: " + radius + ", duration: " + duration + "s)");
        }

        /// <summary>
        /// Gets a formatted status string for the environment effects system.
        /// </summary>
        public string GetStatusString()
        {
            string satRainStatus = IsSatelliteRainReady
                ? "READY"
                : "Cooldown: " + SatelliteRainCooldown.ToString("F0") + "s";

            if (satRainPending)
            {
                float timeToImpact = (satRainActivationTime + config.SatelliteRainDelay) - Time.time;
                satRainStatus = "INCOMING (" + timeToImpact.ToString("F1") + "s)";
            }

            return "Satellite Rain: " + satRainStatus + " | Active AOE Zones: " + activeZones.Count;
        }

        /// <summary>
        /// Processes the pending Satellite Rain impact.
        /// </summary>
        private void ProcessSatelliteRain()
        {
            float timeSinceActivation = Time.time - satRainActivationTime;

            if (timeSinceActivation >= config.SatelliteRainDelay)
            {
                // IMPACT!
                ExecuteSatelliteRainImpact();
                satRainPending = false;
            }
        }

        /// <summary>
        /// Executes the Satellite Rain impact: damages all entities within radius.
        /// </summary>
        private void ExecuteSatelliteRainImpact()
        {
            try
            {
                Manager.GetUIManager().ShowSubtitle("*** SATELLITE RAIN IMPACT ***", 3);

                Collider[] colliders = Physics.OverlapSphere(
                    satRainTargetPosition, config.SatelliteRainRadius);

                int entitiesHit = 0;

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    // Find AIEntity on this collider
                    AIEntity entity = collider.GetComponent<AIEntity>();
                    if (entity == null)
                        entity = collider.GetComponentInParent<AIEntity>();

                    if (entity == null)
                        continue;

                    // Don't damage player agents
                    bool isPlayer = false;
                    foreach (AgentAI agent in AgentAI.GetAgents())
                    {
                        if (agent != null && agent.gameObject == entity.gameObject)
                        {
                            isPlayer = true;
                            break;
                        }
                    }

                    if (isPlayer)
                        continue;

                    // Apply damage
                    try
                    {
                        if (entity.m_Health != null && entity.m_Health.HealthValue > 0f)
                        {
                            // Calculate distance-based damage falloff
                            float distance = Vector3.Distance(
                                entity.transform.position, satRainTargetPosition);
                            float falloff = 1f - (distance / config.SatelliteRainRadius);
                            float damage = config.SatelliteRainDamage * Mathf.Max(falloff, 0.2f);

                            float newHP = Mathf.Max(0f, entity.m_Health.HealthValue - damage);
                            entity.m_Health.SetHealthValue(newHP);
                            entitiesHit++;
                        }
                    }
                    catch
                    {
                        // Individual entity damage might fail, continue with others
                    }
                }

                // Apply chain reaction damage to nearby objects
                ApplyChainReaction(satRainTargetPosition, config.SatelliteRainDamage);

                Debug.Log("SyndicateWarsMod: Satellite Rain impact - " + entitiesHit + " entities hit");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Satellite Rain impact error: " + e.Message);
            }
        }

        /// <summary>
        /// Applies chain reaction damage to entities near an explosion point.
        /// </summary>
        private void ApplyChainReaction(Vector3 center, float baseDamage)
        {
            try
            {
                Collider[] colliders = Physics.OverlapSphere(center, config.ChainReactionRadius);

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    // Look for vehicles (CarAI)
                    CarAI car = collider.GetComponent<CarAI>();
                    if (car == null)
                        car = collider.GetComponentInParent<CarAI>();

                    if (car != null)
                    {
                        // Vehicle found - apply chain reaction damage to nearby entities
                        float chainDamage = baseDamage * config.ChainReactionDamageMultiplier;
                        Collider[] nearbyColliders = Physics.OverlapSphere(
                            car.transform.position, config.ChainReactionRadius * 0.5f);

                        foreach (Collider nearby in nearbyColliders)
                        {
                            if (nearby == null)
                                continue;

                            AIEntity entity = nearby.GetComponent<AIEntity>();
                            if (entity == null)
                                entity = nearby.GetComponentInParent<AIEntity>();

                            if (entity != null && entity.m_Health != null && entity.m_Health.HealthValue > 0f)
                            {
                                // Skip player agents
                                bool isPlayer = false;
                                foreach (AgentAI agent in AgentAI.GetAgents())
                                {
                                    if (agent != null && agent.gameObject == entity.gameObject)
                                    {
                                        isPlayer = true;
                                        break;
                                    }
                                }

                                if (!isPlayer)
                                {
                                    float newHP = Mathf.Max(0f, entity.m_Health.HealthValue - chainDamage);
                                    entity.m_Health.SetHealthValue(newHP);
                                }
                            }
                        }

                        Debug.Log("SyndicateWarsMod: Chain reaction from vehicle at " + car.transform.position);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Chain reaction error: " + e.Message);
            }
        }

        /// <summary>
        /// Processes all active AOE zones (damage ticks, duration countdown).
        /// </summary>
        private void ProcessActiveZones(float deltaTime)
        {
            for (int i = activeZones.Count - 1; i >= 0; i--)
            {
                AOEZone zone = activeZones[i];
                zone.RemainingDuration -= deltaTime;

                if (zone.RemainingDuration <= 0f)
                {
                    activeZones.RemoveAt(i);
                    continue;
                }

                // Apply damage to entities in the zone
                try
                {
                    Collider[] colliders = Physics.OverlapSphere(zone.Position, zone.Radius);
                    foreach (Collider collider in colliders)
                    {
                        if (collider == null)
                            continue;

                        AIEntity entity = collider.GetComponent<AIEntity>();
                        if (entity == null)
                            entity = collider.GetComponentInParent<AIEntity>();

                        if (entity != null && entity.m_Health != null && entity.m_Health.HealthValue > 0f)
                        {
                            // Skip player agents
                            bool isPlayer = false;
                            foreach (AgentAI agent in AgentAI.GetAgents())
                            {
                                if (agent != null && agent.gameObject == entity.gameObject)
                                {
                                    isPlayer = true;
                                    break;
                                }
                            }

                            if (!isPlayer)
                            {
                                float damage = zone.DamagePerSecond * deltaTime;
                                float newHP = Mathf.Max(0f, entity.m_Health.HealthValue - damage);
                                entity.m_Health.SetHealthValue(newHP);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("SyndicateWarsMod: AOE zone error: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Internal class representing an active area-of-effect damage zone.
        /// </summary>
        private class AOEZone
        {
            public Vector3 Position;
            public float Radius;
            public float DamagePerSecond;
            public float RemainingDuration;
            public float KnockbackForce;
        }
    }
}
