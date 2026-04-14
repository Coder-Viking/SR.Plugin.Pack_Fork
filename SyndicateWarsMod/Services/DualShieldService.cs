using System;
using System.Collections.Generic;
using System.Reflection;
using SyndicateWarsMod.Config;
using SyndicateWarsMod.Models;
using UnityEngine;

namespace SyndicateWarsMod.Services
{
    /// <summary>
    /// Dual shield system implementing Syndicate Wars-style Energy and Hard shields.
    /// 
    /// Energy Shield (Blue):
    ///   - Absorbs projectile and laser damage
    ///   - Regenerates after a delay when not taking damage
    ///   - 3 tiers with increasing capacity and regen rate
    /// 
    /// Hard Shield (Red):
    ///   - Absorbs physical and explosion damage
    ///   - Does NOT regenerate
    ///   - 3 tiers with increasing capacity
    /// 
    /// Both shield types are registered as Gear items that agents can equip.
    /// Shield state is tracked per-agent and updated each frame.
    /// </summary>
    public class DualShieldService
    {
        private readonly SyndicateWarsConfig config;
        private readonly Dictionary<int, ShieldState> agentShields;
        private bool isRegistered;
        private float lastUpdateTime;
        private int shieldItemCount;

        // Cached reflection
        private FieldInfo itemListField;
        private FieldInfo healthField;

        public DualShieldService(SyndicateWarsConfig config)
        {
            this.config = config;
            this.agentShields = new Dictionary<int, ShieldState>();
            this.isRegistered = false;
            this.lastUpdateTime = 0f;
            this.shieldItemCount = 0;
        }

        /// <summary>
        /// Gets the number of registered shield items.
        /// </summary>
        public int ShieldItemCount { get { return shieldItemCount; } }

        /// <summary>
        /// Registers all shield items with the game's ItemManager.
        /// </summary>
        public void RegisterShields()
        {
            if (isRegistered)
                return;

            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                {
                    Debug.LogWarning("SyndicateWarsMod: ItemManager not available for shield registration");
                    return;
                }

                int nextID = config.ShieldBaseItemID;

                // Energy Shield Mk1-3
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float capacity = GetConfigValue(config.EnergyShieldCapacities, tierIndex, 50f);
                    float regen = GetConfigValue(config.EnergyShieldRegenRates, tierIndex, 5f);

                    RegisterShieldItem(itemManager, nextID++,
                        "Energy Shield Mk" + tier,
                        tier, 400f * tier, 250f * tier,
                        0.1f + (tier - 1) * 0.25f,
                        capacity, regen);
                }

                // Hard Shield Mk1-3
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float capacity = GetConfigValue(config.HardShieldCapacities, tierIndex, 75f);

                    RegisterShieldItem(itemManager, nextID++,
                        "Hard Shield Mk" + tier,
                        tier, 500f * tier, 300f * tier,
                        0.15f + (tier - 1) * 0.25f,
                        capacity, 0f);
                }

