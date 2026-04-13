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
    /// Behavior priority:
    ///   1. Follow the Persuadatron carrier
    ///   2. If has weapon → auto-fire at enemies in range
    ///   3. If no weapon, weapon on ground nearby → pick it up
    ///   4. If no weapon and none nearby → do nothing (passive follow)
    /// 
    /// Persuaded units follow the carrier of the Persuadatron and engage
    /// hostile entities automatically. They pick up dropped weapons if unarmed.
    /// </summary>
    public class FollowerAIService
    {
        private readonly PersuadatronConfig config;
        private readonly List<PersuadedUnit> followers;
        private float lastUpdateTime;

        // Cached reflection fields
        private FieldInfo entityMovementField;
        private MethodInfo moveToMethod;
        private MethodInfo attackMethod;
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
                // Cache reflection for entity movement commands
                entityMovementField = typeof(GameEntity).GetField("m_Movement",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                // Try to find MoveTo method
                var movementType = typeof(GameEntity).Assembly.GetType("EntityMovement");
                if (movementType != null)
                {
                    moveToMethod = movementType.GetMethod("MoveTo",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                // Try to find attack method
                attackMethod = typeof(GameEntity).GetMethod("ServerSetAttackTarget",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (attackMethod == null)
                {
                    attackMethod = typeof(GameEntity).GetMethod("SetAttackTarget",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

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
        /// </summary>
        public void AddFollower(PersuadedUnit unit)
        {
            if (unit != null && !followers.Contains(unit))
            {
                followers.Add(unit);
                Debug.Log("PersuadatronMod: Added follower. Total: " + followers.Count);
            }
        }

        /// <summary>
        /// Removes a specific follower.
        /// </summary>
        public void RemoveFollower(PersuadedUnit unit)
        {
            followers.Remove(unit);
        }

        /// <summary>
        /// Main update loop for follower AI. Call from the mod's Update() method.
        /// </summary>
        public void UpdateFollowers(Vector3 carrierPosition)
        {
            if (Time.time < lastUpdateTime + config.FollowerUpdateInterval)
                return;

            lastUpdateTime = Time.time;

            // Clean up dead/expired followers
            CleanupFollowers();

            // Update each follower's behavior
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
        /// Removes dead or expired followers from the list.
        /// </summary>
        private void CleanupFollowers()
        {
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                if (followers[i].ShouldRemove)
                {
                    Debug.Log("PersuadatronMod: Removing follower (dead/expired). Remaining: " + (followers.Count - 1));
                    followers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Updates behavior for a single follower unit.
        /// Priority: Follow → Combat (if armed) → Pickup weapon → Idle follow
        /// </summary>
        private void UpdateFollowerBehavior(PersuadedUnit unit, Vector3 carrierPosition)
        {
            if (unit.Entity == null || unit.Transform == null)
                return;

            float distanceToCarrier = Vector3.Distance(unit.Transform.position, carrierPosition);

            // Priority 1: If too far, move to carrier
            if (distanceToCarrier > config.SprintCatchUpDistance)
            {
                MoveToPosition(unit, carrierPosition);
                unit.IsInCombat = false;
                return;
            }

            // Priority 2: If has weapon, look for enemies
            if (unit.HasWeapon)
            {
                GameEntity enemy = FindNearestEnemy(unit.Transform.position);
                if (enemy != null)
                {
                    AttackTarget(unit, enemy);
                    unit.IsInCombat = true;
                    unit.CurrentTarget = enemy;
                    return;
                }
                else
                {
                    unit.IsInCombat = false;
                    unit.CurrentTarget = null;
                }
            }

            // Priority 3: No weapon, look for weapon on ground
            if (!unit.HasWeapon && !unit.IsPickingUpWeapon)
            {
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

            // Priority 4: Follow carrier (maintain formation distance)
            if (distanceToCarrier > config.FollowDistance)
            {
                // Calculate position behind the carrier
                Vector3 followPos = carrierPosition;
                MoveToPosition(unit, followPos);
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

                // Fallback: direct transform movement (basic, no pathfinding)
                if (unit.Transform != null)
                {
                    Vector3 direction = (position - unit.Transform.position).normalized;
                    float speed = 5f; // Base movement speed
                    unit.Transform.position += direction * speed * config.FollowerUpdateInterval;
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
        private void AttackTarget(PersuadedUnit unit, GameEntity enemy)
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
                    var setTarget = typeof(GameEntity).GetMethod("SetTarget",
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
        private GameEntity FindNearestEnemy(Vector3 position)
        {
            try
            {
                Collider[] colliders = Physics.OverlapSphere(position, config.FollowerCombatRange);
                GameEntity nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    GameEntity entity = collider.GetComponent<GameEntity>();
                    if (entity == null)
                        entity = collider.GetComponentInParent<GameEntity>();

                    if (entity == null)
                        continue;

                    // Skip player agents and other followers
                    if (IsPlayerOrFollower(entity))
                        continue;

                    // Check if entity is hostile
                    if (!IsHostile(entity))
                        continue;

                    // Check if alive
                    var health = entity.GetHealth();
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
                Collider[] colliders = Physics.OverlapSphere(position, config.WeaponPickupRange);
                GameObject nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (Collider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null)
                        continue;

                    // Look for ItemPickup components
                    var pickup = collider.GetComponent<ItemPickup>();
                    if (pickup == null)
                        pickup = collider.GetComponentInParent<ItemPickup>();

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
                var pickup = weaponObject.GetComponent<ItemPickup>();
                if (pickup == null)
                    pickup = weaponObject.GetComponentInParent<ItemPickup>();

                if (pickup != null)
                {
                    // Try to invoke pickup via reflection
                    var pickupMethod = typeof(ItemPickup).GetMethod("PickUp",
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
        private bool IsWeaponPickup(ItemPickup pickup)
        {
            try
            {
                var itemField = typeof(ItemPickup).GetField("m_ItemID",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (itemField == null)
                {
                    itemField = typeof(ItemPickup).GetField("m_Item",
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
        private bool IsPlayerOrFollower(GameEntity entity)
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
        private bool IsHostile(GameEntity entity)
        {
            try
            {
                // Check entity's faction via reflection
                var factionField = typeof(GameEntity).GetField("m_Faction",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (factionField != null)
                {
                    var faction = factionField.GetValue(entity);
                    if (faction != null)
                    {
                        // The entity is hostile if its faction ID indicates enemy
                        // Faction 0 = player, Faction 1 = civilians, Faction 2+ = enemies
                        int factionID = (int)faction;
                        return factionID >= 2;
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
