using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunAndGunMod
{
    /// <summary>
    /// Run &amp; Gun Mod for Satellite Reign.
    /// 
    /// Features:
    /// 1. Adds a WeaponDamageMultiplier modifier to each player agent (default 2x).
    ///    Only YOUR agents deal more damage — enemies are unaffected.
    ///    Works the same way as the Soldier's Rage ability damage boost.
    /// 
    /// 2. Reduces sprint energy cost for player agents (configurable, default 0.2x).
    ///    This allows ~15 seconds of base sprint without any implants or skills.
    ///    Does NOT increase max energy, so active abilities are unaffected.
    /// </summary>
    public class RunAndGunMod : ISrPlugin
    {
        private bool isInitialized;
        private bool messageShown;
        private bool configLoaded;
        private RunAndGunConfig config;
        private HashSet<int> modifiedAgentIds = new HashSet<int>();

        public string GetName()
        {
            return "Run & Gun Mod v1.0";
        }

        public void Initialize()
        {
            try
            {
                Debug.Log("RunAndGunMod: Initializing...");
                isInitialized = true;
                Debug.Log("RunAndGunMod: Initialization complete. Waiting for game to start.");
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Initialization failed: " + e.Message);
            }
        }

        public void Update()
        {
            if (!isInitialized || !Manager.Get().GameInProgress)
                return;

            try
            {
                // Load config on first game-ready frame (Manager must be ready for PluginPath)
                if (!configLoaded)
                {
                    config = RunAndGunConfig.Load();
                    configLoaded = true;
                }

                // Apply modifiers to any new/unmodified agents
                ApplyAgentModifiers();
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Update error: " + e.Message);
            }
        }

        /// <summary>
        /// Applies WeaponDamageMultiplier and SprintEnergyCostMultiplier modifiers
        /// to each player agent. Tracked per-agent via GetInstanceID() to avoid
        /// duplicate application. Only affects player agents (AgentAI.GetAgents()),
        /// not enemies.
        /// </summary>
        private void ApplyAgentModifiers()
        {
            try
            {
                var agents = AgentAI.GetAgents();
                if (agents == null)
                    return;

                float damageMultiplier = config.DamageMultiplier;
                float sprintMultiplier = config.SprintEnergyCostMultiplier;

                foreach (var agent in agents)
                {
                    if (agent == null || agent.m_Modifiers == null)
                        continue;

                    int agentId = agent.GetInstanceID();
                    if (modifiedAgentIds.Contains(agentId))
                        continue;

                    // Apply weapon damage multiplier (like the Soldier's Rage ability)
                    if (Math.Abs(damageMultiplier - 1.0f) > 0.001f)
                    {
                        bool hasDamageMod = false;
                        if (agent.m_Modifiers.m_DefaultModifiers != null)
                        {
                            foreach (var mod in agent.m_Modifiers.m_DefaultModifiers)
                            {
                                if (mod.m_Type == ModifierType.WeaponDamageMultiplier)
                                {
                                    hasDamageMod = true;
                                    break;
                                }
                            }
                        }

                        if (!hasDamageMod)
                        {
                            var damageMod = new ModifierData5L();
                            damageMod.m_Type = ModifierType.WeaponDamageMultiplier;
                            damageMod.m_Ammount = damageMultiplier;
                            damageMod.m_TimeOut = 0f;
                            damageMod.m_AmountModifier = ModifierType.NONE;

                            agent.m_Modifiers.AddModifier(damageMod);

                            Debug.Log(string.Format(
                                "RunAndGunMod: Applied damage x{0} to agent {1}.",
                                damageMultiplier, agent.GetName()));
                        }
                    }

                    // Apply sprint energy cost reduction
                    if (Math.Abs(sprintMultiplier - 1.0f) > 0.001f)
                    {
                        bool hasSprintMod = false;
                        if (agent.m_Modifiers.m_DefaultModifiers != null)
                        {
                            foreach (var mod in agent.m_Modifiers.m_DefaultModifiers)
                            {
                                if (mod.m_Type == ModifierType.SprintEnergyCostMultiplier)
                                {
                                    hasSprintMod = true;
                                    break;
                                }
                            }
                        }

                        if (!hasSprintMod)
                        {
                            var sprintMod = new ModifierData5L();
                            sprintMod.m_Type = ModifierType.SprintEnergyCostMultiplier;
                            sprintMod.m_Ammount = sprintMultiplier;
                            sprintMod.m_TimeOut = 0f;
                            sprintMod.m_AmountModifier = ModifierType.SprintEnergyCostMultiplier;

                            agent.m_Modifiers.AddModifier(sprintMod);

                            Debug.Log(string.Format(
                                "RunAndGunMod: Applied sprint cost x{0} to agent {1}.",
                                sprintMultiplier, agent.GetName()));
                        }
                    }

                    modifiedAgentIds.Add(agentId);
                }

                // Show activation message once after first agent is modified
                if (!messageShown && modifiedAgentIds.Count > 0
                    && config.ShowActivationMessage && Manager.GetUIManager() != null)
                {
                    Manager.GetUIManager().ShowMessagePopup(
                        string.Format("Run & Gun active!\nAgent damage x{0}\nSprint cost x{1}",
                            config.DamageMultiplier, config.SprintEnergyCostMultiplier),
                        7);
                    messageShown = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Error applying agent modifiers: " + e.Message);
            }
        }
    }
}
