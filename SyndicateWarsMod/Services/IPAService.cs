using System;
using System.Collections.Generic;
using SyndicateWarsMod.Config;
using SyndicateWarsMod.Models;
using UnityEngine;

namespace SyndicateWarsMod.Services
{
    /// <summary>
    /// Intelligence, Perception, Adrenaline (IPA) system.
    /// Manages three dynamic sliders per agent that affect behavior and combat performance.
    /// 
    /// Intelligence (0.0–1.0): Agent autonomy
    ///   High: Agent independently seeks and prioritizes targets
    ///   Low: Agent waits for player commands
    /// 
    /// Perception (0.0–1.0): Accuracy and critical hits
    ///   High: AccuracyModifier bonus + CritChance bonus
    ///   Low: Standard values
    /// 
    /// Adrenaline (0.0–1.0): Speed and action rate
    ///   High: SpeedMultiplier bonus, higher fire rate
    ///   Drains over time; below threshold causes exhaustion penalty
    /// </summary>
    public class IPAService
    {
        private readonly SyndicateWarsConfig config;
        private readonly Dictionary<int, IPAState> agentStates;
        private float lastUpdateTime;
        private bool showOverlay;

        public IPAService(SyndicateWarsConfig config)
        {
            this.config = config;
            this.agentStates = new Dictionary<int, IPAState>();
            this.lastUpdateTime = 0f;
            this.showOverlay = false;
        }

        /// <summary>
        /// Whether the IPA overlay is currently shown.
        /// </summary>
        public bool ShowOverlay
        {
            get { return showOverlay; }
            set { showOverlay = value; }
        }

        /// <summary>
        /// Main update loop. Handles adrenaline drain and applies IPA effects to agents.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!config.IPAEnabled)
                return;

            if (Time.time < lastUpdateTime + config.IPAUpdateInterval)
                return;

            lastUpdateTime = Time.time;

            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null)
                        continue;

                    int agentID = agent.GetInstanceID();
                    IPAState state = GetOrCreateState(agentID);

                    // Drain adrenaline over time
                    DrainAdrenaline(state, config.IPAUpdateInterval);

