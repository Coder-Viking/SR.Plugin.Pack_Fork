using System;
using System.Collections.Generic;
using System.Reflection;
using PersuadatronMod.Config;
using UnityEngine;

namespace PersuadatronMod.Services
{
    /// <summary>
    /// Monitors enemy deaths and spawns weapon pickups from their corpses.
    /// 
    /// When an enemy dies, their power level is estimated and a matching weapon
    /// is dropped at their position. This enables persuaded followers (who are
    /// unarmed civilians) to pick up weapons and fight.
    /// 
    /// Uses the game's ItemPickup system to spawn real weapon items.
    /// </summary>
    public class EnemyDeathWatcherService
    {
        private readonly PersuadatronConfig config;
        private float lastScanTime;

        // Track entities we've already processed to avoid duplicate drops
        private readonly HashSet<int> processedDeaths;

        // Cached reflection for ItemPickup spawning
        private Type itemPickupType;
        private bool reflectionReady;

        // Weapon IDs from game's built-in arsenal, mapped by tier
        // These are common weapon IDs found in the game's ItemDefinitions
        private readonly int[] tierWeaponIDs;

        public EnemyDeathWatcherService(PersuadatronConfig config)
        {
            this.config = config;
            this.lastScanTime = 0f;
            this.processedDeaths = new HashSet<int>();

            // Default weapon IDs — will be resolved from actual game items at runtime
            this.tierWeaponIDs = new int[0];

            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                itemPickupType = typeof(AIEntity).Assembly.GetType("ItemPickup");
                reflectionReady = itemPickupType != null;

                if (reflectionReady)
                {
                    Debug.Log("PersuadatronMod: EnemyDeathWatcher reflection initialized");
                }
                else
                {
                    Debug.LogWarning("PersuadatronMod: ItemPickup type not found — weapon drops disabled");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: EnemyDeathWatcher reflection failed: " + e.Message);
                reflectionReady = false;
            }
        }

        /// <summary>
        /// Scans for recently killed enemies near the given position and drops weapons.
        /// Call from the mod's Update() loop.
        /// </summary>
        public void ScanForDeadEnemies(Vector3 scanCenter)
        {
            if (Time.time < lastScanTime + config.EnemyDeathScanInterval)
                return;

            lastScanTime = Time.time;

            try
            {
                Collider[] colliders = Physics.OverlapSphere(scanCenter, config.EnemyDeathScanRange);

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    AIEntity entity = collider.GetComponent<AIEntity>();
                    if (entity == null)
                        entity = collider.GetComponentInParent<AIEntity>();

                    if (entity == null)
                        continue;

                    int entityID = entity.GetInstanceID();

                    // Skip if already processed
                    if (processedDeaths.Contains(entityID))
                        continue;

                    // Skip player agents
                    if (entity is AgentAI)
                        continue;

                    // Check if dead
                    if (entity.m_Health == null || entity.m_Health.HealthValue > 0f)
                        continue;

                    // Check if it was a combatant (not a civilian — low ability count)
                    if (!WasArmedCombatant(entity))
                        continue;

                    // Mark as processed
                    processedDeaths.Add(entityID);

                    // Drop a weapon at the corpse location
                    TryDropWeapon(entity);
                }

                // Prune processed deaths set to avoid unbounded growth
                // (entities get destroyed/recycled, old IDs become irrelevant)
                if (processedDeaths.Count > 500)
                {
                    processedDeaths.Clear();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: EnemyDeathWatcher scan error: " + e.Message);
            }
        }

