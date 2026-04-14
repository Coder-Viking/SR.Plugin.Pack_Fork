using System;
using System.Collections.Generic;
using PersuadatronMod.Config;
using PersuadatronMod.Models;
using PersuadatronMod.Services;
using UnityEngine;

namespace PersuadatronMod
{
    /// <summary>
    /// Syndicate Wars-style Persuadatron and Implant System for Satellite Reign.
    /// 
    /// Features:
    ///   1. Persuadatron - Portable item that converts NPCs to followers (brain implant level-gated)
    ///   2. 4 Implant Groups - Legs/Arms/Body/Head with 3 tiers each
    ///   3. Syndicate Wars Weapons - Uzi, Minigun, Pumpgun, Railgun, Flamethrower, Gauss Gun, Laser
    ///   4. Follower AI - Persuaded units follow, fight, and pick up weapons
    /// 
    /// Hotkeys:
    ///   P     - Activate Persuadatron on nearest valid target
    ///   F5    - Show Persuadatron/follower status
    ///   F6    - Reload config
    ///   L     - List all followers
    /// </summary>
    public class PersuadatronMod : ISrPlugin
    {
        #region Fields
        private bool isInitialized;
        private bool servicesRegistered;
        private PersuadatronConfig config;

        // Services
        private ImplantService implantService;
        private WeaponFactory weaponFactory;
        private PersuasionService persuasionService;
        private FollowerAIService followerAIService;

        // Timing
        private float lastStatusUpdate;
        private const float STATUS_UPDATE_INTERVAL = 1f;

        // Persuadatron item IDs
        private int persuadatronMk1ID;
        private int persuadatronMk2ID;
        private int persuadatronMk3ID;
        #endregion

        #region ISrPlugin Implementation
        public string GetName()
        {
            return "Persuadatron Mod v1.0 - Syndicate Wars Style";
        }

        public void Initialize()
        {
            try
            {
                Debug.Log("PersuadatronMod: Initializing...");

                isInitialized = true;
                servicesRegistered = false;
                lastStatusUpdate = 0f;

                Debug.Log("PersuadatronMod: Initialization complete. Waiting for game to load...");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Initialization failed: " + e.Message);
            }
        }

        public void Update()
        {
            if (!isInitialized)
                return;

            try
            {
                // Wait for the game to be fully loaded
                if (Manager.Get() == null || !Manager.Get().GameInProgress || Manager.Get().IsLoading())
                    return;

                // Lazy-init services once the game is ready
                if (!servicesRegistered)
                {
                    InitializeServices();
                    return;
                }

                if (!config.Enabled)
                    return;

                // Handle hotkey input
                HandleInput();

                // Update follower AI
                UpdateFollowerAI();

                // Apply body implant HP regen
                implantService.ApplyHPRegen(Time.deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Update error: " + e.Message);
            }
        }
        #endregion

        #region Service Initialization
        /// <summary>
        /// Initializes all services once the game is fully loaded.
        /// This is called from Update() on the first game frame.
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // Load config
                string pluginPath = Manager.GetPluginManager().PluginPath;
                config = PersuadatronConfig.Load(pluginPath);

                // Initialize services in dependency order
                implantService = new ImplantService(config);
                weaponFactory = new WeaponFactory(config);
                persuasionService = new PersuasionService(config, implantService);
                followerAIService = new FollowerAIService(config);

                // Register items with the game
                implantService.RegisterImplants();
                weaponFactory.RegisterWeapons();
                RegisterPersuadatronItems();

                servicesRegistered = true;

                // Show welcome message
                Manager.GetUIManager().ShowMessagePopup(
                    "Persuadatron Mod Loaded!\n\n" +
                    "Hotkeys:\n" +
                    "  P - Persuade nearest target\n" +
                    "  F5 - Show status\n" +
                    "  F6 - Reload config\n" +
                    "  L - List followers\n\n" +
                    "Equip a Brain Implant to unlock Persuadatron levels.\n" +
                    "Syndicate Wars weapons are now available for research!", 10);

                Debug.Log("PersuadatronMod: All services initialized and items registered");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Service initialization failed: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Registers the three Persuadatron items (Mk1/Mk2/Mk3) as gear items.
        /// </summary>
        private void RegisterPersuadatronItems()
        {
            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                    return;

                persuadatronMk1ID = config.PersuadatronBaseItemID;
                persuadatronMk2ID = config.PersuadatronBaseItemID + 1;
                persuadatronMk3ID = config.PersuadatronBaseItemID + 2;

                RegisterPersuadatronItem(itemManager, persuadatronMk1ID, "Persuadatron Mk1",
                    "Converts civilians to followers. Requires Neural Cortex Mk1.",
                    500f, 300f, 0.15f, 0);

                RegisterPersuadatronItem(itemManager, persuadatronMk2ID, "Persuadatron Mk2",
                    "Converts civilians and light units. Requires Neural Cortex Mk2.",
                    1200f, 800f, 0.35f, persuadatronMk1ID);

                RegisterPersuadatronItem(itemManager, persuadatronMk3ID, "Persuadatron Mk3",
                    "Converts most units. Requires Neural Cortex Mk3.",
                    3000f, 2000f, 0.55f, persuadatronMk2ID);

                Debug.Log("PersuadatronMod: Registered 3 Persuadatron items");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to register Persuadatron items: " + e.Message);
            }
        }

