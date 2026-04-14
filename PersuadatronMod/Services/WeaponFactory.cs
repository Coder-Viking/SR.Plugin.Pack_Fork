using System;
using System.Collections.Generic;
using System.Reflection;
using PersuadatronMod.Config;
using PersuadatronMod.Models;
using UnityEngine;

namespace PersuadatronMod.Services
{
    /// <summary>
    /// Creates and registers Syndicate Wars-style weapons with the game's WeaponManager and ItemManager.
    /// 
    /// Weapons:
    ///   - Uzi (SMG): High fire rate, low accuracy, medium damage, short range
    ///   - Minigun: Very high fire rate, high damage, very low accuracy, spin-up time
    ///   - Pumpgun (Shotgun): Multiple projectiles per shot, short range, high close damage
    ///   - Railgun (Sniper): Very high range, very high single-shot damage, long charge time
    ///   - Flamethrower: Area damage, beam weapon, short range
    ///   - Gauss Gun: EMP damage, medium range
    ///   - Laser: Beam weapon, high accuracy, medium damage
    /// </summary>
    public class WeaponFactory
    {
        private readonly PersuadatronConfig config;
        private List<WeaponDefinition> weaponDefinitions;
        private bool isRegistered;

        public WeaponFactory(PersuadatronConfig config)
        {
            this.config = config;
            this.weaponDefinitions = new List<WeaponDefinition>();
            this.isRegistered = false;
        }

        /// <summary>
        /// Creates and registers all Syndicate Wars weapons.
        /// </summary>
        public void RegisterWeapons()
        {
            if (isRegistered)
                return;

            try
            {
                weaponDefinitions = CreateAllWeaponDefinitions();
                RegisterWeaponsWithGame();
                isRegistered = true;
                Debug.Log("PersuadatronMod: Registered " + weaponDefinitions.Count + " weapons");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to register weapons: " + e.Message);
            }
        }

        /// <summary>
        /// Gets all weapon definitions.
        /// </summary>
        public List<WeaponDefinition> GetWeaponDefinitions()
        {
            return weaponDefinitions;
        }

