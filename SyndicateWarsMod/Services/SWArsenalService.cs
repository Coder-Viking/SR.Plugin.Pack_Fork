using System;
using System.Collections.Generic;
using System.Reflection;
using SyndicateWarsMod.Config;
using UnityEngine;

namespace SyndicateWarsMod.Services
{
    /// <summary>
    /// Registers new Syndicate Wars-style weapons with the game's ItemManager.
    /// 
    /// New Weapons (ID range 90300+):
    ///   - Long Range Rifle: Extreme range sniper, charge-per-shot, high single-shot damage
    ///   - Razor Wire: Short-range area denial weapon with high damage radius
    ///   - Pulse Laser: Precise beam weapon, high shield damage, armor penetration
    ///   - Graviton Gun: Heavy weapon with massive knockback and AOE, EMP component
    ///   - Satellite Rain: Gear item that marks a position for orbital strike
    ///   - Nuclear Grenade: Massive radius grenade with extreme damage
    /// </summary>
    public class SWArsenalService
    {
        private readonly SyndicateWarsConfig config;
        private bool isRegistered;

        // WeaponType integer values matching the game's enum
        // (named members don't exist in Assembly-CSharp.dll, so we use int casts)
        private const int WeaponTypeAssaultRifle = 2;
        private const int WeaponTypeSniperRifle = 3;
        private const int WeaponTypeHeavyWeapon = 4;
        private const int WeaponTypeGrenade = 5;

        // Cached reflection for ItemManager access
        private FieldInfo itemListField;

        public SWArsenalService(SyndicateWarsConfig config)
        {
            this.config = config;
            this.isRegistered = false;
        }

        /// <summary>
        /// Gets the number of registered weapons.
        /// </summary>
        public int WeaponCount { get { return 6; } }

        /// <summary>
        /// Registers all new weapons with the game's ItemManager.
        /// </summary>
        public void RegisterWeapons()
        {
            if (isRegistered)
                return;

            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                {
                    Debug.LogWarning("SyndicateWarsMod: ItemManager not available for weapon registration");
                    return;
                }

                int nextID = config.WeaponBaseItemID;

                // 1. Long Range Rifle (Sniper)
                RegisterWeaponItem(itemManager, nextID++, "Long Range Rifle",
                    ItemSlotTypes.Weapon, (WeaponType)WeaponTypeSniperRifle,
                    2500f, 1800f, 0.4f, -0.3f);

                // 2. Razor Wire (Area denial)
                RegisterWeaponItem(itemManager, nextID++, "Razor Wire",
                    ItemSlotTypes.Weapon, (WeaponType)WeaponTypeHeavyWeapon,
                    600f, 300f, 0.15f, 0.1f);

                // 3. Pulse Laser (Precise beam)
                RegisterWeaponItem(itemManager, nextID++, "Pulse Laser",
                    ItemSlotTypes.Weapon, (WeaponType)WeaponTypeAssaultRifle,
                    1800f, 1200f, 0.35f, 0.2f);

                // 4. Graviton Gun (Heavy AOE + knockback)
                RegisterWeaponItem(itemManager, nextID++, "Graviton Gun",
                    ItemSlotTypes.Weapon, (WeaponType)WeaponTypeHeavyWeapon,
                    3500f, 2500f, 0.55f, 0.7f);

                // 5. Satellite Rain (Orbital strike marker - Gear item)
                RegisterGearItem(itemManager, nextID++, "Satellite Rain Beacon",
                    5000f, 3500f, 0.7f);

                // 6. Nuclear Grenade
                RegisterWeaponItem(itemManager, nextID++, "Nuclear Grenade",
                    ItemSlotTypes.Weapon, (WeaponType)WeaponTypeGrenade,
                    4000f, 3000f, 0.65f, 0.9f);

                isRegistered = true;
                Debug.Log("SyndicateWarsMod: Registered 6 new weapons (IDs " +
                    config.WeaponBaseItemID + "-" + (nextID - 1) + ")");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Weapon registration failed: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Registers a weapon item with the ItemManager.
        /// </summary>
        private void RegisterWeaponItem(ItemManager itemManager, int id, string name,
            ItemSlotTypes slot, WeaponType weaponType,
            float cost, float researchCost, float progression, float stealthVsCombat)
        {
            ItemManager.ItemData itemData = new ItemManager.ItemData();
            itemData.m_ID = id;
            itemData.m_FriendlyName = name;
            itemData.m_Slot = slot;
            itemData.m_GearSubCategory = GetSubCategoryForWeaponType(weaponType);
            itemData.m_WeaponType = weaponType;
            itemData.m_Cost = cost;
            itemData.m_ResearchCost = researchCost;
            itemData.m_Progression = progression;
            itemData.m_StealthVsCombat = stealthVsCombat;
            itemData.m_AvailableToPlayer = true;
            itemData.m_PlayerCanResearchFromStart = true;
            itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
            itemData.m_PrereqID = 0;
            itemData.m_AbilityIDs = new List<int>();
            itemData.m_AbilityMasks = new List<int>();
            itemData.m_Modifiers = new ModifierData5L[0];

            RegisterItem(itemManager, itemData);
            Debug.Log("SyndicateWarsMod: Registered weapon: " + name + " (ID: " + id + ")");
        }

        /// <summary>
        /// Registers a gear item (like Satellite Rain beacon).
        /// </summary>
        private void RegisterGearItem(ItemManager itemManager, int id, string name,
            float cost, float researchCost, float progression)
        {
            ItemManager.ItemData itemData = new ItemManager.ItemData();
            itemData.m_ID = id;
            itemData.m_FriendlyName = name;
            itemData.m_Slot = ItemSlotTypes.Gear;
            itemData.m_GearSubCategory = ItemSubCategories.Standard;
            itemData.m_WeaponType = WeaponType.None;
            itemData.m_Cost = cost;
            itemData.m_ResearchCost = researchCost;
            itemData.m_Progression = progression;
            itemData.m_StealthVsCombat = -0.5f;
            itemData.m_AvailableToPlayer = true;
            itemData.m_PlayerCanResearchFromStart = true;
            itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
            itemData.m_PrereqID = 0;
            itemData.m_AbilityIDs = new List<int>();
            itemData.m_AbilityMasks = new List<int>();
            itemData.m_Modifiers = new ModifierData5L[0];

            RegisterItem(itemManager, itemData);
            Debug.Log("SyndicateWarsMod: Registered gear item: " + name + " (ID: " + id + ")");
        }

        /// <summary>
        /// Maps WeaponType to the corresponding ItemSubCategories value.
        /// </summary>
        private ItemSubCategories GetSubCategoryForWeaponType(WeaponType wt)
        {
            return ItemSubCategories.Standard;
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
    }
}