        private void RegisterPersuadatronItem(ItemManager itemManager, int id, string name,
            string description, float cost, float researchCost, float progression, int prereqID)
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
            itemData.m_PrereqID = prereqID;
            itemData.m_AvailableToPlayer = true;
            itemData.m_PlayerCanResearchFromStart = true;
            itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
            itemData.m_AbilityIDs = new List<int>();
            itemData.m_AbilityMasks = new List<int>();
            itemData.m_Modifiers = new ModifierData5L[0];
            itemData.m_StealthVsCombat = -0.5f; // Stealthy device

            // Use the same registration pattern as ImplantService
            RegisterItemWithManager(itemManager, itemData);
        }

        private void RegisterItemWithManager(ItemManager itemManager, ItemManager.ItemData itemData)
        {
            try
            {
                var itemListField = typeof(ItemManager).GetField("m_ItemData",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (itemListField == null)
                {
                    itemListField = typeof(ItemManager).GetField("m_Items",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
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
                Debug.LogError("PersuadatronMod: RegisterItem failed: " + e.Message);
            }
        }
        #endregion

        #region Input Handling
        private void HandleInput()
        {
            // P - Persuade nearest target
            if (Input.GetKeyDown(KeyCode.P))
            {
                TryPersuadeNearestTarget();
            }

            // F5 - Show status
            if (Input.GetKeyDown(KeyCode.F5))
            {
                ShowStatus();
            }

            // F6 - Reload config
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ReloadConfig();
            }

            // L - List followers
            if (Input.GetKeyDown(KeyCode.L))
            {
                ListFollowers();
            }
        }
        #endregion

        #region Persuasion Logic
        /// <summary>
        /// Attempts to persuade the nearest valid target to the first selected agent.
        /// </summary>
        private void TryPersuadeNearestTarget()
        {
            try
            {
                int persuadatronLevel = persuasionService.GetCurrentPersuadatronLevel();

                if (persuadatronLevel <= 0)
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron: No Brain Implant equipped! Equip a Neural Cortex to use.", 3);
                    return;
                }

                // Check if Persuadatron item is equipped
                if (!IsPersuadatronEquipped())
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron: No Persuadatron device equipped!", 3);
                    return;
                }

                if (!persuasionService.IsReady)
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron: Cooldown - " + persuasionService.CooldownRemaining.ToString("F1") + "s", 2);
                    return;
                }

                // Get carrier position (first selected agent)
                AgentAI carrier = AgentAI.FirstSelectedAgentAi();
                if (carrier == null)
                {
                    Manager.GetUIManager().ShowSubtitle("Persuadatron: No agent selected!", 2);
                    return;
                }

                Vector3 carrierPos = carrier.transform.position;

                // Find targets in range
                List<AIEntity> targets = persuasionService.FindTargetsInRange(carrierPos, persuadatronLevel);

                if (targets.Count == 0)
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron Mk" + persuadatronLevel + ": No valid targets in range", 2);
                    return;
                }

