using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PersuadatronMod.Models
{
    /// <summary>
    /// Defines an implant item that can be equipped in one of the four augmentation slots.
    /// Each implant has a tier (Mk1/Mk2/Mk3) and provides stat modifiers.
    /// </summary>
    [Serializable]
    public class ImplantDefinition
    {
        /// <summary>
        /// Unique item ID for this implant.
        /// </summary>
        [XmlElement("ItemID")]
        public int ItemID;

        /// <summary>
        /// Display name shown in the UI.
        /// </summary>
        [XmlElement("Name")]
        public string Name;

        /// <summary>
        /// The augmentation slot this implant occupies.
        /// 1=Head, 2=Body, 3=Arms, 4=Legs
        /// </summary>
        [XmlElement("SlotType")]
        public int SlotType;

        /// <summary>
        /// Tier of this implant: 1=Mk1, 2=Mk2, 3=Mk3
        /// </summary>
        [XmlElement("Tier")]
        public int Tier;

        /// <summary>
        /// Cost to purchase/research this implant.
        /// </summary>
        [XmlElement("Cost")]
        public float Cost;

        /// <summary>
        /// Research cost for this implant.
        /// </summary>
        [XmlElement("ResearchCost")]
        public float ResearchCost;

        /// <summary>
        /// Progression value (0.0-1.0) determining when this item becomes available.
        /// </summary>
        [XmlElement("Progression")]
        public float Progression;

        /// <summary>
        /// Stat modifiers this implant provides.
        /// </summary>
        [XmlArray("Modifiers")]
        [XmlArrayItem("Modifier")]
        public List<ImplantModifier> Modifiers = new List<ImplantModifier>();

        /// <summary>
        /// Ability IDs granted by this implant.
        /// </summary>
        [XmlArray("Abilities")]
        [XmlArrayItem("AbilityID")]
        public List<int> AbilityIDs = new List<int>();

        /// <summary>
        /// For brain implants: the Persuadatron level this implant unlocks.
        /// 0 = no persuadatron capability.
        /// </summary>
        [XmlElement("PersuadatronLevel")]
        public int PersuadatronLevel;

        /// <summary>
        /// Icon name for the UI.
        /// </summary>
        [XmlElement("IconName")]
        public string IconName = "";

        public ImplantDefinition() { }
    }

    /// <summary>
    /// A stat modifier applied by an implant.
    /// </summary>
    [Serializable]
    public class ImplantModifier
    {
        /// <summary>
        /// Modifier type: 1=Add, 2=Multiply, 3=Replace
        /// </summary>
        [XmlAttribute("Type")]
        public int Type;

        /// <summary>
        /// The amount to apply.
        /// </summary>
        [XmlAttribute("Amount")]
        public float Amount;

        /// <summary>
        /// Timeout in seconds. 0 = permanent while equipped.
        /// </summary>
        [XmlAttribute("TimeOut")]
        public float TimeOut;

        public ImplantModifier() { }
    }
}
