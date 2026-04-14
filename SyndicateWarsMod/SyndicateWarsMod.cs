using System;
using SyndicateWarsMod.Config;
using SyndicateWarsMod.Services;
using UnityEngine;

namespace SyndicateWarsMod
{
    /// <summary>
    /// Syndicate Wars Features Mod for Satellite Reign.
    /// 
    /// Implements the following systems inspired by Syndicate Wars (1996):
    /// 
    ///   Phase 1 - Arsenal: 6 new weapons (Long Range Rifle, Razor Wire, Pulse Laser,
    ///             Graviton Gun, Satellite Rain, Nuclear Grenade)
    ///   Phase 2 - Implants: 12 enhanced implants (4 slots x 3 tiers) with combo effects
    ///             integrating Eyes (weapon range) into Brain and Heart (regen) into Body
    ///   Phase 3 - IPA System: Intelligence/Perception/Adrenaline sliders per agent
    ///             controlling autonomy, accuracy, and speed
    ///   Phase 4 - Dual Shields: Energy Shield (regen) and Hard Shield (no regen) as gear items
    ///   Phase 5 - Environment Effects: Satellite Rain orbital strike, chain reactions,
    ///             AOE knockback zones
    /// 
    /// Hotkeys:
    ///   F7    - Show Syndicate Wars Mod status
    ///   F8    - Reload configuration
    ///   I     - Toggle IPA overlay
    ///   [/]   - Adjust Intelligence (-/+)
    ///   ,/.   - Adjust Perception (-/+)
    ///   -/=   - Adjust Adrenaline (-/+)
    ///   H     - Trigger Satellite Rain orbital strike
    /// </summary>
    public class SyndicateWarsMod : ISrPlugin
    {
        #region Fields
        private bool isInitialized;
        private bool servicesRegistered;
        private SyndicateWarsConfig config;

        // Services
        private SWArsenalService arsenalService;
        private SWImplantService implantService;
        private IPAService ipaService;
        private DualShieldService shieldService;
        private EnvironmentEffectsService envService;
        #endregion

        #region ISrPlugin Implementation
        public string GetName()
        {
            return "Syndicate Wars Mod v1.0";
        }

        public void Initialize()
        {
            try
            {
                Debug.Log("SyndicateWarsMod: Initializing...");
                isInitialized = true;
                servicesRegistered = false;
                Debug.Log("SyndicateWarsMod: Initialization complete. Waiting for game to load...");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Initialization failed: " + e.Message);
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

                // Update IPA system
                ipaService.Update(Time.deltaTime);

                // Update dual shield system
                shieldService.Update(Time.deltaTime);

                // Update environment effects
                envService.Update(Time.deltaTime);

                // Apply body implant regen effects (Heart combo)
                implantService.ApplyBodyImplantEffects(Time.deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Update error: " + e.Message);
            }
        }
        #endregion

        #region Service Initialization
        /// <summary>
        /// Initializes all services once the game is fully loaded.
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // Load config
                string pluginPath = Manager.GetPluginManager().PluginPath;
                config = SyndicateWarsConfig.Load(pluginPath);

                // Initialize services
                arsenalService = new SWArsenalService(config);
                implantService = new SWImplantService(config);
                ipaService = new IPAService(config);
                shieldService = new DualShieldService(config);
                envService = new EnvironmentEffectsService(config);

                // Register items with the game
                arsenalService.RegisterWeapons();
                implantService.RegisterImplants();
                shieldService.RegisterShields();

                servicesRegistered = true;

                // Show welcome message
                Manager.GetUIManager().ShowMessagePopup(
                    "Syndicate Wars Mod Loaded!\n\n" +
                    "New Arsenal: Long Range Rifle, Razor Wire, Pulse Laser,\n" +
                    "  Graviton Gun, Satellite Rain, Nuclear Grenade\n\n" +
                    "Enhanced Implants: Brain+Eyes, Body+Heart combos\n" +
                    "IPA System: [/] Intelligence, ,/. Perception, -/= Adrenaline\n" +
                    "Dual Shields: Energy + Hard Shield items\n\n" +
                    "Hotkeys: F7=Status, F8=Reload, I=IPA, H=Satellite Rain", 12);

                Debug.Log("SyndicateWarsMod: All services initialized - " +
                    arsenalService.WeaponCount + " weapons, " +
                    implantService.ImplantCount + " implants, " +
                    shieldService.ShieldItemCount + " shields registered");
            }
            catch (Exception e)
            {
                Debug.LogError("SyndicateWarsMod: Service initialization failed: " +
                    e.Message + "\n" + e.StackTrace);
            }
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Handles all hotkey input for the mod.
        /// </summary>
        private void HandleInput()
        {
            // F7 - Show status
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ShowStatus();
            }

