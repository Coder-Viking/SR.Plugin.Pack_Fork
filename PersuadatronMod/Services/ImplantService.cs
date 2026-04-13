using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using PersuadatronMod.Config;
using PersuadatronMod.Models;
using UnityEngine;

namespace PersuadatronMod.Services
{
    /// <summary>
    /// Manages the implant system: 4 augmentation groups (Head/Body/Arms/Legs)
    /// with 3 tiers each (Mk1/Mk2/Mk3).
    /// 
    /// Implant groups:
    ///   Legs  (AugmentationLegs):  Sprint speed + duration
    ///   Arms  (AugmentationArms):  Carry capacity + weapon accuracy
    ///   Body  (AugmentationBody):  HP + damage resist + HP regen
    ///   Head  (AugmentationHead):  Persuadatron level + cognitive abilities
    /// 
    /// This service creates SerializableItemData entries for each implant
    /// and registers them with the game's ItemManager.
    /// </summary>
    public class ImplantService
    {
        private readonly PersuadatronConfig config;
        private List<ImplantDefinition> implantDefinitions;
        private bool isRegistered;

        // Cached reflection for ItemManager access
        private FieldInfo itemListField;

        public ImplantService(PersuadatronConfig config)
        {
            this.config = config;
            this.implantDefinitions = new List<ImplantDefinition>();
            this.isRegistered = false;
        }

        /// <summary>
        /// Creates all implant definitions and registers them with the game.
        /// </summary>
        public void RegisterImplants()
        {
            if (isRegistered)
                return;

            try
            {
                implantDefinitions = CreateAllImplantDefinitions();
                RegisterWithItemManager();
                isRegistered = true;
                Debug.Log("PersuadatronMod: Registered " + implantDefinitions.Count + " implants");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to register implants: " + e.Message);
            }
        }

        /// <summary>
        /// Gets the Persuadatron level from the currently equipped brain implant.
        /// Returns 0 if no brain implant is equipped.
        /// </summary>
        public int GetEquippedBrainImplantLevel()
        {
            try
            {
                // Check each player agent for equipped brain implant
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null)
                        continue;

                    // Check if agent has any of our brain implant items equipped
                    foreach (var implant in implantDefinitions)
                    {
                        if (implant.SlotType != 1) // 1 = Head
                            continue;

                        if (IsItemEquipped(agent, implant.ItemID))
                        {
                            return implant.PersuadatronLevel;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Error checking brain implant: " + e.Message);
            }
            return 0;
        }

        /// <summary>
        /// Applies HP regeneration from body implants during Update.
        /// </summary>
        public void ApplyHPRegen(float deltaTime)
        {
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null || agent.m_Health == null)
                        continue;

                    int bodyTier = GetEquippedImplantTier(agent, 2); // 2 = Body
                    if (bodyTier <= 0)
                        continue;

                    // Get regen rate for this tier
                    int tierIndex = bodyTier - 1;
                    if (tierIndex >= config.BodyHPRegenPerSecond.Count)
                        continue;

                    float regenPerSecond = config.BodyHPRegenPerSecond[tierIndex];
                    if (regenPerSecond <= 0f)
                        continue;

                    float currentHealth = agent.m_Health.HealthValue;
                    float maxHealth = agent.m_Health.GetMaxHealth();

                    if (currentHealth < maxHealth && currentHealth > 0f)
                    {
                        float newHealth = Mathf.Min(currentHealth + regenPerSecond * deltaTime, maxHealth);
                        agent.m_Health.SetHealthValue(newHealth);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: HP regen error: " + e.Message);
            }
        }

        /// <summary>
        /// Gets the implant definitions list.
        /// </summary>
        public List<ImplantDefinition> GetImplantDefinitions()
        {
            return implantDefinitions;
        }

