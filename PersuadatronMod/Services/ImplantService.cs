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
                    if (agent == null || agent.GetItems() == null)
                        continue;

                    // Check if agent has any of our brain implant items equipped
                    foreach (var implant in implantDefinitions)
                    {
                        if (implant.SlotType != 1) // 1 = Head
                            continue;

                        if (agent.GetItems().HasEquipped(implant.ItemID))
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
                        agent.SetHealthValue(newHealth);
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
                    // CRITICAL: Initialize m_ResearchDataPoints to prevent NullReferenceException on save.
                    // The game's SaveItemData constructor calls .ToArray() on this field.
                    itemData.m_ResearchDataPoints = new List<ResearchDataPoint>();
                    itemData.m_ID = implant.ItemID;
                    itemData.m_FriendlyName = implant.Name;
                    itemData.m_Slot = GetSlotType(implant.SlotType);
                    itemData.m_GearSubCategory = ItemSubCategories.Standard;
                    itemData.m_WeaponType = WeaponType.None;
                    itemData.m_Cost = implant.Cost;
                    itemData.m_ResearchCost = implant.ResearchCost;
                    itemData.m_Progression = implant.Progression;
                    itemData.m_AvailableToPlayer = true;
                    itemData.m_PlayerCanResearchFromStart = true;
                    itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
                    itemData.m_PlayerHasPrototype = true;
                    itemData.m_PlayerHasBlueprints = true;
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
                            m_AmountModifier = ModifierType.NONE,
                            m_TimeOut = mod.TimeOut
                        });
                    }
                    itemData.m_Modifiers = modifiers.ToArray();

                    // Register with ItemManager using the correct field
                    RegisterItem(itemManager, itemData);

                    // Register localization
                    RegisterItemLocalization(implant.ItemID, implant.Name,
                        "Tier " + implant.Tier + " augmentation implant.");

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
        /// Registers a single item with the ItemManager using the public m_ItemDefinitions list.
        /// </summary>
        private void RegisterItem(ItemManager itemManager, ItemManager.ItemData itemData)
        {
            try
            {
                // Check for duplicates before adding
                bool exists = false;
                foreach (var existing in itemManager.m_ItemDefinitions)
                {
                    if (existing.m_ID == itemData.m_ID)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    itemManager.m_ItemDefinitions.Add(itemData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: RegisterItem failed for ID " +
                    itemData.m_ID + ": " + e.Message);
            }
        }

        /// <summary>
        /// Registers localization entries for an item so it displays properly in the UI.
        /// </summary>
        private void RegisterItemLocalization(int itemID, string name, string description)
        {
            try
            {
                var textManager = TextManager.Get();
                if (textManager == null)
                    return;

                var langLookupField = typeof(TextManager).GetField("m_FastLanguageLookup",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (langLookupField == null)
                    return;

                var langLookup = langLookupField.GetValue(textManager) as Dictionary<string, TextManager.LocElement>;
                if (langLookup == null)
                    return;

                // Register item name
                string nameKey = "ITEM_" + itemID + "_NAME";
                if (!langLookup.ContainsKey(nameKey))
                {
                    var nameElement = new TextManager.LocElement();
                    nameElement.m_token = nameKey;
                    nameElement.m_Translations = new string[8];
                    nameElement.m_Translations[2] = name;
                    langLookup[nameKey] = nameElement;
                }

                // Register item description
                if (!string.IsNullOrEmpty(description))
                {
                    string descKey = "ITEM_" + itemID + "_DESCRIPTION";
                    if (!langLookup.ContainsKey(descKey))
                    {
                        var descElement = new TextManager.LocElement();
                        descElement.m_token = descKey;
                        descElement.m_Translations = new string[8];
                        descElement.m_Translations[2] = description;
                        langLookup[descKey] = descElement;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: RegisterItemLocalization failed for ID " + itemID + ": " + e.Message);
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
        /// Gets the tier of the equipped implant for a specific slot type on an agent.
        /// Returns 0 if no implant of that type is equipped.
        /// </summary>
        private int GetEquippedImplantTier(AgentAI agent, int slotType)
        {
            if (agent == null || agent.GetItems() == null)
                return 0;

            foreach (var implant in implantDefinitions)
            {
                if (implant.SlotType == slotType && agent.GetItems().HasEquipped(implant.ItemID))
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
