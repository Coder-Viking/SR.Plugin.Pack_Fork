using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunAndGunMod
{
    /// <summary>
    /// Run &amp; Gun Mod for Satellite Reign.
    /// 
    /// Features:
    /// 1. Multiplies ALL weapon damage (configurable, default 2x).
    ///    Note: This affects all weapons globally (player AND enemy).
    ///    In practice this benefits the player since agents can heal/revive
    ///    and the faster TTK suits aggressive run &amp; gun playstyle.
    /// 
    /// 2. Reduces sprint energy cost for player agents (configurable, default 0.2x).
    ///    This allows ~15 seconds of base sprint without any implants or skills.
    ///    Does NOT increase max energy, so active abilities are unaffected.
    /// </summary>
    public class RunAndGunMod : ISrPlugin
    {
        private bool isInitialized;
        private bool weaponsModified;
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

                // Apply weapon damage multiplier once
                if (!weaponsModified)
                {
                    ApplyDamageMultiplier();
                }

                // Apply sprint modifier to any new/unmodified agents
                ApplySprintModifiers();
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Update error: " + e.Message);
            }
        }

        /// <summary>
        /// Multiplies m_damage_min, m_damage_max and m_shield_damage for every
        /// weapon ammo type registered in WeaponManager.
        /// This is a global change affecting ALL entities (player + enemy).
        /// </summary>
        private void ApplyDamageMultiplier()
        {
            try
            {
                var weaponManager = Manager.GetWeaponManager();
                if (weaponManager == null || weaponManager.m_WeaponData == null)
                    return;

                float multiplier = config.DamageMultiplier;
                if (Math.Abs(multiplier - 1.0f) < 0.001f)
                {
                    // Multiplier is 1.0, no change needed
                    weaponsModified = true;
                    Debug.Log("RunAndGunMod: Damage multiplier is 1.0, skipping weapon modification.");
                    return;
                }

                int modifiedCount = 0;

                foreach (var weaponData in weaponManager.m_WeaponData)
                {
                    if (weaponData == null || weaponData.m_Ammo == null)
                        continue;

                    foreach (var ammo in weaponData.m_Ammo)
                    {
                        if (ammo == null)
                            continue;

                        ammo.m_damage_min *= multiplier;
                        ammo.m_damage_max *= multiplier;
                        ammo.m_shield_damage *= multiplier;
                        modifiedCount++;
                    }
                }

                weaponsModified = true;
                Debug.Log(string.Format(
                    "RunAndGunMod: Damage multiplied by {0}x for {1} weapon ammo types.",
                    multiplier, modifiedCount));

                if (config.ShowActivationMessage && Manager.GetUIManager() != null)
                {
                    Manager.GetUIManager().ShowMessagePopup(
                        string.Format("Run & Gun active!\nWeapon damage x{0}\nSprint cost x{1}",
                            config.DamageMultiplier, config.SprintEnergyCostMultiplier),
                        7);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Error applying damage multiplier: " + e.Message);
            }
        }

        /// <summary>
        /// Adds a SprintEnergyCostMultiplier modifier to each player agent's
        /// default modifier list. Tracked per-agent to avoid duplicate application.
        /// This reduces sprint energy drain without touching max energy or ability costs.
        /// </summary>
        private void ApplySprintModifiers()
        {
            try
            {
                float sprintMultiplier = config.SprintEnergyCostMultiplier;
                if (Math.Abs(sprintMultiplier - 1.0f) < 0.001f)
                    return; // No change needed

                var agents = AgentAI.GetAgents();
                if (agents == null)
                    return;

                foreach (var agent in agents)
                {
                    if (agent == null || agent.m_Modifiers == null)
                        continue;

                    int agentId = agent.GetInstanceID();
                    if (modifiedAgentIds.Contains(agentId))
                        continue;

                    // Check if agent already has a SprintEnergyCostMultiplier modifier
                    // from this mod (avoid stacking if somehow already present)
                    bool alreadyHasModifier = false;
                    if (agent.m_Modifiers.m_DefaultModifiers != null)
                    {
                        foreach (var mod in agent.m_Modifiers.m_DefaultModifiers)
                        {
                            if (mod.m_Type == ModifierType.SprintEnergyCostMultiplier)
                            {
                                alreadyHasModifier = true;
                                break;
                            }
                        }
                    }

                    if (!alreadyHasModifier)
                    {
                        var sprintMod = new ModifierData5L();
                        sprintMod.m_Type = ModifierType.SprintEnergyCostMultiplier;
                        sprintMod.m_Ammount = sprintMultiplier;
                        sprintMod.m_TimeOut = 0f;
                        sprintMod.m_AmountModifier = ModifierType.SprintEnergyCostMultiplier;

                        agent.m_Modifiers.m_DefaultModifiers.Add(sprintMod);

                        Debug.Log(string.Format(
                            "RunAndGunMod: Applied sprint cost x{0} to agent {1}.",
                            sprintMultiplier, agent.GetName()));
                    }

                    modifiedAgentIds.Add(agentId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("RunAndGunMod: Error applying sprint modifiers: " + e.Message);
            }
        }
    }
}