        /// <summary>
        /// Creates all 12 implant definitions (4 groups × 3 tiers).
        /// </summary>
        private List<ImplantDefinition> CreateAllImplantDefinitions()
        {
            var defs = new List<ImplantDefinition>();
            int nextID = config.ImplantBaseItemID;

            // Leg Implants (AugmentationLegs = 4)
            for (int tier = 1; tier <= 3; tier++)
            {
                int tierIndex = tier - 1;
                float sprintMult = tierIndex < config.LegSprintMultipliers.Count
                    ? config.LegSprintMultipliers[tierIndex] : 1f;

                defs.Add(new ImplantDefinition
                {
                    ItemID = nextID++,
                    Name = "Cybernetic Legs Mk" + tier,
                    SlotType = 4, // AugmentationLegs
                    Tier = tier,
                    Cost = 500f * tier,
                    ResearchCost = 300f * tier,
                    Progression = 0.1f + (tier - 1) * 0.25f,
                    PersuadatronLevel = 0,
                    Modifiers = new List<ImplantModifier>
                    {
                        new ImplantModifier { Type = 2, Amount = sprintMult, TimeOut = 0f } // Multiply sprint speed
                    }
                });
            }

            // Arm Implants (AugmentationArms = 3)
            for (int tier = 1; tier <= 3; tier++)
            {
                int tierIndex = tier - 1;
                float accuracyBonus = tierIndex < config.ArmAccuracyBonuses.Count
                    ? config.ArmAccuracyBonuses[tierIndex] : 0f;
                float carryMult = tierIndex < config.ArmCarryMultipliers.Count
                    ? config.ArmCarryMultipliers[tierIndex] : 1f;

                defs.Add(new ImplantDefinition
                {
                    ItemID = nextID++,
                    Name = "Cybernetic Arms Mk" + tier,
                    SlotType = 3, // AugmentationArms
                    Tier = tier,
                    Cost = 600f * tier,
                    ResearchCost = 350f * tier,
                    Progression = 0.1f + (tier - 1) * 0.25f,
                    PersuadatronLevel = 0,
                    Modifiers = new List<ImplantModifier>
                    {
                        new ImplantModifier { Type = 1, Amount = accuracyBonus, TimeOut = 0f }, // Add accuracy
                        new ImplantModifier { Type = 2, Amount = carryMult, TimeOut = 0f }      // Multiply carry
                    }
                });
            }

            // Body Implants (AugmentationBody = 2)
            for (int tier = 1; tier <= 3; tier++)
            {
                int tierIndex = tier - 1;
                float hpMult = tierIndex < config.BodyHPMultipliers.Count
                    ? config.BodyHPMultipliers[tierIndex] : 1f;
                float resistBonus = tierIndex < config.BodyResistBonuses.Count
                    ? config.BodyResistBonuses[tierIndex] : 0f;

                defs.Add(new ImplantDefinition
                {
                    ItemID = nextID++,
                    Name = "Cybernetic Torso Mk" + tier,
                    SlotType = 2, // AugmentationBody
                    Tier = tier,
                    Cost = 700f * tier,
                    ResearchCost = 400f * tier,
                    Progression = 0.15f + (tier - 1) * 0.25f,
                    PersuadatronLevel = 0,
                    Modifiers = new List<ImplantModifier>
                    {
                        new ImplantModifier { Type = 2, Amount = hpMult, TimeOut = 0f },      // Multiply HP
                        new ImplantModifier { Type = 1, Amount = resistBonus, TimeOut = 0f }   // Add damage resist
                    }
                });
            }

            // Brain Implants (AugmentationHead = 1)
            for (int tier = 1; tier <= 3; tier++)
            {
                int tierIndex = tier - 1;
                int persuadatronLvl = tierIndex < config.BrainPersuadatronLevels.Count
                    ? config.BrainPersuadatronLevels[tierIndex] : 0;

                var abilities = new List<int>();
                // Mk2+: Hack boost
                if (tier >= 2)
                    abilities.Add((int)AbilityEnum.Hack_Target);
                // Mk3: World scan
                if (tier >= 3)
                    abilities.Add((int)AbilityEnum.World_Scan);

                defs.Add(new ImplantDefinition
                {
                    ItemID = nextID++,
                    Name = "Neural Cortex Mk" + tier,
                    SlotType = 1, // AugmentationHead
                    Tier = tier,
                    Cost = 800f * tier,
                    ResearchCost = 500f * tier,
                    Progression = 0.1f + (tier - 1) * 0.25f,
                    PersuadatronLevel = persuadatronLvl,
                    AbilityIDs = abilities,
                    Modifiers = new List<ImplantModifier>()
                });
            }

            return defs;
        }