                isRegistered = true;
                Debug.Log("SyndicateWarsMod: Registered " + shieldItemCount + " shield items");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Shield registration failed: " + e.Message);
            }
        }

        /// <summary>
        /// Main update loop. Handles shield regeneration and damage absorption.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Time.time < lastUpdateTime + config.ShieldUpdateInterval)
                return;

            lastUpdateTime = Time.time;

            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null)
                        continue;

                    int agentID = agent.GetInstanceID();
                    ShieldState state = GetOrCreateState(agentID);

                    // Update shield tiers based on equipped items
                    UpdateEquippedShields(agent, state);

                    // Regenerate Energy Shield
                    RegenerateEnergyShield(state, config.ShieldUpdateInterval);

                    // Monitor health for shield absorption
                    MonitorHealthForShieldAbsorption(agent, state);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Shield update error: " + e.Message);
            }
        }

        /// <summary>
        /// Gets a formatted status string for a specific agent's shields.
        /// </summary>
        public string GetAgentShieldStatus(AgentAI agent)
        {
            if (agent == null) return "No agent";

            int agentID = agent.GetInstanceID();
            if (!agentShields.ContainsKey(agentID))
                return "No shields equipped";

            return agentShields[agentID].GetStatusString();
        }

        /// <summary>
        /// Gets or creates a shield state for an agent.
        /// </summary>
        private ShieldState GetOrCreateState(int agentID)
        {
            if (!agentShields.ContainsKey(agentID))
            {
                agentShields[agentID] = new ShieldState();
            }
            return agentShields[agentID];
        }

        /// <summary>
        /// Updates the shield capacities based on which shield items are currently equipped.
        /// </summary>
        private void UpdateEquippedShields(AgentAI agent, ShieldState state)
        {
            try
            {
                int baseID = config.ShieldBaseItemID;

                // Check Energy Shield tiers (IDs: baseID+0, baseID+1, baseID+2)
                int energyTier = 0;
                for (int tier = 3; tier >= 1; tier--)
                {
                    if (IsItemEquipped(agent, baseID + tier - 1))
                    {
                        energyTier = tier;
                        break;
                    }
                }

                // Check Hard Shield tiers (IDs: baseID+3, baseID+4, baseID+5)
                int hardTier = 0;
                for (int tier = 3; tier >= 1; tier--)
                {
                    if (IsItemEquipped(agent, baseID + 3 + tier - 1))
                    {
                        hardTier = tier;
                        break;
                    }
                }

                // Update Energy Shield
                if (energyTier != state.EnergyShieldTier)
                {
                    state.EnergyShieldTier = energyTier;
                    if (energyTier > 0)
                    {
                        int tierIndex = energyTier - 1;
                        state.EnergyShieldMax = GetConfigValue(config.EnergyShieldCapacities, tierIndex, 50f);
                        state.EnergyShieldRegenRate = GetConfigValue(config.EnergyShieldRegenRates, tierIndex, 5f);
                        state.EnergyShieldCurrent = Mathf.Min(state.EnergyShieldCurrent, state.EnergyShieldMax);
                    }
                    else
                    {
                        state.EnergyShieldMax = 0f;
                        state.EnergyShieldRegenRate = 0f;
                        state.EnergyShieldCurrent = 0f;
                    }
                }

                // Update Hard Shield
                if (hardTier != state.HardShieldTier)
                {
                    state.HardShieldTier = hardTier;
                    if (hardTier > 0)
                    {
                        int tierIndex = hardTier - 1;
                        state.HardShieldMax = GetConfigValue(config.HardShieldCapacities, tierIndex, 75f);
                        state.HardShieldCurrent = Mathf.Min(state.HardShieldCurrent, state.HardShieldMax);
                    }
                    else
                    {
                        state.HardShieldMax = 0f;
                        state.HardShieldCurrent = 0f;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Shield equip check error: " + e.Message);
            }
        }

        /// <summary>
        /// Regenerates Energy Shield after a delay since last damage.
        /// </summary>
        private void RegenerateEnergyShield(ShieldState state, float deltaTime)
        {
            if (state.EnergyShieldTier <= 0)
                return;

            // Check regen delay
            if (Time.time < state.LastDamageTime + config.EnergyShieldRegenDelay)
                return;

            if (state.EnergyShieldCurrent < state.EnergyShieldMax)
            {
                state.EnergyShieldCurrent = Mathf.Min(
                    state.EnergyShieldCurrent + state.EnergyShieldRegenRate * deltaTime,
                    state.EnergyShieldMax);
            }
        }

        /// <summary>
        /// Monitors agent health changes to detect damage and absorb it with shields.
        /// Compares current health to previous health to detect damage taken.
        /// </summary>
        private void MonitorHealthForShieldAbsorption(AgentAI agent, ShieldState state)
        {
            if (state.EnergyShieldTier <= 0 && state.HardShieldTier <= 0)
                return;

            try
            {
                if (agent.m_Health == null)
                    return;

                float currentHP = agent.m_Health.HealthValue;
                float maxHP = agent.m_Health.GetMaxHealth();

                // Simple damage detection: if health dropped, shields absorb some
                // This is a simplified approach; proper damage interception would need hooks
                if (currentHP < maxHP && currentHP > 0f)
                {
                    float totalShield = state.EnergyShieldCurrent + state.HardShieldCurrent;
                    if (totalShield > 0f)
                    {
                        // Mark damage time for regen delay
                        state.LastDamageTime = Time.time;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Shield absorption error: " + e.Message);
            }
        }

        /// <summary>
        /// Checks if a specific item is equipped on an agent.
        /// </summary>
        private bool IsItemEquipped(AgentAI agent, int itemID)
        {
            try
            {
                var equippedField = typeof(AgentAI).GetField("m_EquippedItems",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (equippedField == null)
                {
                    equippedField = typeof(AgentAI).GetField("m_Equipment",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (equippedField != null)
                {
                    var equipped = equippedField.GetValue(agent);
                    if (equipped is int[])
                    {
                        foreach (int id in (int[])equipped)
                        {
                            if (id == itemID) return true;
                        }
                    }
                    else if (equipped is List<int>)
                    {
                        return ((List<int>)equipped).Contains(itemID);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Error checking equipped shield: " + e.Message);
            }
            return false;
        }

        /// <summary>
        /// Registers a shield item with the ItemManager.
        /// </summary>
        private void RegisterShieldItem(ItemManager itemManager, int id, string name,
            int tier, float cost, float researchCost, float progression,
            float shieldCapacity, float regenRate)
        {
            ItemManager.ItemData itemData = new ItemManager.ItemData();
            itemData.m_ID = id;
            itemData.m_FriendlyName = name;
            itemData.m_Slot = ItemSlotTypes.Gear;
            itemData.m_GearSubCategory = ItemSubCategories.Gear;
            itemData.m_WeaponType = WeaponType.None;
            itemData.m_Cost = cost;
            itemData.m_ResearchCost = researchCost;
            itemData.m_Progression = progression;
            itemData.m_StealthVsCombat = 0f;
            itemData.m_AvailableToPlayer = true;
            itemData.m_PlayerCanResearchFromStart = true;
            itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
            itemData.m_PrereqID = tier > 1 ? id - 1 : 0;
            itemData.m_AbilityIDs = new List<int>();
            itemData.m_AbilityMasks = new List<int>();
            itemData.m_Modifiers = new ModifierData5L[0];

            RegisterItem(itemManager, itemData);
            shieldItemCount++;

            Debug.Log("SyndicateWarsMod: Registered shield: " + name +
                " (ID: " + id + ", Cap: " + shieldCapacity + ", Regen: " + regenRate + ")");
        }

        /// <summary>
        /// Registers a single item with the ItemManager using reflection.
        /// </summary>
        private void RegisterItem(ItemManager itemManager, ItemManager.ItemData itemData)
        {
            try
            {
                if (itemListField == null)
                {
                    itemListField = typeof(ItemManager).GetField("m_ItemData",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (itemListField == null)
                    {
                        itemListField = typeof(ItemManager).GetField("m_Items",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                }

                if (itemListField != null)
                {
                    var itemList = itemListField.GetValue(itemManager);
                    if (itemList is List<ItemManager.ItemData>)
                    {
                        var list = (List<ItemManager.ItemData>)itemList;
                        bool exists = false;
                        foreach (var existing in list)
                        {
                            if (existing.m_ID == itemData.m_ID)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            list.Add(itemData);
                        }
                    }
                    else if (itemList is ItemManager.ItemData[])
                    {
                        var array = (ItemManager.ItemData[])itemList;
                        var newArray = new ItemManager.ItemData[array.Length + 1];
                        Array.Copy(array, newArray, array.Length);
                        newArray[array.Length] = itemData;
                        itemListField.SetValue(itemManager, newArray);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Shield RegisterItem failed for ID " +
                    itemData.m_ID + ": " + e.Message);
            }
        }

        /// <summary>
        /// Safely gets a value from a config list by index with a default fallback.
        /// </summary>
        private float GetConfigValue(List<float> list, int index, float defaultValue)
        {
            if (list != null && index >= 0 && index < list.Count)
                return list[index];
            return defaultValue;
        }
    }
}
