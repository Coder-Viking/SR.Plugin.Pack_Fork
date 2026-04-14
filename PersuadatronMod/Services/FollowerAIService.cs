using System;
using System.Collections.Generic;
using System.Reflection;
using PersuadatronMod.Config;
using PersuadatronMod.Models;
using UnityEngine;

namespace PersuadatronMod.Services
{
    /// <summary>
    /// AI controller for persuaded units.
    /// 
    /// Uses the game's built-in Hijack system to make persuaded units behave
    /// like hijacked soldiers. They follow the player naturally using the game's
    /// pathfinding, fight enemies automatically, and pick up weapons.
    /// 
    /// The Hijack system handles:
    ///   - Natural pathfinding-based following (no teleportation)
    ///   - Combat engagement (units fight instead of fleeing)
    ///   - Proper integration with the game's AI state machine
    /// </summary>
    public class FollowerAIService
    {
        private readonly PersuadatronConfig config;
        private readonly List<PersuadedUnit> followers;
        private float lastUpdateTime;

        // Cached reflection fields for fallback behavior
        private FieldInfo entityMovementField;
        private MethodInfo moveToMethod;
        private MethodInfo attackMethod;
        private Type itemPickupType;
        private bool reflectionReady;

        public FollowerAIService(PersuadatronConfig config)
        {
            this.config = config;
            this.followers = new List<PersuadedUnit>();
            this.lastUpdateTime = 0f;

            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                // Cache reflection for entity movement commands (fallback)
                entityMovementField = typeof(AIEntity).GetField("m_Movement",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                // Try to find MoveTo method
                var movementType = typeof(AIEntity).Assembly.GetType("EntityMovement");
                if (movementType != null)
                {
                    moveToMethod = movementType.GetMethod("MoveTo",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                // Try to find attack method
                attackMethod = typeof(AIEntity).GetMethod("ServerSetAttackTarget",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (attackMethod == null)
                {
                    attackMethod = typeof(AIEntity).GetMethod("SetAttackTarget",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                // Try to find ItemPickup type at runtime
                itemPickupType = typeof(AIEntity).Assembly.GetType("ItemPickup");

                reflectionReady = true;
                Debug.Log("PersuadatronMod: FollowerAI reflection initialized");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: FollowerAI reflection failed: " + e.Message);
                reflectionReady = false;
            }
        }

        /// <summary>
        /// Current number of active followers.
        /// </summary>
        public int FollowerCount
        {
            get { return followers.Count; }
        }

        /// <summary>
        /// Gets the list of current followers (read-only copy).
        /// </summary>
        public List<PersuadedUnit> GetFollowers()
        {
            return new List<PersuadedUnit>(followers);
        }

        /// <summary>
        /// Adds a newly persuaded unit to the follower list.
        /// Uses the game's Hijack system to make the unit behave like a hijacked soldier,
        /// enabling natural following, combat participation, and weapon usage.
        /// </summary>
        public void AddFollower(PersuadedUnit unit)
        {
            if (unit != null && !followers.Contains(unit))
            {
                followers.Add(unit);
                HijackUnit(unit);
                Debug.Log("PersuadatronMod: Added follower (hijacked). Total: " + followers.Count);
            }
        }

        /// <summary>
        /// Removes a specific follower and releases hijack control.
        /// </summary>
        public void RemoveFollower(PersuadedUnit unit)
        {
            if (unit != null)
            {
                UnhijackUnit(unit);
            }
            followers.Remove(unit);
        }

        /// <summary>
        /// Hijacks a persuaded unit using the game's built-in Hijack system.
        /// This makes the unit behave like a hijacked soldier:
        /// - Follows the player agent naturally via game pathfinding
        /// - Fights enemies instead of fleeing
        /// - Uses equipped weapons in combat
        /// </summary>
        private void HijackUnit(PersuadedUnit unit)
        {
            try
            {
                if (unit == null || unit.Entity == null)
                    return;

                AIEntity entity = unit.Entity;

                // Use the game's Hijack method, same as SyndicateMod's ToggleControlOfUnits
                entity.m_IsIgnoringInput = false;
                entity.m_IsControllable = true;
                entity.Hijack(AgentAI.GetAgent(AgentAI.AgentClass.Soldier), true);

                // Visual feedback: cyan xray color for persuaded units
                try
                {
                    entity.m_Wardrobe.SetXrayColor(Color.cyan);
                    entity.UpdateXrayColor();
                }
                catch
                {
                    // Visual feedback is optional, ignore errors
                }

                // Check if the entity already has a weapon equipped
                try
                {
                    var items = entity.GetItems();
                    if (items != null)
                    {
                        // Check weapon slots for any equipped weapon
                        var weaponItem = items.GetEquippedItem(ItemSlotTypes.Weapon, 0);
                        var pistolItem = items.GetEquippedItem(ItemSlotTypes.WeaponPistol, 0);
                        if (weaponItem != null || pistolItem != null)
                        {
                            unit.HasWeapon = true;
                        }
                    }
                }
                catch
                {
                    // Weapon check is optional
                }

                Debug.Log("PersuadatronMod: Hijacked unit for persuasion control");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: HijackUnit failed: " + e.Message);
            }
        }

        /// <summary>
        /// Releases hijack control of a persuaded unit.
        /// </summary>
        private void UnhijackUnit(PersuadedUnit unit)
        {
            try
            {
                if (unit == null || unit.Entity == null)
                    return;

                AIEntity entity = unit.Entity;
                entity.m_IsControllable = false;
                entity.Unhijack();

                // Reset xray color
                try
                {
                    entity.m_Wardrobe.SetXrayColor(Color.blue);
                    entity.UpdateXrayColor();
                }
                catch
                {
                    // Visual feedback is optional
                }

                Debug.Log("PersuadatronMod: Released hijack control of unit");
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: UnhijackUnit failed: " + e.Message);
            }
        }

        /// <summary>
        /// Main update loop for follower AI. Call from the mod's Update() method.
        /// With the Hijack system active, the game handles most AI behavior.
        /// This update loop handles cleanup, weapon pickup, and leash distance.
        /// </summary>
        public void UpdateFollowers(Vector3 carrierPosition)
        {
            if (Time.time < lastUpdateTime + config.FollowerUpdateInterval)
                return;

            lastUpdateTime = Time.time;

            // Clean up dead/expired followers (unhijacks them)
            CleanupFollowers();

            // Update each follower's supplementary behavior
            for (int i = 0; i < followers.Count; i++)
            {
                try
                {
                    UpdateFollowerBehavior(followers[i], carrierPosition);
                }
                catch (Exception e)
                {
                    Debug.LogError("PersuadatronMod: Follower AI error for unit " + i + ": " + e.Message);
                }
            }
        }

        /// <summary>
        /// Removes dead or expired followers from the list, releasing hijack control.
        /// </summary>
        private void CleanupFollowers()
        {
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                if (followers[i].ShouldRemove)
                {
                    UnhijackUnit(followers[i]);
                    Debug.Log("PersuadatronMod: Removing follower (dead/expired). Remaining: " + (followers.Count - 1));
                    followers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Updates supplementary behavior for a single follower unit.
        /// The Hijack system handles core movement and combat.
        /// This method handles:
        ///   - Leash distance (move to carrier if too far)
        ///   - Weapon pickup for unarmed followers
        ///   - Combat state tracking
        /// </summary>
        private void UpdateFollowerBehavior(PersuadedUnit unit, Vector3 carrierPosition)
        {
            if (unit.Entity == null || unit.Transform == null)
                return;

            float distanceToCarrier = Vector3.Distance(unit.Transform.position, carrierPosition);

            // If too far from carrier, command the hijacked unit to move back
            if (distanceToCarrier > config.SprintCatchUpDistance)
            {
                MoveToPosition(unit, carrierPosition);
                unit.IsInCombat = false;
                return;
            }

            // Update weapon status from entity's actual equipment
            UpdateWeaponStatus(unit);

            // If armed, let the hijack AI handle combat
            // Just update our tracking state based on whether enemies are nearby
            if (unit.HasWeapon)
            {
                AIEntity enemy = FindNearestEnemy(unit.Transform.position);
                if (enemy != null)
                {
                    // Ensure the hijacked unit is attacking the nearest enemy
                    AttackTarget(unit, enemy);
                    unit.IsInCombat = true;
                    unit.CurrentTarget = enemy;
                }
                else
                {
                    unit.IsInCombat = false;
                    unit.CurrentTarget = null;

                    // No enemies and far from carrier - follow
                    if (distanceToCarrier > config.FollowDistance)
                    {
                        MoveToPosition(unit, carrierPosition);
                    }
                }
                return;
            }

            // Unarmed: look for weapon pickups (retry every WeaponSearchRetryInterval seconds)
            if (!unit.HasWeapon && !unit.IsPickingUpWeapon)
            {
                if (Time.time >= unit.LastWeaponSearchTime + config.WeaponSearchRetryInterval)
                {
                    unit.LastWeaponSearchTime = Time.time;
                    GameObject weaponPickup = FindNearestWeaponPickup(unit.Transform.position);
                    if (weaponPickup != null)
                    {
                        MoveToPosition(unit, weaponPickup.transform.position);
                        unit.IsPickingUpWeapon = true;

                        // Check if close enough to pick up
                        float distToWeapon = Vector3.Distance(unit.Transform.position, weaponPickup.transform.position);
                        if (distToWeapon < 2f)
                        {
                            TryPickupWeapon(unit, weaponPickup);
                            unit.IsPickingUpWeapon = false;
                        }
                        return;
                    }
                }
            }

            // Unarmed, no weapons nearby: follow carrier
            if (distanceToCarrier > config.FollowDistance)
            {
                MoveToPosition(unit, carrierPosition);
            }
        }

        /// <summary>
        /// Updates the weapon status of a follower based on actual equipped items.
        /// </summary>
        private void UpdateWeaponStatus(PersuadedUnit unit)
        {
            try
            {
                var items = unit.Entity.GetItems();
                if (items != null)
                {
                    var weaponItem = items.GetEquippedItem(ItemSlotTypes.Weapon, 0);
                    var pistolItem = items.GetEquippedItem(ItemSlotTypes.WeaponPistol, 0);
                    unit.HasWeapon = (weaponItem != null || pistolItem != null);
                }
            }
            catch
            {
                // Weapon check is optional
            }
        }

        /// <summary>
        /// Commands a follower to move to a world position.
        /// Uses the entity movement system via reflection.
        /// </summary>
        private void MoveToPosition(PersuadedUnit unit, Vector3 position)
        {
            try
            {
                if (entityMovementField != null && moveToMethod != null)
                {
                    var movement = entityMovementField.GetValue(unit.Entity);
                    if (movement != null)
                    {
                        moveToMethod.Invoke(movement, new object[] { position });
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: MoveToPosition failed: " + e.Message);
            }
        }

        /// <summary>
        /// Commands a follower to attack an enemy entity.
        /// </summary>
        private void AttackTarget(PersuadedUnit unit, AIEntity enemy)
        {
            try
            {
                if (attackMethod != null)
                {
                    attackMethod.Invoke(unit.Entity, new object[] { enemy });
                    return;
                }

                // Fallback: try to set target via available methods
                try
                {
                    var setTarget = typeof(AIEntity).GetMethod("SetTarget",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (setTarget != null)
                    {
                        setTarget.Invoke(unit.Entity, new object[] { enemy });
                    }
                }
                catch
                {
                    // Silent fallback - just move towards enemy
                    if (unit.Transform != null && enemy.transform != null)
                    {
                        MoveToPosition(unit, enemy.transform.position);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: AttackTarget failed: " + e.Message);
            }
        }

        /// <summary>
        /// Finds the nearest enemy entity to the given position.
        /// Enemies are determined by faction opposition to the player.
        /// </summary>
        private AIEntity FindNearestEnemy(Vector3 position)
        {
            try
            {
                Collider[] colliders = Physics.OverlapSphere(position, config.FollowerCombatRange);
                AIEntity nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    AIEntity entity = collider.GetComponent<AIEntity>();
                    if (entity == null)
                        entity = collider.GetComponentInParent<AIEntity>();

                    if (entity == null)
                        continue;

                    // Skip player agents and other followers
                    if (IsPlayerOrFollower(entity))
                        continue;

                    // Check if entity is hostile
                    if (!IsHostile(entity))
                        continue;

                    // Check if alive
                    var health = entity.m_Health;
                    if (health == null || health.HealthValue <= 0f)
                        continue;

                    float distance = Vector3.Distance(position, entity.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = entity;
                    }
                }

                return nearest;
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: FindNearestEnemy error: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Finds the nearest dropped weapon on the ground near the position.
        /// </summary>
        private GameObject FindNearestWeaponPickup(Vector3 position)
        {
            try
            {
                if (itemPickupType == null)
                    return null;

                Collider[] colliders = Physics.OverlapSphere(position, config.WeaponPickupRange);
                GameObject nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (Collider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null)
                        continue;

                    // Look for ItemPickup components via runtime type
                    var pickup = collider.GetComponent(itemPickupType);
                    if (pickup == null)
                        pickup = collider.GetComponentInParent(itemPickupType);

                    if (pickup == null)
                        continue;

                    // Check if it's a weapon
                    if (!IsWeaponPickup(pickup))
                        continue;

                    float distance = Vector3.Distance(position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = collider.gameObject;
                    }
                }

                return nearest;
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: FindNearestWeaponPickup error: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Attempts to have a follower pick up a weapon from the ground.
        /// </summary>
        private void TryPickupWeapon(PersuadedUnit unit, GameObject weaponObject)
        {
            try
            {
                if (itemPickupType == null)
                    return;

                var pickup = weaponObject.GetComponent(itemPickupType);
                if (pickup == null)
                    pickup = weaponObject.GetComponentInParent(itemPickupType);

                if (pickup != null)
                {
                    // Try to invoke pickup via reflection
                    var pickupMethod = itemPickupType.GetMethod("PickUp",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (pickupMethod != null)
                    {
                        pickupMethod.Invoke(pickup, new object[] { unit.Entity });
                        unit.HasWeapon = true;
                        Debug.Log("PersuadatronMod: Follower picked up weapon");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("PersuadatronMod: Weapon pickup failed: " + e.Message);
            }
        }

        /// <summary>
        /// Checks if a pickup is a weapon item.
        /// </summary>
        private bool IsWeaponPickup(Component pickup)
        {
            try
            {
                if (itemPickupType == null)
                    return false;

                var itemField = itemPickupType.GetField("m_ItemID",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (itemField == null)
                {
                    itemField = itemPickupType.GetField("m_Item",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (itemField != null)
                {
                    var value = itemField.GetValue(pickup);
                    if (value is int)
                    {
                        int itemID = (int)value;
                        var itemData = Manager.GetItemManager().GetItemData(itemID);
                        if (itemData != null)
                        {
                            return itemData.m_Slot == ItemSlotTypes.Weapon ||
                                   itemData.m_Slot == ItemSlotTypes.WeaponPistol;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors - not a weapon
            }
            return false;
        }

        /// <summary>
        /// Checks if an entity is a player agent or one of our followers.
        /// </summary>
        private bool IsPlayerOrFollower(AIEntity entity)
        {
            // Check player agents
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent != null && agent.gameObject == entity.gameObject)
                        return true;
                }
            }
            catch
            {
                // Ignore
            }

            // Check followers
            foreach (var follower in followers)
            {
                if (follower.Entity == entity)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if an entity is hostile to the player.
        /// </summary>
        private bool IsHostile(AIEntity entity)
        {
            try
            {
                // Check entity's faction via reflection
                var factionField = typeof(AIEntity).GetField("m_Faction",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (factionField != null)
                {
                    var faction = factionField.GetValue(entity);
                    if (faction != null && faction is int)
                    {
                        // The entity is hostile if its faction ID indicates enemy
                        // Faction 0 = player, Faction 1 = civilians, Faction 2+ = enemies
                        int factionID = (int)faction;
                        return factionID >= 2;
                    }
                    else if (faction != null)
                    {
                        // Faction may be an enum; try converting via underlying type
                        try
                        {
                            int factionID = Convert.ToInt32(faction);
                            return factionID >= 2;
                        }
                        catch
                        {
                            // Unable to determine faction
                        }
                    }
                }

                // Fallback: check if entity has combat abilities (likely hostile)
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
    }
}