                    // Apply IPA effects
                    ApplyIPAEffects(agent, state);
                }

                // Show overlay if enabled
                if (showOverlay)
                {
                    DisplayOverlay();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: IPA update error: " + e.Message);
            }
        }

        /// <summary>
        /// Adjusts Intelligence for the selected agent.
        /// </summary>
        public void AdjustIntelligence(float delta)
        {
            AdjustSelectedAgent("Intelligence", delta, delegate(IPAState s, float v) { s.Intelligence = v; },
                delegate(IPAState s) { return s.Intelligence; });
        }

        /// <summary>
        /// Adjusts Perception for the selected agent.
        /// </summary>
        public void AdjustPerception(float delta)
        {
            AdjustSelectedAgent("Perception", delta, delegate(IPAState s, float v) { s.Perception = v; },
                delegate(IPAState s) { return s.Perception; });
        }

        /// <summary>
        /// Adjusts Adrenaline for the selected agent.
        /// </summary>
        public void AdjustAdrenaline(float delta)
        {
            AdjustSelectedAgent("Adrenaline", delta, delegate(IPAState s, float v) { s.Adrenaline = v; },
                delegate(IPAState s) { return s.Adrenaline; });
        }

        /// <summary>
        /// Gets a formatted status string for all agents' IPA states.
        /// </summary>
        public string GetStatusString()
        {
            string result = "=== IPA System ===\n";
            try
            {
                foreach (AgentAI agent in AgentAI.GetAgents())
                {
                    if (agent == null)
                        continue;

                    int agentID = agent.GetInstanceID();
                    IPAState state = GetOrCreateState(agentID);
                    string agentName = "Agent";
                    try { agentName = agent.GetName(); } catch { }

                    result += agentName + ": " + state.GetStatusString() + "\n";
                }
            }
            catch (Exception e)
            {
                result += "Error: " + e.Message + "\n";
            }
            return result;
        }

        /// <summary>
        /// Gets or creates an IPA state for an agent.
        /// </summary>
        private IPAState GetOrCreateState(int agentID)
        {
            if (!agentStates.ContainsKey(agentID))
            {
                agentStates[agentID] = new IPAState();
            }
            return agentStates[agentID];
        }

        /// <summary>
        /// Drains adrenaline over time and manages exhaustion state.
        /// </summary>
        private void DrainAdrenaline(IPAState state, float deltaTime)
        {
            if (state.Adrenaline > 0f)
            {
                state.Adrenaline = Mathf.Max(0f, state.Adrenaline - config.AdrenalineDrainRate * deltaTime);
            }

            // Check exhaustion
            if (state.Adrenaline < config.AdrenalineExhaustionThreshold)
            {
                state.IsExhausted = true;
            }
            else if (state.Adrenaline > config.AdrenalineRecoveryThreshold)
            {
                state.IsExhausted = false;
            }
        }

        /// <summary>
        /// Applies IPA effects to an agent's combat stats.
        /// Uses the agent's modifier system to apply bonuses/penalties.
        /// </summary>
        private void ApplyIPAEffects(AgentAI agent, IPAState state)
        {
            try
            {
                // Adrenaline effects: Speed
                if (state.IsExhausted)
                {
                    // Exhaustion penalty - agent is slow and sluggish
                    // We apply this as a temporary speed reduction
                    ApplySpeedModifier(agent, 1f + config.AdrenalineExhaustionPenalty);
                }
                else if (state.Adrenaline > 0.5f)
                {
                    // Adrenaline boost - proportional to level above 0.5
                    float bonusFraction = (state.Adrenaline - 0.5f) * 2f; // 0 to 1
                    float speedBonus = 1f + bonusFraction * config.AdrenalineMaxSpeedBonus;
                    ApplySpeedModifier(agent, speedBonus);
                }
                else
                {
                    ApplySpeedModifier(agent, 1f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: IPA effects error: " + e.Message);
            }
        }

        /// <summary>
        /// Applies a speed modifier to an agent via the agent's modifier system.
        /// </summary>
        private void ApplySpeedModifier(AgentAI agent, float speedMultiplier)
        {
            try
            {
                // Access the agent's Modifiers component to set speed
                var modifiersField = typeof(AIEntity).GetField("m_Modifiers",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (modifiersField != null)
                {
                    var modifiers = modifiersField.GetValue(agent);
                    if (modifiers != null)
                    {
                        // Try to set speed multiplier via the modifier system
                        var setSpeedMethod = modifiers.GetType().GetMethod("SetSpeedMultiplier",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                        if (setSpeedMethod != null)
                        {
                            setSpeedMethod.Invoke(modifiers, new object[] { speedMultiplier });
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore - speed modification may not be available
            }
        }

        /// <summary>
        /// Adjusts an IPA value for the currently selected agent.
        /// </summary>
        private void AdjustSelectedAgent(string valueName, float delta,
            Action<IPAState, float> setter, Func<IPAState, float> getter)
        {
            try
            {
                AgentAI agent = AgentAI.FirstSelectedAgentAi();
                if (agent == null)
                {
                    Manager.GetUIManager().ShowSubtitle("IPA: No agent selected", 2);
                    return;
                }

                int agentID = agent.GetInstanceID();
                IPAState state = GetOrCreateState(agentID);

                float currentValue = getter(state);
                float newValue = Mathf.Clamp01(currentValue + delta);
                setter(state, newValue);

                string agentName = "Agent";
                try { agentName = agent.GetName(); } catch { }

                Manager.GetUIManager().ShowSubtitle(
                    agentName + " " + valueName + ": " + (newValue * 100f).ToString("F0") + "%", 2);
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: IPA adjust error: " + e.Message);
            }
        }

        /// <summary>
        /// Displays the IPA overlay with current values for all agents.
        /// </summary>
        private void DisplayOverlay()
        {
            try
            {
                string overlay = GetStatusString();
                Manager.GetUIManager().ShowSubtitle(overlay, config.IPAUpdateInterval + 0.1f);
            }
            catch
            {
                // Silently ignore overlay errors
            }
        }

    }
}