        /// <summary>
        /// Checks if a dead entity was an armed combatant (not a civilian).
        /// </summary>
        private bool WasArmedCombatant(AIEntity entity)
        {
            try
            {
                // Check faction — faction 2+ are enemy combatants
                var factionField = typeof(AIEntity).GetField("m_Faction",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (factionField != null)
                {
                    var faction = factionField.GetValue(entity);
                    if (faction != null)
                    {
                        int factionID = Convert.ToInt32(faction);
                        if (factionID >= 2)
                            return true;
                    }
                }

                // Fallback: check if entity had combat abilities (more than 3 abilities = combatant)
                var abilities = entity.GetAbilities();
                if (abilities != null)
                {
                    var allAbilities = abilities.AllAbilities();
                    if (allAbilities != null && allAbilities.Count > 3)
                        return true;
                }
            }
            catch
            {
                // Ignore
            }
            return false;
        }

        /// <summary>
        /// Drops a weapon at the position of a dead enemy entity.
        /// The weapon tier is determined by the enemy's estimated power level.
        /// </summary>
        private void TryDropWeapon(AIEntity deadEntity)
        {
            try
            {
                Vector3 dropPosition = deadEntity.transform.position;

                // Determine an appropriate weapon based on power level
                int weaponID = GetWeaponIDForEntity(deadEntity);
                if (weaponID <= 0)
                    return;

                // Spawn the weapon as a pickup
                SpawnWeaponPickup(weaponID, dropPosition);

                Debug.Log("PersuadatronMod: Dropped weapon (ID=" + weaponID + ") at dead enemy position");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: TryDropWeapon failed: " + e.Message);
            }
        }

        /// <summary>
        /// Selects an appropriate weapon ID based on the dead enemy's power level.
        /// Uses the game's existing weapon items from the ItemManager.
        /// </summary>
        private int GetWeaponIDForEntity(AIEntity entity)
        {
            try
            {
                float maxHealth = entity.m_Health != null ? entity.m_Health.GetMaxHealth() : 100f;

                // Scale: 50 HP = civilian (no drop), 100 HP = basic, 200+ = mid, 300+ = advanced
                const float civilianHP = 50f;

                if (maxHealth <= civilianHP)
                    return 0; // Civilians don't drop weapons

                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                    return 0;

                // Find suitable weapons from the game's item definitions
                // Prefer weapons appropriate to the enemy's power level
                List<int> pistolIDs = new List<int>();
                List<int> rifleIDs = new List<int>();
                List<int> heavyIDs = new List<int>();

                foreach (var item in itemManager.m_ItemDefinitions)
                {
                    if (item == null)
                        continue;

                    if (item.m_Slot == ItemSlotTypes.WeaponPistol)
                    {
                        pistolIDs.Add(item.m_ID);
                    }
                    else if (item.m_Slot == ItemSlotTypes.Weapon)
                    {
                        if (item.m_WeaponType == WeaponType.SniperRifle ||
                            item.m_WeaponType == WeaponType.HeavyWeapon)
                        {
                            heavyIDs.Add(item.m_ID);
                        }
                        else
                        {
                            rifleIDs.Add(item.m_ID);
                        }
                    }
                }

                // Select weapon tier based on enemy HP
                if (maxHealth >= 300f && heavyIDs.Count > 0)
                {
                    // High-level enemy: drop a heavy/sniper weapon
                    return heavyIDs[UnityEngine.Random.Range(0, heavyIDs.Count)];
                }
                else if (maxHealth >= 150f && rifleIDs.Count > 0)
                {
                    // Mid-level enemy: drop a rifle
                    return rifleIDs[UnityEngine.Random.Range(0, rifleIDs.Count)];
                }
                else if (pistolIDs.Count > 0)
                {
                    // Low-level enemy: drop a pistol
                    return pistolIDs[UnityEngine.Random.Range(0, pistolIDs.Count)];
                }
                else if (rifleIDs.Count > 0)
                {
                    // Fallback to any rifle
                    return rifleIDs[UnityEngine.Random.Range(0, rifleIDs.Count)];
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: GetWeaponIDForEntity failed: " + e.Message);
            }

            return 0;
        }

        /// <summary>
        /// Spawns a weapon pickup at the given world position using the game's ItemPickup system.
        /// </summary>
        private void SpawnWeaponPickup(int weaponID, Vector3 position)
        {
            try
            {
                if (!reflectionReady || itemPickupType == null)
                    return;

                ItemManager itemManager = Manager.GetItemManager();
                if (itemManager == null)
                    return;

                // Try to use the game's item drop/spawn system
                // Look for a static or instance method to create pickups
                var spawnMethod = typeof(ItemManager).GetMethod("SpawnItemPickup",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (spawnMethod != null)
                {
                    spawnMethod.Invoke(itemManager, new object[] { weaponID, position });
                    return;
                }

                // Alternative: try DropItem
                var dropMethod = typeof(ItemManager).GetMethod("DropItem",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (dropMethod != null)
                {
                    dropMethod.Invoke(itemManager, new object[] { weaponID, position });
                    return;
                }

                // Alternative: try CreatePickup
                var createMethod = typeof(ItemManager).GetMethod("CreatePickup",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (createMethod != null)
                {
                    createMethod.Invoke(itemManager, new object[] { weaponID, position });
                    return;
                }

                // Fallback: try to create a pickup GameObject directly
                // Create a simple GameObject at position with an ItemPickup component
                var createPickupStatic = itemPickupType.GetMethod("Create",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                if (createPickupStatic != null)
                {
                    createPickupStatic.Invoke(null, new object[] { weaponID, position });
                    return;
                }

                // Final fallback: create a bare pickup GameObject
                SpawnPickupFallback(weaponID, position);
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: SpawnWeaponPickup failed: " + e.Message);
            }
        }

        /// <summary>
        /// Fallback weapon pickup spawning: creates a minimal GameObject with the ItemPickup component.
        /// </summary>
        private void SpawnPickupFallback(int weaponID, Vector3 position)
        {
            try
            {
                ItemManager itemManager = Manager.GetItemManager();
                var itemData = itemManager.GetItemData(weaponID);
                if (itemData == null)
                    return;

                // Create a simple pickup object
                GameObject pickupObj = new GameObject("WeaponDrop_" + weaponID);
                pickupObj.transform.position = position + Vector3.up * 0.3f; // Slightly above ground

                // Add a collider so it can be found by OverlapSphere
                SphereCollider col = pickupObj.AddComponent<SphereCollider>();
                col.radius = 0.5f;
                col.isTrigger = true;

                // Add the ItemPickup component via reflection
                var pickupComponent = pickupObj.AddComponent(itemPickupType);
                if (pickupComponent != null)
                {
                    // Try to set the item ID on the pickup
                    var itemIDField = itemPickupType.GetField("m_ItemID",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (itemIDField != null)
                    {
                        itemIDField.SetValue(pickupComponent, weaponID);
                    }
                    else
                    {
                        var itemField = itemPickupType.GetField("m_Item",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (itemField != null)
                        {
                            itemField.SetValue(pickupComponent, weaponID);
                        }
                    }
                }

                // Auto-destroy after 60 seconds to avoid clutter
                UnityEngine.Object.Destroy(pickupObj, 60f);

                Debug.Log("PersuadatronMod: Spawned fallback weapon pickup (ID=" + weaponID +
                    ") at " + position);
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: SpawnPickupFallback failed: " + e.Message);
            }
        }
    }
}
