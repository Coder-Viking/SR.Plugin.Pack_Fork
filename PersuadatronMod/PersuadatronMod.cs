using System;
using System.Collections.Generic;
using System.Reflection;
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
    ///   1. Persuadatron - Weapon that creates a persuasion aura while equipped
    ///   2. 4 Implant Groups - Legs/Arms/Body/Head with 3 tiers each
    ///   3. Follower AI - Persuaded units follow, fight, and pick up weapons
    ///   4. Enemy weapon drops - Defeated enemies drop level-appropriate weapons
    ///   5. Follower capacity scales with Neural Cortex implant tier
    /// 
    /// Hotkeys:
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
        private PersuasionService persuasionService;
        private FollowerAIService followerAIService;
        private EnemyDeathWatcherService enemyDeathWatcher;

        // Timing
        private float lastStatusUpdate;
        private const float STATUS_UPDATE_INTERVAL = 1f;

        // Single Persuadatron item ID (weapon)
        private int persuadatronID;
        #endregion

        #region ISrPlugin Implementation
        public string GetName()
        {
            return "Persuadatron Mod v2.0 - Syndicate Wars Style";
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

                // Update persuasion aura (auto-persuade while Persuadatron weapon is equipped)
                UpdatePersuasionAura();

                // Update follower AI
                UpdateFollowerAI();

                // Scan for dead enemies to drop weapons
                UpdateEnemyDeathWatcher();

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
                persuasionService = new PersuasionService(config, implantService);
                followerAIService = new FollowerAIService(config);
                enemyDeathWatcher = new EnemyDeathWatcherService(config);

                // Register items with the game
                implantService.RegisterImplants();
                RegisterPersuadatronItem();

                servicesRegistered = true;

                // Show welcome message
                Manager.GetUIManager().ShowMessagePopup(
                    "Persuadatron Mod v2.0 Loaded!\n\n" +
                    "Equip the Persuadatron weapon to auto-persuade nearby targets.\n" +
                    "Equip a Neural Cortex implant to unlock & strengthen persuasion.\n\n" +
                    "Hotkeys:\n" +
                    "  F5 - Show status\n" +
                    "  F6 - Reload config\n" +
                    "  L - List followers", 10);

                Debug.Log("PersuadatronMod: All services initialized and items registered");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Service initialization failed: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Registers a single Persuadatron item as a weapon (pistol slot).
        /// When equipped as the current weapon, its persuasion aura is active.
        /// </summary>
        private void RegisterPersuadatronItem()
        {
            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                    return;

                persuadatronID = config.PersuadatronBaseItemID;

                ItemManager.ItemData itemData = new ItemManager.ItemData();
                // CRITICAL: Initialize m_ResearchDataPoints to prevent NullReferenceException on save.
                itemData.m_ResearchDataPoints = new List<ResearchDataPoint>();
                itemData.m_ID = persuadatronID;
                itemData.m_FriendlyName = "Persuadatron";
                itemData.m_Slot = ItemSlotTypes.WeaponPistol;
                itemData.m_GearSubCategory = ItemSubCategories.Standard;
                itemData.m_WeaponType = WeaponType.Pistol;
                itemData.m_Cost = 800f;
                itemData.m_ResearchCost = 500f;
                itemData.m_Progression = 0.15f;
                itemData.m_PrereqID = 0;
                itemData.m_AvailableToPlayer = true;
                itemData.m_PlayerCanResearchFromStart = true;
                itemData.m_AvailableFor_ALPHA_BETA_EARLYACCESS = true;
                // Available from game start — no research needed (only Mk1 exists)
                itemData.m_PlayerHasPrototype = true;
                itemData.m_PlayerHasBlueprints = true;
                itemData.m_AbilityIDs = new List<int>();
                itemData.m_AbilityMasks = new List<int>();
                itemData.m_Modifiers = new ModifierData5L[0];
                itemData.m_StealthVsCombat = -0.5f; // Stealthy device

                RegisterItemWithManager(itemManager, itemData);
                RegisterItemLocalization(persuadatronID, "Persuadatron",
                    "Syndicate Wars persuasion device. Equip as weapon to create a persuasion aura " +
                    "that automatically converts nearby NPCs into followers. " +
                    "Effectiveness depends on equipped Neural Cortex implant level.");

                Debug.Log("PersuadatronMod: Registered Persuadatron weapon (ID: " + persuadatronID + ")");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Failed to register Persuadatron: " + e.Message);
            }
        }

        private void RegisterItemWithManager(ItemManager itemManager, ItemManager.ItemData itemData)
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
                Debug.LogError("PersuadatronMod: RegisterItem failed: " + e.Message);
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
                    nameElement.m_Translations[2] = name; // English
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
                        descElement.m_Translations[2] = description; // English
                        langLookup[descKey] = descElement;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: RegisterItemLocalization failed for ID " + itemID + ": " + e.Message);
            }
        }
        #endregion

        #region Input Handling
        private void HandleInput()
        {
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

        #region Persuasion Aura
        /// <summary>
        /// Automatic persuasion aura: while the Persuadatron weapon is equipped,
        /// nearby valid targets are automatically persuaded at regular intervals.
        /// Persuasion level and follower cap depend on the equipped Neural Cortex implant.
        /// </summary>
        private void UpdatePersuasionAura()
        {
            try
            {
                int persuadatronLevel = persuasionService.GetCurrentPersuadatronLevel();
                if (persuadatronLevel <= 0)
                    return;

                // Check if any agent has the Persuadatron weapon equipped
                AgentAI carrier = GetPersuadatronCarrier();
                if (carrier == null)
                    return;

                // Check cooldown
                if (!persuasionService.IsReady)
                    return;

                // Get max followers for current implant level
                int maxFollowers = GetMaxFollowersForLevel(persuadatronLevel);
                if (followerAIService.FollowerCount >= maxFollowers)
                    return;

                Vector3 carrierPos = carrier.transform.position;

                // Find targets in aura range
                List<AIEntity> targets = persuasionService.FindTargetsInRange(
                    carrierPos, persuadatronLevel, config.PersuasionAuraRange);

                // Auto-persuade the nearest valid target
                AIEntity nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var target in targets)
                {
                    if (IsAlreadyPersuaded(target))
                        continue;

                    float dist = Vector3.Distance(carrierPos, target.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = target;
                    }
                }

                if (nearest != null)
                {
                    PersuadedUnit unit = persuasionService.TryPersuade(
                        nearest, persuadatronLevel, followerAIService.FollowerCount, maxFollowers);

                    if (unit != null)
                    {
                        followerAIService.AddFollower(unit);
                        Manager.GetUIManager().ShowSubtitle(
                            "Persuadatron: Target persuaded! Followers: " +
                            followerAIService.FollowerCount + "/" + maxFollowers, 3);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Persuasion aura error: " + e.Message);
            }
        }

        /// <summary>
        /// Finds the player agent that currently has the Persuadatron weapon equipped
        /// in a weapon slot (WeaponPistol).
        /// </summary>
        private AgentAI GetPersuadatronCarrier()
        {
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null)
                        continue;

                    var items = agent.GetItems();
                    if (items == null)
                        continue;

                    // Check if the Persuadatron is equipped in the pistol weapon slot
                    if (items.HasEquipped(persuadatronID))
                    {
                        return agent;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: GetPersuadatronCarrier error: " + e.Message);
            }
            return null;
        }

        /// <summary>
        /// Returns the maximum number of followers allowed for the given Neural Cortex level.
        /// </summary>
        private int GetMaxFollowersForLevel(int level)
        {
            if (level <= 0)
                return 0;

            int index = level - 1;
            if (index < config.MaxFollowersPerLevel.Count)
                return config.MaxFollowersPerLevel[index];

            // Fallback: return the highest defined limit
            if (config.MaxFollowersPerLevel.Count > 0)
                return config.MaxFollowersPerLevel[config.MaxFollowersPerLevel.Count - 1];

            return 8; // Default fallback
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

        #region Enemy Death Watcher
        private void UpdateEnemyDeathWatcher()
        {
            try
            {
                // Only scan if we have followers that might need weapons
                if (followerAIService.FollowerCount == 0)
                    return;

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
                    enemyDeathWatcher.ScanForDeadEnemies(carrier.transform.position);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Enemy death watcher error: " + e.Message);
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
                    ? "Neural Cortex Mk" + brainLevel
                    : "None equipped";

                bool persuadatronEquipped = GetPersuadatronCarrier() != null;
                string persuadatronStatus = persuadatronEquipped
                    ? "EQUIPPED (Aura Active)"
                    : "Not equipped";

                string cooldownStatus = persuasionService.IsReady
                    ? "READY"
                    : persuasionService.CooldownRemaining.ToString("F1") + "s";

                int maxFollowers = GetMaxFollowersForLevel(brainLevel);

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
                    "Persuadatron: " + persuadatronStatus + "\n" +
                    "Cooldown: " + cooldownStatus + "\n" +
                    "Valid Targets: " + targetInfo + "\n" +
                    "Followers: " + followerAIService.FollowerCount + "/" + maxFollowers + "\n" +
                    "Aura Range: " + config.PersuasionAuraRange + " units\n\n" +
                    "Enemy weapon drops: Active\n" +
                    "Implants Registered: " + implantService.GetImplantDefinitions().Count + "\n\n" +
                    "Hotkeys: F5=Status, F6=Reload, L=List Followers";

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
                int brainLevel = persuasionService.GetCurrentPersuadatronLevel();
                int maxFollowers = GetMaxFollowersForLevel(brainLevel);

                if (followers.Count == 0)
                {
                    Manager.GetUIManager().ShowSubtitle("No followers currently persuaded.", 3);
                    return;
                }

                string info = "=== Persuaded Followers (" + followers.Count + "/" + maxFollowers + ") ===\n\n";

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
