using System;
using System.Collections.Generic;
using System.Reflection;
using SyndicateWarsMod.Config;
using UnityEngine;

namespace SyndicateWarsMod.Services
{
    /// <summary>
    /// Enhanced implant system with Syndicate Wars-style combo effects.
    /// Maps 6 Syndicate Wars body areas to Satellite Reign's 4 augmentation slots:
    ///   Brain + Eyes → AugmentationHead (accuracy, weapon range, persuadatron level)
    ///   Heart + Chest → AugmentationBody (HP, damage resist, HP regen, energy regen)
    ///   Arms → AugmentationArms (accuracy, carry capacity, reload speed)
    ///   Legs → AugmentationLegs (sprint speed, movement)
    /// 
    /// Each slot has 3 tiers (Mk1/Mk2/Mk3) with progressively stronger bonuses.
    /// </summary>
    public class SWImplantService
    {
        private readonly SyndicateWarsConfig config;
        private bool isRegistered;
        private int implantCount;

        // Cached reflection
        private FieldInfo itemListField;

        public SWImplantService(SyndicateWarsConfig config)
        {
            this.config = config;
            this.isRegistered = false;
            this.implantCount = 0;
        }

        /// <summary>
        /// Gets the number of registered implants.
        /// </summary>
        public int ImplantCount { get { return implantCount; } }

        /// <summary>
        /// Registers all enhanced implants with the game's ItemManager.
        /// </summary>
        public void RegisterImplants()
        {
            if (isRegistered)
                return;

            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                {
                    Debug.LogWarning("SyndicateWarsMod: ItemManager not available for implant registration");
                    return;
                }

                int nextID = config.ImplantBaseItemID;

                // Brain + Eyes combo implants (AugmentationHead = slot 1)
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float accuracyBonus = GetConfigValue(config.BrainAccuracyBonuses, tierIndex, 0f);
                    float rangeMult = GetConfigValue(config.BrainRangeMultipliers, tierIndex, 1f);

                    var modifiers = new List<ModifierData5L>();
                    // Accuracy modifier (Add)
                    if (accuracyBonus > 0f)
                    {
                        modifiers.Add(new ModifierData5L
                        {
                            m_Type = (ModifierType)1, // Add
                            m_Ammount = accuracyBonus,
                            m_AmountModifier = ModifierType.NONE,
                            m_TimeOut = 0f
                        });
                    }
                    // Weapon range multiplier (Multiply)
                    if (rangeMult > 1f)
                    {
                        modifiers.Add(new ModifierData5L
                        {
                            m_Type = (ModifierType)2, // Multiply
                            m_Ammount = rangeMult,
                            m_AmountModifier = ModifierType.NONE,
                            m_TimeOut = 0f
                        });
                    }

                    var abilities = new List<int>();
                    if (tier >= 2) abilities.Add(1317); // Hack_Target
                    if (tier >= 3) abilities.Add(1331); // World_Scan

                    RegisterImplantItem(itemManager, nextID++,
                        "Neural Cortex+ Mk" + tier,
                        ItemSlotTypes.AugmentationHead,
                        tier, 800f * tier, 500f * tier,
                        0.1f + (tier - 1) * 0.25f,
                        modifiers.ToArray(), abilities);
                }

                // Heart + Chest combo implants (AugmentationBody = slot 2)
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float hpMult = GetConfigValue(config.BodyHPMultipliers, tierIndex, 1f);
                    float resistBonus = GetConfigValue(config.BodyResistBonuses, tierIndex, 0f);

                    var modifiers = new List<ModifierData5L>();
                    // HP multiplier (Multiply)
                    modifiers.Add(new ModifierData5L
                    {
                        m_Type = (ModifierType)2, // Multiply
                        m_Ammount = hpMult,
                        m_AmountModifier = ModifierType.NONE,
                        m_TimeOut = 0f
                    });
                    // Damage resistance (Add)
                    if (resistBonus > 0f)
                    {
                        modifiers.Add(new ModifierData5L
                        {
                            m_Type = (ModifierType)1, // Add
                            m_Ammount = resistBonus,
                            m_AmountModifier = ModifierType.NONE,
                            m_TimeOut = 0f
                        });
                    }

                    RegisterImplantItem(itemManager, nextID++,
                        "Cybernetic Torso+ Mk" + tier,
                        ItemSlotTypes.AugmentationBody,
                        tier, 700f * tier, 400f * tier,
                        0.15f + (tier - 1) * 0.25f,
                        modifiers.ToArray(), new List<int>());
                }

                // Arm implants (AugmentationArms = slot 3)
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float accuracyBonus = GetConfigValue(config.ArmAccuracyBonuses, tierIndex, 0f);
                    float carryMult = GetConfigValue(config.ArmCarryMultipliers, tierIndex, 1f);

                    var modifiers = new List<ModifierData5L>();
                    // Accuracy bonus (Add)
                    if (accuracyBonus > 0f)
                    {
                        modifiers.Add(new ModifierData5L
                        {
                            m_Type = (ModifierType)1, // Add
                            m_Ammount = accuracyBonus,
                            m_AmountModifier = ModifierType.NONE,
                            m_TimeOut = 0f
                        });
                    }
                    // Carry capacity multiplier (Multiply)
                    modifiers.Add(new ModifierData5L
                    {
                        m_Type = (ModifierType)2, // Multiply
                        m_Ammount = carryMult,
                        m_AmountModifier = ModifierType.NONE,
                        m_TimeOut = 0f
                    });

