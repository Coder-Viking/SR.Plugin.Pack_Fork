using System;

namespace SyndicateWarsMod.Models
{
    /// <summary>
    /// Per-agent IPA (Intelligence, Perception, Adrenaline) state.
    /// Each value ranges from 0.0 to 1.0 and affects agent behavior and combat stats.
    /// </summary>
    [Serializable]
    public class IPAState
    {
        /// <summary>
        /// Intelligence (0.0–1.0): Controls agent autonomy.
        /// High: Agent seeks targets and prioritizes independently.
        /// Low: Agent waits for player commands.
        /// </summary>
        public float Intelligence;

        /// <summary>
        /// Perception (0.0–1.0): Controls accuracy and critical hit chance.
        /// High: Accuracy bonus, increased crit chance.
        /// Low: Standard values.
        /// </summary>
        public float Perception;

        /// <summary>
        /// Adrenaline (0.0–1.0): Controls speed and fire rate.
        /// High: Speed and fire rate bonus.
        /// Drains over time. Below 0.1 causes exhaustion (negative modifiers).
        /// </summary>
        public float Adrenaline;

        /// <summary>
        /// Whether the agent is currently exhausted (Adrenaline below threshold).
        /// </summary>
        public bool IsExhausted;

        /// <summary>
        /// Time accumulator for adrenaline drain.
        /// </summary>
        public float DrainAccumulator;

        public IPAState()
        {
            Intelligence = 0.5f;
            Perception = 0.5f;
            Adrenaline = 0.5f;
            IsExhausted = false;
            DrainAccumulator = 0f;
        }

        /// <summary>
        /// Returns a formatted status string for this IPA state.
        /// </summary>
        public string GetStatusString()
        {
            string exhaustedStr = IsExhausted ? " [EXHAUSTED]" : "";
            return "I:" + (Intelligence * 100f).ToString("F0") + "% " +
                   "P:" + (Perception * 100f).ToString("F0") + "% " +
                   "A:" + (Adrenaline * 100f).ToString("F0") + "%" + exhaustedStr;
        }
    }
}