        /// <summary>
        /// Registers all implant definitions as items in the game's ItemManager.
        /// </summary>
        private void RegisterWithItemManager()
        {
            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                {
                    Debug.LogWarning("PersuadatronMod: ItemManager not available yet");
                    isRegistered = false;
                    return;
                }

                // Map slot type ints to ItemSlotTypes enum
                foreach (var implant in implantDefinitions)
                {
                    ItemManager.ItemData itemData = new ItemManager.ItemData();
                    itemData.m_ID = implant.ItemID;
                    itemData.m_FriendlyName = implant.Name;
                    itemData.m_Slot = GetSlotType(implant.SlotType);
                    itemData.m_GearSubCategory = ItemSubCategories.Augmentation;
                    itemData.m_WeaponType = WeaponType.None;
                    itemData.m_Cost = implant.Cost;
                    itemData.m_ResearchCost = implant.ResearchCost;
                    itemData.m_Progression = implant.Progression;
                    itemData.m_AvailableToPlayer = true;
                    itemData.m_PlayerCanResearchFromStart = true;
                    itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
                    itemData.m_PrereqID = implant.Tier > 1
                        ? implant.ItemID - 1 // Previous tier is prerequisite
                        : 0;
                    itemData.m_AbilityIDs = new List<int>(implant.AbilityIDs);
                    itemData.m_AbilityMasks = new List<int>();
                    itemData.m_StealthVsCombat = 0f;

                    // Convert modifiers
                    var modifiers = new List<ModifierData5L>();
                    foreach (var mod in implant.Modifiers)
                    {
                        modifiers.Add(new ModifierData5L
                        {
                            m_Type = (ModifierType)mod.Type,
                            m_Ammount = mod.Amount,
                            m_AmountModifier = ModifierType.None,
                            m_TimeOut = mod.TimeOut
                        });
                    }
                    itemData.m_Modifiers = modifiers.ToArray();

                    // Register with ItemManager
                    RegisterItem(itemManager, itemData);

                    Debug.Log("PersuadatronMod: Registered implant: " + implant.Name +
                        " (ID: " + implant.ItemID + ", Slot: " + implant.SlotType + ")");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to register items: " + e.Message + "\n" + e.StackTrace);
                isRegistered = false;
            }
        }

        /// <summary>
        /// Registers a single item with the ItemManager using reflection to access internal lists.
        /// </summary>
        private void RegisterItem(ItemManager itemManager, ItemManager.ItemData itemData)
        {
            try
            {
                // Try to access the item list via reflection
                if (itemListField == null)
                {
                    itemListField = typeof(ItemManager).GetField("m_ItemData",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (itemListField == null)
                    {
                        // Try alternative field names
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
                        // Don't add duplicates
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
                    Debug.LogWarning("PersuadatronMod: Could not find item data field in ItemManager");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: RegisterItem failed for ID " +
                    itemData.m_ID + ": " + e.Message);
            }
        }

        /// <summary>
        /// Maps integer slot type to ItemSlotTypes enum.
        /// </summary>
        private ItemSlotTypes GetSlotType(int slotType)
        {
            switch (slotType)
            {
                case 1: return ItemSlotTypes.AugmentationHead;
                case 2: return ItemSlotTypes.AugmentationBody;
                case 3: return ItemSlotTypes.AugmentationArms;
                case 4: return ItemSlotTypes.AugmentationLegs;
                default: return ItemSlotTypes.Gear;
            }
        }

        /// <summary>
        /// Checks whether a specific item is equipped on an agent.
        /// </summary>
        private bool IsItemEquipped(AgentAI agent, int itemID)
        {
            try
            {
                // Access agent's equipped items via reflection
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
                            if (id == itemID)
                                return true;
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
                Debug.LogError("PersuadatronMod: Error checking equipped item: " + e.Message);
            }
            return false;
        }

        /// <summary>
        /// Gets the tier of the equipped implant for a specific slot type on an agent.
        /// Returns 0 if no implant of that type is equipped.
        /// </summary>
        private int GetEquippedImplantTier(AgentAI agent, int slotType)
        {
            foreach (var implant in implantDefinitions)
            {
                if (implant.SlotType == slotType && IsItemEquipped(agent, implant.ItemID))
                {
                    return implant.Tier;
                }
            }
            return 0;
        }

        // Enum reference for ability IDs
        private enum AbilityEnum
        {
            Hack_Target = 1317,
            World_Scan = 1331
        }
    }
}