                    RegisterImplantItem(itemManager, nextID++,
                        "Cybernetic Arms+ Mk" + tier,
                        ItemSlotTypes.AugmentationArms,
                        tier, 600f * tier, 350f * tier,
                        0.1f + (tier - 1) * 0.25f,
                        modifiers.ToArray(), new List<int>());
                }

                // Leg implants (AugmentationLegs = slot 4)
                for (int tier = 1; tier <= 3; tier++)
                {
                    int tierIndex = tier - 1;
                    float sprintMult = GetConfigValue(config.LegSprintMultipliers, tierIndex, 1f);

                    var modifiers = new List<ModifierData5L>();
                    // Sprint speed multiplier (Multiply)
                    modifiers.Add(new ModifierData5L
                    {
                        m_Type = (ModifierType)2, // Multiply
                        m_Ammount = sprintMult,
                        m_AmountModifier = ModifierType.NONE,
                        m_TimeOut = 0f
                    });

                    RegisterImplantItem(itemManager, nextID++,
                        "Cybernetic Legs+ Mk" + tier,
                        ItemSlotTypes.AugmentationLegs,
                        tier, 500f * tier, 300f * tier,
                        0.1f + (tier - 1) * 0.25f,
                        modifiers.ToArray(), new List<int>());
                }

                isRegistered = true;
                Debug.Log("SyndicateWarsMod: Registered " + implantCount + " enhanced implants");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Implant registration failed: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Applies HP and energy regeneration from body implants (Heart combo effect).
        /// Called every frame from the main Update loop.
        /// </summary>
        public void ApplyBodyImplantEffects(float deltaTime)
        {
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null || agent.m_Health == null)
                        continue;

                    int bodyTier = GetEquippedImplantTier(agent, ItemSlotTypes.AugmentationBody);
                    if (bodyTier <= 0)
                        continue;

                    int tierIndex = bodyTier - 1;

                    // HP Regen (Heart effect)
                    float hpRegen = GetConfigValue(config.BodyHPRegenPerSecond, tierIndex, 0f);
                    if (hpRegen > 0f)
                    {
                        float currentHP = agent.m_Health.HealthValue;
                        float maxHP = agent.m_Health.GetMaxHealth();
                        if (currentHP < maxHP && currentHP > 0f)
                        {
                            float newHP = Mathf.Min(currentHP + hpRegen * deltaTime, maxHP);
                            agent.SetHealthValue(newHP);
                        }
                    }

                    // Energy Regen (Heart effect)
                    float energyRegen = GetConfigValue(config.BodyEnergyRegenPerSecond, tierIndex, 0f);
                    if (energyRegen > 0f && agent.m_Energy != null)
                    {
                        float currentEnergy = agent.m_Energy.EnergyValue;
                        float maxEnergy = agent.m_Energy.GetMaxEnergy();
                        if (currentEnergy < maxEnergy)
                        {
                            agent.m_Energy.AddEnergy(energyRegen * deltaTime);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Body implant effects error: " + e.Message);
            }
        }

        /// <summary>
        /// Gets the equipped implant tier for a specific slot type on an agent.
        /// Returns 0 if no enhanced implant is equipped in that slot.
        /// </summary>
        private int GetEquippedImplantTier(AgentAI agent, ItemSlotTypes slotType)
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
                    int baseID = config.ImplantBaseItemID;

                    // Determine the ID range for this slot type
                    int slotOffset = 0;
                    switch (slotType)
                    {
                        case ItemSlotTypes.AugmentationHead: slotOffset = 0; break;  // IDs 0-2
                        case ItemSlotTypes.AugmentationBody: slotOffset = 3; break;  // IDs 3-5
                        case ItemSlotTypes.AugmentationArms: slotOffset = 6; break;  // IDs 6-8
                        case ItemSlotTypes.AugmentationLegs: slotOffset = 9; break;  // IDs 9-11
                        default: return 0;
                    }

                    // Check if any of the 3 tiers for this slot are equipped
                    for (int tier = 1; tier <= 3; tier++)
                    {
                        int itemID = baseID + slotOffset + (tier - 1);
                        if (IsItemInEquipment(equipped, itemID))
                            return tier;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Error checking equipped implant: " + e.Message);
            }
            return 0;
        }

        /// <summary>
        /// Checks if an item ID is present in the equipment data.
        /// </summary>
        private bool IsItemInEquipment(object equipped, int itemID)
        {
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
            return false;
        }

        /// <summary>
        /// Registers a single implant item with the ItemManager.
        /// </summary>
        private void RegisterImplantItem(ItemManager itemManager, int id, string name,
            ItemSlotTypes slotType, int tier,
            float cost, float researchCost, float progression,
            ModifierData5L[] modifiers, List<int> abilityIDs)
        {
            ItemManager.ItemData itemData = new ItemManager.ItemData();
            itemData.m_ID = id;
            itemData.m_FriendlyName = name;
            itemData.m_Slot = slotType;
            itemData.m_GearSubCategory = ItemSubCategories.Standard;
            itemData.m_WeaponType = WeaponType.None;
            itemData.m_Cost = cost;
            itemData.m_ResearchCost = researchCost;
            itemData.m_Progression = progression;
            itemData.m_AvailableToPlayer = true;
            itemData.m_PlayerCanResearchFromStart = true;
            itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
            itemData.m_PrereqID = tier > 1 ? id - 1 : 0;
            itemData.m_AbilityIDs = new List<int>(abilityIDs);
            itemData.m_AbilityMasks = new List<int>();
            itemData.m_StealthVsCombat = 0f;
            itemData.m_Modifiers = modifiers;

            RegisterItem(itemManager, itemData);
            implantCount++;

            Debug.Log("SyndicateWarsMod: Registered implant: " + name + " (ID: " + id + ")");
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
                else
                {
                    Debug.LogWarning("SyndicateWarsMod: Could not find item data field in ItemManager");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: RegisterItem failed for ID " +
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