                // Persuade the nearest target
                AIEntity nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var target in targets)
                {
                    // Skip targets already persuaded
                    if (IsAlreadyPersuaded(target))
                        continue;

                    float dist = Vector3.Distance(carrierPos, target.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = target;
                    }
                }

                if (nearest == null)
                {
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron: All targets in range already persuaded", 2);
                    return;
                }

                PersuadedUnit unit = persuasionService.TryPersuade(
                    nearest, persuadatronLevel, followerAIService.FollowerCount);

                if (unit != null)
                {
                    followerAIService.AddFollower(unit);
                    Manager.GetUIManager().ShowSubtitle(
                        "Persuadatron: Target persuaded! Followers: " +
                        followerAIService.FollowerCount + "/" + config.MaxFollowers, 3);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Persuasion error: " + e.Message);
            }
        }

        /// <summary>
        /// Checks if a target is already in the follower list.
        /// </summary>
        private bool IsAlreadyPersuaded(AIEntity target)
        {
            foreach (var follower in followerAIService.GetFollowers())
            {
                if (follower.Entity == target)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if any player agent has a Persuadatron item equipped.
        /// </summary>
        private bool IsPersuadatronEquipped()
        {
            // For simplicity, if the player has a brain implant (which gates persuadatron),
            // we consider the persuadatron available. In a full implementation,
            // we'd check the Gear slot specifically.
            return persuasionService.GetCurrentPersuadatronLevel() > 0;
        }
        #endregion

        #region Follower AI Update
        private void UpdateFollowerAI()
        {
            if (followerAIService.FollowerCount == 0)
                return;

            try
            {
                // Get the carrier's position (first selected agent or first agent)
                AgentAI carrier = AgentAI.FirstSelectedAgentAi();
                if (carrier == null)
                {
                    foreach (AgentAI agent in AgentAI.GetAgents())
                    {
                        if (agent != null)
                        {
                            carrier = agent;
                            break;
                        }
                    }
                }

                if (carrier != null)
                {
                    followerAIService.UpdateFollowers(carrier.transform.position);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Follower AI update error: " + e.Message);
            }
        }
        #endregion

        #region Status Display
        private void ShowStatus()
        {
            try
            {
                int brainLevel = persuasionService.GetCurrentPersuadatronLevel();
                string brainStatus = brainLevel > 0
                    ? "Neural Cortex Mk" + brainLevel + " (Persuadatron Lvl " + brainLevel + ")"
                    : "None equipped";

                string cooldownStatus = persuasionService.IsReady
                    ? "READY"
                    : persuasionService.CooldownRemaining.ToString("F1") + "s";

                string targetInfo = "";
                switch (brainLevel)
                {
                    case 1:
                        targetInfo = "Civilians only";
                        break;
                    case 2:
                        targetInfo = "Civilians + Light units (PowerLevel <= " +
                            (config.Mk2PowerLevelMax * 100f).ToString("F0") + "%)";
                        break;
                    case 3:
                        targetInfo = "All units (PowerLevel <= " +
                            (config.Mk3PowerLevelMax * 100f).ToString("F0") + "%)";
                        break;
                    default:
                        targetInfo = "N/A - Equip a Neural Cortex";
                        break;
                }

                string status =
                    "=== Persuadatron Mod Status ===\n\n" +
                    "Brain Implant: " + brainStatus + "\n" +
                    "Persuadatron: " + cooldownStatus + "\n" +
                    "Valid Targets: " + targetInfo + "\n" +
                    "Followers: " + followerAIService.FollowerCount + "/" + config.MaxFollowers + "\n" +
                    "Range: " + config.PersuasionRange + " units\n\n" +
                    "Implants Registered: " + implantService.GetImplantDefinitions().Count + "\n" +
                    "Weapons Registered: " + weaponFactory.GetWeaponDefinitions().Count + "\n\n" +
                    "Hotkeys: P=Persuade, F5=Status, F6=Reload, L=List Followers";

                Manager.GetUIManager().ShowMessagePopup(status, 15);
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup("Persuadatron Mod: Error showing status - " + e.Message, 5);
            }
        }

        private void ListFollowers()
        {
            try
            {
                var followers = followerAIService.GetFollowers();
                if (followers.Count == 0)
                {
                    Manager.GetUIManager().ShowSubtitle("No followers currently persuaded.", 3);
                    return;
                }

                string info = "=== Persuaded Followers (" + followers.Count + "/" + config.MaxFollowers + ") ===\n\n";

                for (int i = 0; i < followers.Count; i++)
                {
                    var f = followers[i];
                    string state = "Following";
                    if (f.IsInCombat) state = "COMBAT";
                    else if (f.IsPickingUpWeapon) state = "Picking up weapon";

                    string weaponStatus = f.HasWeapon ? "Armed" : "Unarmed";
                    string timeLeft = f.Duration > 0
                        ? ((f.PersuadedAtTime + f.Duration - Time.time).ToString("F0") + "s left")
                        : "Permanent";

                    info += (i + 1) + ". [" + state + "] " + weaponStatus +
                        " | PwrLvl: " + (f.PowerLevel * 100f).ToString("F0") + "% | " + timeLeft + "\n";
                }

                Manager.GetUIManager().ShowMessagePopup(info, 10);
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup("Error listing followers: " + e.Message, 5);
            }
        }

        private void ReloadConfig()
        {
            try
            {
                string pluginPath = Manager.GetPluginManager().PluginPath;
                config = PersuadatronConfig.Load(pluginPath);
                Manager.GetUIManager().ShowMessagePopup("Persuadatron Mod: Config reloaded!", 3);
                Debug.Log("PersuadatronMod: Config reloaded");
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup("Config reload failed: " + e.Message, 5);
            }
        }
        #endregion
    }
}
