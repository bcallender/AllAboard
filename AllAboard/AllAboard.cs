using AllAboard.System.Utility;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Entities;

namespace AllAboard
{
    public class AllAboard : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(AllAboard)}.Mod")
            .SetShowsErrorsInUI(false);

        public static AllAboardSettings m_AllAboardSettings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_AllAboardSettings = new AllAboardSettings(this);
            m_AllAboardSettings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_AllAboardSettings));


            AssetDatabase.global.LoadSettings("AllAboard", m_AllAboardSettings,
                new AllAboardSettings(this));

            PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data =
                (uint)m_AllAboardSettings.TrainMaxDwellDelaySlider;
            PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data =
                (uint)m_AllAboardSettings.BusMaxDwellDelaySlider;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransportTrainAISystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransportCarAISystem>().Enabled = false;
            updateSystem.UpdateAt<System.Patched.PatchedTransportCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<System.Patched.PatchedTransportTrainAISystem>(SystemUpdatePhase.GameSimulation);
            log.Info("Completed Replacement of Base Train/CarAI Systems.");
            log.InfoFormat("Bus Max Dwell Time: {0}", PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data);
            log.InfoFormat("Train Max Dwell Time: {0}", PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data);
            //updateSystem.UpdateBefore<InstantBoardingSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_AllAboardSettings != null)
            {
                m_AllAboardSettings.UnregisterInOptionsUI();
                m_AllAboardSettings = null;
            }
        }
    }
}