            // F8 - Reload config
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ReloadConfig();
            }

            // I - Toggle IPA overlay
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleIPAOverlay();
            }

            // IPA Adjustments (only when IPA is enabled)
            if (config.IPAEnabled)
            {
                // [ / ] - Intelligence
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                    ipaService.AdjustIntelligence(-config.IPAAdjustStep);
                if (Input.GetKeyDown(KeyCode.RightBracket))
                    ipaService.AdjustIntelligence(config.IPAAdjustStep);

                // , / . - Perception
                if (Input.GetKeyDown(KeyCode.Comma))
                    ipaService.AdjustPerception(-config.IPAAdjustStep);
                if (Input.GetKeyDown(KeyCode.Period))
                    ipaService.AdjustPerception(config.IPAAdjustStep);

                // - / = - Adrenaline
                if (Input.GetKeyDown(KeyCode.Minus))
                    ipaService.AdjustAdrenaline(-config.IPAAdjustStep);
                if (Input.GetKeyDown(KeyCode.Equals))
                    ipaService.AdjustAdrenaline(config.IPAAdjustStep);
            }

            // H - Satellite Rain
            if (Input.GetKeyDown(KeyCode.H))
            {
                envService.TriggerSatelliteRain();
            }
        }
        #endregion

        #region Status Display
        /// <summary>
        /// Shows a comprehensive status popup with all subsystem states.
        /// </summary>
        private void ShowStatus()
        {
            try
            {
                string status =
                    "=== Syndicate Wars Mod Status ===\n\n" +
                    "Weapons: " + arsenalService.WeaponCount + " registered\n" +
                    "Implants: " + implantService.ImplantCount + " registered\n" +
                    "Shields: " + shieldService.ShieldItemCount + " registered\n\n";

                // IPA Status
                if (config.IPAEnabled)
                {
                    status += ipaService.GetStatusString() + "\n";
                }
                else
                {
                    status += "IPA System: Disabled\n\n";
                }

                // Shield Status for selected agent
                AgentAI selectedAgent = AgentAI.FirstSelectedAgentAi();
                if (selectedAgent != null)
                {
                    status += "Selected Agent Shields: " +
                        shieldService.GetAgentShieldStatus(selectedAgent) + "\n\n";
                }

                // Environment Status
                status += envService.GetStatusString() + "\n\n";

                // Hotkeys
                status += "Hotkeys: F7=Status, F8=Reload, I=IPA\n" +
                    "[/]=Int, ,/.=Per, -/==Adr, H=SatRain";

                Manager.GetUIManager().ShowMessagePopup(status, 15);
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup(
                    "Syndicate Wars Mod: Status error - " + e.Message, 5);
            }
        }

        /// <summary>
        /// Toggles the IPA overlay display.
        /// </summary>
        private void ToggleIPAOverlay()
        {
            ipaService.ShowOverlay = !ipaService.ShowOverlay;
            string state = ipaService.ShowOverlay ? "ON" : "OFF";
            Manager.GetUIManager().ShowSubtitle("IPA Overlay: " + state, 2);
        }

        /// <summary>
        /// Reloads the configuration from disk.
        /// </summary>
        private void ReloadConfig()
        {
            try
            {
                string pluginPath = Manager.GetPluginManager().PluginPath;
                config = SyndicateWarsConfig.Load(pluginPath);
                Manager.GetUIManager().ShowMessagePopup("Syndicate Wars Mod: Config reloaded!", 3);
                Debug.Log("SyndicateWarsMod: Config reloaded");
            }
            catch (Exception e)
            {
                Manager.GetUIManager().ShowMessagePopup(
                    "Config reload failed: " + e.Message, 5);
            }
        }
        #endregion
    }
}
