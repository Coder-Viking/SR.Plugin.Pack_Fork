using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PersuadatronMod.Models
{
    /// <summary>
    /// Defines a Syndicate Wars-style weapon with all stats for registration in the game.
    /// </summary>
    [Serializable]
    public class WeaponDefinition
    {
        /// <summary>
        /// Unique item ID for the weapon item.
        /// </summary>
        [XmlElement("ItemID")]
        public int ItemID;

        /// <summary>
        /// Display name of the weapon.
        /// </summary>
        [XmlElement("Name")]
        public string Name;

        /// <summary>
        /// Weapon type: 1=Pistol, 2=AssaultRifle, 3=SniperRifle, 4=HeavyWeapon, 5=Grenade
        /// </summary>
        [XmlElement("WeaponType")]
        public int WeaponType;

        /// <summary>
        /// Maximum engagement range in world units.
        /// </summary>
        [XmlElement("Range")]
        public float Range;

        /// <summary>
        /// Whether the weapon can fire while switching targets.
        /// </summary>
        [XmlElement("ShootWhileChangeTarget")]
        public bool ShootWhileChangeTarget;

        /// <summary>
        /// Default ammo type (as int).
        /// </summary>
        [XmlElement("DefaultAmmo")]
        public int DefaultAmmo;

        /// <summary>
        /// Ammo configurations for this weapon.
        /// </summary>
        [XmlArray("AmmoTypes")]
        [XmlArrayItem("Ammo")]
        public List<WeaponAmmoDefinition> AmmoTypes = new List<WeaponAmmoDefinition>();

        /// <summary>
        /// Ability IDs attached to this weapon.
        /// </summary>
        [XmlArray("Abilities")]
        [XmlArrayItem("AbilityID")]
        public List<int> AbilityIDs = new List<int>();

        /// <summary>
        /// Item slot type for the weapon item.
        /// 5=WeaponPistol, 6=Weapon
        /// </summary>
        [XmlElement("ItemSlot")]
        public int ItemSlot = 6;

        /// <summary>
        /// Item sub category.
        /// </summary>
        [XmlElement("SubCategory")]
        public int SubCategory;

        /// <summary>
        /// Cost to purchase/build this weapon.
        /// </summary>
        [XmlElement("Cost")]
        public float Cost;

        /// <summary>
        /// Research cost.
        /// </summary>
        [XmlElement("ResearchCost")]
        public float ResearchCost;

        /// <summary>
        /// Progression value (0.0-1.0) for availability timing.
        /// </summary>
        [XmlElement("Progression")]
        public float Progression;

        /// <summary>
        /// Stealth vs Combat rating (-1 to 1). Negative = stealthy, positive = loud.
        /// </summary>
        [XmlElement("StealthVsCombat")]
        public float StealthVsCombat;

        /// <summary>
        /// Icon name for the UI.
        /// </summary>
        [XmlElement("IconName")]
        public string IconName = "";

        public WeaponDefinition() { }
    }

    /// <summary>
    /// Ammo type configuration for a weapon.
    /// </summary>
    [Serializable]
    public class WeaponAmmoDefinition
    {
        [XmlElement("Type")]
        public int Type;

        [XmlElement("DamageMin")]
        public float DamageMin;

        [XmlElement("DamageMax")]
        public float DamageMax;

        [XmlElement("DamageRadius")]
        public float DamageRadius;

        [XmlElement("KnockbackAmount")]
        public float KnockbackAmount;

        [XmlElement("MaxAmmo")]
        public int MaxAmmo;

        [XmlElement("ReloadTime")]
        public float ReloadTime;

        [XmlElement("ReloadSpeed")]
        public float ReloadSpeed = 1f;

        [XmlElement("ChargeTime")]
        public float ChargeTime;

        [XmlElement("ChargeEveryShot")]
        public bool ChargeEveryShot;

        [XmlElement("ShieldDamage")]
        public float ShieldDamage;

        [XmlElement("CritChance")]
        public float CritChance;

        [XmlElement("CritDamageMultiplier")]
        public float CritDamageMultiplier = 1.5f;

        [XmlElement("AccuracyDelta")]
        public float AccuracyDelta;

        [XmlElement("EmpDamage")]
        public float EmpDamage;

        [XmlElement("MaxBeamWidth")]
        public float MaxBeamWidth;

        [XmlElement("ProjectilesPerShot")]
        public int ProjectilesPerShot = 1;

        public WeaponAmmoDefinition() { }
    }
}
