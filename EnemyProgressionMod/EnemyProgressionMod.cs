using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EnemyProgressionMod
{
    /// <summary>
    /// Satellite Reign mod that replaces the vanilla enemy scaling system.
    /// 
    /// Vanilla problem: Enemies scale based on the player's equipment/upgrades,
    /// meaning investing in better gear results in equally stronger enemies,
    /// making progression feel pointless.
    /// 
    /// This mod fix: Enemy strength is tied to which district the player is in
    /// (game world progression), not player equipment. Early districts have weak
    /// enemies, later districts have strong enemies - and upgrading your gear
    /// gives you a real advantage.
    /// 
    /// Hotkeys:
    ///   F7 - Show current progression status
    ///   F8 - Reload config from file
    /// </summary>
    public class EnemyProgressionMod : ISrPlugin
    {
        private bool isInitialized;
        private bool hasAppliedOverrides;
        private float lastUpdateTime;
        private int lastDistrictIndex = -1;
        private ProgressionConfig config;

        // Cached reflection fields for SpawnManager access
        private FieldInfo spawnDecksField;
        private bool reflectionReady;

        // Tolerance for floating-point progression comparisons
        private const float PROGRESSION_TOLERANCE = 0.001f;

        // Snapshot of original vanilla spawn card progression values (for restore/reference)
        private Dictionary<int, OriginalSpawnCardData> originalSpawnData;

        public string GetName()
        {
            return "Enemy Progression Mod v1.0";
        }

        public void Initialize()
        {
            try
            {
                Debug.Log("EnemyProgressionMod: Initializing...");
                isInitialized = true;
                hasAppliedOverrides = false;
                lastUpdateTime = 0f;
                originalSpawnData = new Dictionary<int, OriginalSpawnCardData>();

                // Cache the reflection field for SpawnManager.m_SpawnDecks
                spawnDecksField = typeof(SpawnManager).GetField("m_SpawnDecks",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                reflectionReady = spawnDecksField != null;

                if (!reflectionReady)
                {
                    Debug.LogError("EnemyProgressionMod: Could not find m_SpawnDecks field via reflection. Mod will not function.");
                }

                Debug.Log("EnemyProgressionMod: Initialization complete. Reflection ready: " + reflectionReady);
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Initialization failed: " + e.Message);
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

                // Load config on first game frame
                if (config == null)
                {
                    LoadConfig();
                    // Show a message so the user knows the mod is loaded
                    try
                    {
                        Manager.GetUIManager().ShowMessagePopup(
                            "Enemy Progression Mod loaded!\n" +
                            "F7 = Status, F8 = Reload Config\n" +
                            "Reflection ready: " + reflectionReady,
                            5);
                    }
                    catch { }
                }

                // Handle hotkeys BEFORE the reflection/enabled check so
                // F7 (status) and F8 (reload) always work, even if reflection failed
                HandleInput();

                if (!config.Enabled || !reflectionReady)
                    return;

                // Check periodically
                if (Time.time < lastUpdateTime + config.UpdateIntervalSeconds)
                    return;

                lastUpdateTime = Time.time;

                // Detect district changes and apply overrides
                int currentDistrict = GetCurrentDistrictIndex();
                if (currentDistrict != lastDistrictIndex || !hasAppliedOverrides)
                {
                    if (currentDistrict != lastDistrictIndex && lastDistrictIndex != -1 && config.ShowDistrictChangeMessage)
                    {
                        float minProg, maxProg;
                        config.GetProgressionForDistrict(currentDistrict, out minProg, out maxProg);
                        string districtName = GetDistrictName(currentDistrict);

                        Manager.GetUIManager().ShowMessagePopup(
                            "Enemy Progression Mod\n" +
                            "Entered: " + districtName + "\n" +
                            "Enemy Level Range: " + (minProg * 100f).ToString("F0") + "% - " + (maxProg * 100f).ToString("F0") + "%",
                            5);
                    }

                    lastDistrictIndex = currentDistrict;
                    ApplyProgressionOverrides(currentDistrict);
                    hasAppliedOverrides = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Update error: " + e.Message);
            }
        }

        private void HandleInput()
        {
            // F7 - Show current status
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ShowProgressionStatus();
            }

            // F8 - Reload config
            if (Input.GetKeyDown(KeyCode.F8))
            {
                LoadConfig();
                hasAppliedOverrides = false; // Force re-apply
                Manager.GetUIManager().ShowMessagePopup("Enemy Progression Mod: Config reloaded!", 3);
            }
        }

        /// <summary>
        /// Gets the current district index from the game's ProgressionManager.
        /// </summary>
        private int GetCurrentDistrictIndex()
        {
            try
            {
                var progressionManager = ProgressionManager.Get();
                if (progressionManager != null)
                {
                    // CurrentDistrict returns a District enum value which casts to int
                    return (int)progressionManager.CurrentDistrict;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Could not get current district: " + e.Message);
            }
            return 0; // Default to first district
        }

        /// <summary>
        /// Gets a human-readable name for the district.
        /// </summary>
        private string GetDistrictName(int districtIndex)
        {
            foreach (var entry in config.DistrictProgressionOverrides)
            {
                if (entry.DistrictIndex == districtIndex)
                    return entry.Name;
            }

            // Try to get from the District enum
            try
            {
                District district = (District)districtIndex;
                return district.ToString();
            }
            catch
            {
                return "District " + districtIndex;
            }
        }

        /// <summary>
        /// Core logic: Overrides the SpawnManager's spawn decks so that
        /// enemy progression values are locked to the current district's range
        /// instead of scaling with the player's power.
        /// </summary>
        private void ApplyProgressionOverrides(int districtIndex)
        {
            try
            {
                SpawnManager spawnManager = SpawnManager.Get();
                if (spawnManager == null)
                {
                    Debug.LogError("EnemyProgressionMod: SpawnManager is null");
                    return;
                }

                float minProg, maxProg;
                config.GetProgressionForDistrict(districtIndex, out minProg, out maxProg);

                // Access the private m_SpawnDecks field
                var spawnDecks = (Dictionary<GroupID, List<SpawnCard>>)spawnDecksField.GetValue(spawnManager);
                if (spawnDecks == null)
                {
                    Debug.LogWarning("EnemyProgressionMod: m_SpawnDecks is null, waiting for SpawnManager initialization...");
                    return;
                }

                int modifiedCount = 0;
                int totalCards = 0;

                foreach (var kvp in spawnDecks)
                {
                    foreach (SpawnCard card in kvp.Value)
                    {
                        totalCards++;
                        int uid = card.m_Enemy != null ? card.m_Enemy.m_UID : -1;

                        // Store original values on first encounter
                        if (uid >= 0 && !originalSpawnData.ContainsKey(uid))
                        {
                            originalSpawnData[uid] = new OriginalSpawnCardData
                            {
                                UID = uid,
                                OriginalMinProgression = card.m_MinProgression,
                                OriginalMaxProgression = card.m_MaxProgression,
                                EnemyName = card.m_Enemy != null ? card.m_Enemy.m_EnemyName : "Unknown"
                            };
                        }

                        // Clamp the spawn card's progression to the district range.
                        // This means:
                        // - Cards meant for higher progression won't spawn in early districts
                        // - Cards for lower progression will be the ones that appear
                        // - The key change: this is based on DISTRICT, not player equipment
                        float newMin = Mathf.Clamp(card.m_MinProgression, minProg, maxProg);
                        float newMax = Mathf.Clamp(card.m_MaxProgression, minProg, maxProg);

                        // Ensure min <= max with proper bounds
                        newMin = Mathf.Min(newMin, newMax);
                        newMax = Mathf.Max(newMin, newMax);

                        if (Math.Abs(card.m_MinProgression - newMin) > PROGRESSION_TOLERANCE ||
                            Math.Abs(card.m_MaxProgression - newMax) > PROGRESSION_TOLERANCE)
                        {
                            card.m_MinProgression = newMin;
                            card.m_MaxProgression = newMax;
                            modifiedCount++;
                        }
                    }
                }

                Debug.Log("EnemyProgressionMod: Applied district " + districtIndex + " (" + GetDistrictName(districtIndex) +
                    ") overrides. Range: " + minProg.ToString("F2") + "-" + maxProg.ToString("F2") +
                    ". Modified " + modifiedCount + "/" + totalCards + " spawn cards.");
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Failed to apply overrides: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Shows a detailed status popup with current progression info.
        /// </summary>
        private void ShowProgressionStatus()
        {
            try
            {
                int districtIndex = GetCurrentDistrictIndex();
                string districtName = GetDistrictName(districtIndex);
                float minProg, maxProg;
                config.GetProgressionForDistrict(districtIndex, out minProg, out maxProg);

                SpawnManager spawnManager = SpawnManager.Get();
                int totalEnemyDefs = spawnManager != null ? spawnManager.m_EnemyDefinitions.Count : 0;

                int totalCards = 0;
                int cardsInRange = 0;

                if (spawnManager != null)
                {
                    var spawnDecks = (Dictionary<GroupID, List<SpawnCard>>)spawnDecksField.GetValue(spawnManager);
                    if (spawnDecks != null)
                    {
                        foreach (var kvp in spawnDecks)
                        {
                            foreach (SpawnCard card in kvp.Value)
                            {
                                totalCards++;
                                if (card.m_MinProgression >= minProg - PROGRESSION_TOLERANCE && card.m_MaxProgression <= maxProg + PROGRESSION_TOLERANCE)
                                    cardsInRange++;
                            }
                        }
                    }
                }

                string status =
                    "=== Enemy Progression Mod ===\n\n" +
                    "Status: " + (config.Enabled ? "ACTIVE" : "DISABLED") + "\n" +
                    "Reflection Ready: " + (reflectionReady ? "YES" : "NO - spawn overrides will not work!") + "\n\n" +
                    "--- Schwierigkeitsgrad / Difficulty ---\n" +
                    "Aktueller Distrikt: " + districtName + " (Index " + districtIndex + ")\n" +
                    "Gegner-Level: " + (minProg * 100f).ToString("F0") + "% - " + (maxProg * 100f).ToString("F0") + "%\n\n" +
                    "Enemy Definitions: " + totalEnemyDefs + "\n" +
                    "Spawn Cards: " + totalCards + " total, " + cardsInRange + " in current range\n" +
                    "Original Data Cached: " + originalSpawnData.Count + " entries\n\n" +
                    "District Progression Map:\n";

                foreach (var entry in config.DistrictProgressionOverrides)
                {
                    string marker = entry.DistrictIndex == districtIndex ? " <<< YOU ARE HERE" : "";
                    status += "  " + entry.Name + ": " +
                        (entry.MinProgression * 100f).ToString("F0") + "% - " +
                        (entry.MaxProgression * 100f).ToString("F0") + "%" + marker + "\n";
                }

                status += "\nHotkeys: F7 = Status, F8 = Reload Config";

                Manager.GetUIManager().ShowMessagePopup(status, 15);
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup("Enemy Progression Mod: Error showing status - " + e.Message, 5);
            }
        }

        private void LoadConfig()
        {
            try
            {
                string pluginPath = Manager.GetPluginManager().PluginPath;
                config = ProgressionConfig.Load(pluginPath);
                Debug.Log("EnemyProgressionMod: Config loaded. Enabled: " + config.Enabled +
                    ", Districts configured: " + config.DistrictProgressionOverrides.Count);
            }
            catch (Exception e)
            {
                Debug.LogError("EnemyProgressionMod: Config load failed, using defaults: " + e.Message);
                config = ProgressionConfig.CreateDefault();
            }
        }

        /// <summary>
        /// Internal data structure to remember original (vanilla) spawn card values.
        /// </summary>
        private class OriginalSpawnCardData
        {
            public int UID;
            public float OriginalMinProgression;
            public float OriginalMaxProgression;
            public string EnemyName;
        }
    }
}
