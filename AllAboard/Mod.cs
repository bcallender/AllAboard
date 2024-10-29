using AllAboard.System.Patched;
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
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(AllAboard)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(AllAboard), m_Setting, new Setting(this));

            PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data = (uint) m_Setting.TrainMaxDwellDelaySlider;
            PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data = (uint) m_Setting.BusMaxDwellDelaySlider;
            
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PatchedTransportTrainAISystem>().Enabled = false;

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PatchedTransportCarAISystem>().Enabled = false;
            updateSystem.UpdateAt<PatchedTransportCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PatchedTransportTrainAISystem>(SystemUpdatePhase.GameSimulation);
            log.Info("Completed Replacement of Base Train/CarAI Systems.");
            log.InfoFormat("Bus Max Dwell Time: {0}", PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data);
            log.InfoFormat("Train Max Dwell Time: {0}", PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data);
            //updateSystem.UpdateBefore<InstantBoardingSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}