        /// <summary>
        /// Creates all Syndicate Wars weapon definitions.
        /// </summary>
        private List<WeaponDefinition> CreateAllWeaponDefinitions()
        {
            var defs = new List<WeaponDefinition>();
            int nextID = config.WeaponBaseItemID;

            // 1. Uzi (SMG) - High fire rate, low accuracy, medium damage
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Uzi",
                WeaponType = 1, // Pistol
                Range = 12f,
                ShootWhileChangeTarget = true,
                DefaultAmmo = 0,
                ItemSlot = 5, // WeaponPistol
                SubCategory = 1, // Pistol
                Cost = 400f,
                ResearchCost = 200f,
                Progression = 0.1f,
                StealthVsCombat = 0.3f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 8f,
                        DamageMax = 14f,
                        DamageRadius = 0f,
                        KnockbackAmount = 0.1f,
                        MaxAmmo = 60,
                        ReloadTime = 1.5f,
                        ReloadSpeed = 1.2f,
                        ChargeTime = 0f,
                        ChargeEveryShot = false,
                        ShieldDamage = 3f,
                        CritChance = 0.05f,
                        CritDamageMultiplier = 1.5f,
                        AccuracyDelta = 0.35f, // Low accuracy (high delta = more spread)
                        EmpDamage = 0f,
                        MaxBeamWidth = 0f,
                        ProjectilesPerShot = 1
                    }
                }
            });

            // 2. Minigun - Very high fire rate, high damage, very low accuracy, spin-up
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Minigun",
                WeaponType = 4, // HeavyWeapon
                Range = 18f,
                ShootWhileChangeTarget = true,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 4, // HeavyWeapon
                Cost = 2500f,
                ResearchCost = 1500f,
                Progression = 0.5f,
                StealthVsCombat = 0.9f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 12f,
                        DamageMax = 20f,
                        DamageRadius = 0.5f,
                        KnockbackAmount = 0.3f,
                        MaxAmmo = 200,
                        ReloadTime = 4f,
                        ReloadSpeed = 0.8f,
                        ChargeTime = 1.5f, // Spin-up time
                        ChargeEveryShot = false,
                        ShieldDamage = 8f,
                        CritChance = 0.03f,
                        CritDamageMultiplier = 1.3f,
                        AccuracyDelta = 0.45f, // Very low accuracy
                        EmpDamage = 0f,
                        MaxBeamWidth = 0f,
                        ProjectilesPerShot = 1
                    }
                }
            });

            // 3. Pumpgun (Shotgun) - Multiple projectiles, short range, high damage
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Pumpgun",
                WeaponType = 2, // AssaultRifle (closest match)
                Range = 8f,
                ShootWhileChangeTarget = false,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 2, // AssaultRifle
                Cost = 800f,
                ResearchCost = 400f,
                Progression = 0.2f,
                StealthVsCombat = 0.6f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 5f,
                        DamageMax = 10f,
                        DamageRadius = 1.5f, // Spread area
                        KnockbackAmount = 0.8f, // High knockback
                        MaxAmmo = 8,
                        ReloadTime = 2.5f, // Slow pump reload
                        ReloadSpeed = 0.7f,
                        ChargeTime = 0f,
                        ChargeEveryShot = false,
                        ShieldDamage = 6f,
                        CritChance = 0.08f,
                        CritDamageMultiplier = 1.8f,
                        AccuracyDelta = 0.25f,
                        EmpDamage = 0f,
                        MaxBeamWidth = 0f,
                        ProjectilesPerShot = 8 // Shotgun pellets
                    }
                }
            });

            // 4. Railgun (Sniper) - Very high range, very high damage, long charge
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Railgun",
                WeaponType = 3, // SniperRifle
                Range = 40f,
                ShootWhileChangeTarget = false,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 3, // SniperRifle
                Cost = 3000f,
                ResearchCost = 2000f,
                Progression = 0.6f,
                StealthVsCombat = -0.2f, // Somewhat stealthy
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 80f,
                        DamageMax = 120f,
                        DamageRadius = 0f,
                        KnockbackAmount = 1.5f,
                        MaxAmmo = 5,
                        ReloadTime = 3f,
                        ReloadSpeed = 0.6f,
                        ChargeTime = 2f, // Long charge per shot
                        ChargeEveryShot = true,
                        ShieldDamage = 25f,
                        CritChance = 0.25f,
                        CritDamageMultiplier = 2.5f,
                        AccuracyDelta = 0.02f, // Very high accuracy
                        EmpDamage = 5f,
                        MaxBeamWidth = 0f,
                        ProjectilesPerShot = 1
                    }
                }
            });

            // 5. Flamethrower - Area damage, beam, short range
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Flamethrower",
                WeaponType = 4, // HeavyWeapon
                Range = 10f,
                ShootWhileChangeTarget = true,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 4, // HeavyWeapon
                Cost = 1800f,
                ResearchCost = 1000f,
                Progression = 0.4f,
                StealthVsCombat = 0.8f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 6f,
                        DamageMax = 12f,
                        DamageRadius = 2f, // Wide area damage
                        KnockbackAmount = 0.1f,
                        MaxAmmo = 100,
                        ReloadTime = 3.5f,
                        ReloadSpeed = 0.9f,
                        ChargeTime = 0.3f,
                        ChargeEveryShot = false,
                        ShieldDamage = 2f,
                        CritChance = 0.02f,
                        CritDamageMultiplier = 1.2f,
                        AccuracyDelta = 0.15f,
                        EmpDamage = 0f,
                        MaxBeamWidth = 1.5f, // Beam weapon
                        ProjectilesPerShot = 1
                    }
                }
            });

            // 6. Gauss Gun - EMP damage, medium range
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Gauss Gun",
                WeaponType = 2, // AssaultRifle
                Range = 20f,
                ShootWhileChangeTarget = false,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 2, // AssaultRifle
                Cost = 2000f,
                ResearchCost = 1200f,
                Progression = 0.45f,
                StealthVsCombat = 0.4f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 15f,
                        DamageMax = 25f,
                        DamageRadius = 1f,
                        KnockbackAmount = 0.5f,
                        MaxAmmo = 20,
                        ReloadTime = 2.5f,
                        ReloadSpeed = 1f,
                        ChargeTime = 0.8f,
                        ChargeEveryShot = true,
                        ShieldDamage = 20f,
                        CritChance = 0.10f,
                        CritDamageMultiplier = 1.5f,
                        AccuracyDelta = 0.10f,
                        EmpDamage = 15f, // High EMP damage
                        MaxBeamWidth = 0f,
                        ProjectilesPerShot = 1
                    }
                }
            });

            // 7. Laser - Beam weapon, high accuracy, medium damage
            defs.Add(new WeaponDefinition
            {
                ItemID = nextID++,
                Name = "Laser Rifle",
                WeaponType = 2, // AssaultRifle
                Range = 25f,
                ShootWhileChangeTarget = true,
                DefaultAmmo = 0,
                ItemSlot = 6, // Weapon
                SubCategory = 2, // AssaultRifle
                Cost = 1500f,
                ResearchCost = 900f,
                Progression = 0.35f,
                StealthVsCombat = 0.2f,
                AmmoTypes = new List<WeaponAmmoDefinition>
                {
                    new WeaponAmmoDefinition
                    {
                        Type = 0,
                        DamageMin = 10f,
                        DamageMax = 18f,
                        DamageRadius = 0f,
                        KnockbackAmount = 0.05f,
                        MaxAmmo = 40,
                        ReloadTime = 2f,
                        ReloadSpeed = 1.1f,
                        ChargeTime = 0f,
                        ChargeEveryShot = false,
                        ShieldDamage = 12f,
                        CritChance = 0.12f,
                        CritDamageMultiplier = 1.8f,
                        AccuracyDelta = 0.05f, // High accuracy
                        EmpDamage = 3f,
                        MaxBeamWidth = 0.3f, // Thin beam
                        ProjectilesPerShot = 1
                    }
                }
            });

            return defs;
        }

        /// <summary>
        /// Registers all weapons with the game's WeaponManager and ItemManager.
        /// </summary>
        private void RegisterWeaponsWithGame()
        {
            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                {
                    Debug.LogWarning("PersuadatronMod: ItemManager not available for weapon registration");
                    isRegistered = false;
                    return;
                }

                foreach (var weapon in weaponDefinitions)
                {
                    // Create ItemData for the weapon
                    ItemManager.ItemData itemData = new ItemManager.ItemData();
                    // CRITICAL: Initialize m_ResearchDataPoints to prevent NullReferenceException on save.
                    // The game's SaveItemData constructor calls .ToArray() on this field.
                    itemData.m_ResearchDataPoints = new List<ResearchDataPoint>();
                    itemData.m_ID = weapon.ItemID;
                    itemData.m_FriendlyName = weapon.Name;
                    itemData.m_Slot = GetWeaponSlotType(weapon.ItemSlot);
                    itemData.m_GearSubCategory = (ItemSubCategories)weapon.SubCategory;
                    itemData.m_WeaponType = (WeaponType)weapon.WeaponType;
                    itemData.m_Cost = weapon.Cost;
                    itemData.m_ResearchCost = weapon.ResearchCost;
                    itemData.m_Progression = weapon.Progression;
                    itemData.m_StealthVsCombat = weapon.StealthVsCombat;
                    itemData.m_AvailableToPlayer = true;
                    itemData.m_PlayerCanResearchFromStart = true;
                    itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
                    itemData.m_PlayerHasPrototype = true;
                    itemData.m_PlayerHasBlueprints = true;
                    itemData.m_PrereqID = 0;
                    itemData.m_AbilityIDs = new List<int>(weapon.AbilityIDs);
                    itemData.m_AbilityMasks = new List<int>();
                    itemData.m_Modifiers = new ModifierData5L[0];

                    RegisterItem(itemManager, itemData);

                    // Register localization
                    RegisterItemLocalization(weapon.ItemID, weapon.Name,
                        "Syndicate Wars weapon: " + weapon.Name);

                    Debug.Log("PersuadatronMod: Registered weapon: " + weapon.Name +
                        " (ID: " + weapon.ItemID + ", Type: " + weapon.WeaponType + ")");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Weapon registration failed: " + e.Message + "\n" + e.StackTrace);
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
                Debug.LogError("PersuadatronMod: RegisterItem failed for weapon ID " +
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
        /// Maps integer slot type to ItemSlotTypes enum for weapons.
        /// </summary>
        private ItemSlotTypes GetWeaponSlotType(int slotType)
        {
            switch (slotType)
            {
                case 5: return ItemSlotTypes.WeaponPistol;
                case 6: return ItemSlotTypes.Weapon;
                case 7: return ItemSlotTypes.WeaponAugmentation;
                default: return ItemSlotTypes.Weapon;
            }
        }
    }
}
